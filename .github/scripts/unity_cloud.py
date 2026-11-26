"""Unity Cloud orchestration. Raw API responses and signed URLs are never logged."""
import argparse
import hashlib
import json
import os
import shutil
import sys
import time
import zipfile
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.parse import quote, urljoin, urlsplit
from urllib.request import HTTPRedirectHandler, Request, build_opener

BASE = 'https://build-api.cloud.unity3d.com/api/v1'
HOST = urlsplit(BASE).netloc
TERMINAL = {'success', 'failure', 'canceled', 'unknown'}

class CloudError(RuntimeError):
    pass

class SafeRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        redirected = super().redirect_request(req, fp, code, msg, headers, newurl)
        if redirected and urlsplit(req.full_url).netloc != urlsplit(newurl).netloc:
            redirected.remove_header('Authorization')
        return redirected

class Client:
    def __init__(self, key):
        if not key:
            raise CloudError('UNITY_BUILD_API_KEY Secret is missing.')
        self.key = key.strip()
        self.scheme = 'Basic'
        self.opener = build_opener(SafeRedirect())

    def open(self, path, payload=None):
        url = urljoin(BASE + '/', path) if path.startswith('https:') else (
            'https://' + HOST + path if path.startswith('/api/') else BASE + path)
        if urlsplit(url).netloc != HOST:
            raise CloudError('Refused to send credentials to an external host.')
        data = None if payload is None else json.dumps(payload).encode()
        for attempt in range(2):
            headers = {'Authorization': self.scheme + ' ' + self.key, 'Accept': 'application/json'}
            if data is not None:
                headers['Content-Type'] = 'application/json'
            try:
                return self.opener.open(Request(url, data=data, headers=headers), timeout=60)
            except HTTPError as exc:
                if exc.code == 303:
                    return exc  # Unity uses a JSON string body, sometimes without Location.
                if exc.code == 401 and payload is None and attempt == 0 and self.scheme == 'Basic':
                    self.scheme = 'Bearer'
                    continue
                raise CloudError(f'Unity API HTTP {exc.code}: {urlsplit(url).path}. Body omitted.') from None
            except URLError:
                raise CloudError('Unity API connection failed. No credentials were logged.') from None
        raise CloudError('Unity API authentication failed.')

    def request(self, path, payload=None, method=None):
        # Updates use a complete settings object so unrelated fields are preserved.
        if method == 'PUT':
            url = BASE + path
            req = Request(url, data=json.dumps(payload).encode(), method='PUT', headers={
                'Authorization': self.scheme + ' ' + self.key, 'Content-Type': 'application/json'})
            try:
                with self.opener.open(req, timeout=60) as response:
                    return json.load(response)
            except HTTPError as exc:
                raise CloudError(f'Unity configuration update failed: HTTP {exc.code}. Body omitted.') from None
        with self.open(path, payload) as response:
            return json.load(response)

    def download(self, href, destination):
        url = urljoin('https://' + HOST, href)
        for _ in range(5):
            if urlsplit(url).scheme != 'https':
                raise CloudError('Artifact download must use HTTPS.')
            if urlsplit(url).netloc == HOST:
                response = self.open(url)
            else:
                # Signed artifact URLs receive NO Unity Authorization header.
                response = self.opener.open(Request(url), timeout=120)
            with response:
                if response.status == 303 or 'json' in response.headers.get('Content-Type', ''):
                    url = json.load(response)
                    if not isinstance(url, str):
                        raise CloudError('Unexpected artifact redirect format.')
                    continue
                with destination.open('wb') as output:
                    shutil.copyfileobj(response, output)
                return
        raise CloudError('Too many artifact redirects.')

def segment(value):
    return quote(str(value), safe='')

def project_path(org, project):
    return f'/orgs/{segment(org)}/projects/{segment(project)}'

def safe_target(target):
    settings = target.get('settings', {})
    scm = settings.get('scm', {})
    advanced = settings.get('advanced', {})
    return {
        'id': target.get('buildtargetid'), 'name': target.get('name'),
        'platform': target.get('platform'), 'enabled': target.get('enabled'),
        'branch': scm.get('branch'), 'subfolder': scm.get('subdirectory'),
        'auto_build': settings.get('autoBuild'),
        'unity_version': settings.get('unityVersion'),
        'auto_detect_version': settings.get('autoDetectUnityVersion'),
        'machine_type': settings.get('machineTypeLabel'),
        'operating_system': settings.get('operatingSystemSelected'),
        'operating_system_version': settings.get('operatingSystemVersion'),
        'cache': settings.get('remoteCacheStrategy'),
        'executable': settings.get('executablename'),
        'android_settings': {k: v for k, v in advanced.get('android', {}).items()
                             if k in {'buildAppBundle', 'buildAPK', 'splitBinary', 'buildAssetPacks'}},
        'signing_configured': bool((target.get('credentials') or {}).get('signing')),
    }

def discover(client, configure=False):
    projects = client.request('/projects')
    if isinstance(projects, dict):
        projects = projects.get('projects', [])
    wanted = os.getenv('UNITY_CLOUD_PROJECT_ID', '').strip()
    matches = [p for p in projects if (str(p.get('projectid')) == wanted if wanted
               else p.get('name', '').casefold() == 'pickdoll')]
    if len(matches) != 1:
        raise CloudError('Expected one PickDoll project. Set UNITY_CLOUD_PROJECT_ID repository variable to select it.')
    p = matches[0]
    org, project = p.get('orgid'), p.get('projectid')
    if not org or not project:
        raise CloudError('Unity API did not return organization/project identifiers.')
    base = project_path(org, project)
    targets = client.request(base + '/buildtargets')
    summary = {'organization': org, 'project': project, 'name': p['name'], 'targets': []}
    for entry in targets:
        tid = entry.get('buildtargetid')
        if not tid:
            continue
        path = base + '/buildtargets/' + segment(tid)
        target = client.request(path)
        if target.get('platform') not in {'android', 'standalonewindows64'}:
            continue
        if configure:
            settings = target['settings']
            settings['scm']['branch'] = 'main'
            settings['scm']['subdirectory'] = 'Unity'
            settings['autoBuild'] = False
            settings['autoDetectUnityVersion'] = True
            settings['executablename'] = 'PickDoll'
            client.request(path, {'settings': settings}, method='PUT')
            target = client.request(path)
        summary['targets'].append(safe_target(target))
    return summary

def select_targets(summary, platform):
    requested = {'android': 'android', 'windows': 'standalonewindows64'}
    wanted = list(requested.values()) if platform == 'both' else [requested[platform]]
    targets = []
    for target_platform in wanted:
        matches = [t for t in summary['targets'] if t['platform'] == target_platform]
        if len(matches) != 1:
            raise CloudError('Expected one enabled build target for ' + target_platform)
        t = matches[0]
        if not t['enabled'] or t['subfolder'] != 'Unity' or t['branch'] != 'main':
            raise CloudError('Build target must be enabled, branch main, subfolder Unity: ' + t['id'])
        if t['auto_build'] or not t['auto_detect_version']:
            raise CloudError('Disable auto-build and enable Unity version auto-detection: ' + t['id'])
        if t['operating_system'] != 'windows' or 'micro' not in str(t['machine_type']).lower():
            raise CloudError('Use a Windows Micro builder to keep the expected free-tier cost: ' + t['id'])
        if target_platform == 'android' and t.get('android_settings', {}).get('buildAppBundle'):
            raise CloudError('Disable AAB output for the test APK.')
        if target_platform == 'android' and not t['signing_configured']:
            raise CloudError('Android signing credentials are missing.')
        targets.append(t)
    return targets

def package_primary(source, output, platform):
    if not zipfile.is_zipfile(source):
        raise CloudError('Unity artifact is not a valid ZIP/APK.')
    if platform == 'android':
        with zipfile.ZipFile(source) as archive:
            names = archive.namelist()
            if 'AndroidManifest.xml' in names:
                shutil.copy2(source, output / 'PickDoll.apk')
            else:
                apks = [n for n in names if n.lower().endswith('.apk')]
                if len(apks) != 1:
                    raise CloudError('Expected one APK. Verify the Android AAB option is disabled.')
                with archive.open(apks[0]) as data, (output / 'PickDoll.apk').open('wb') as dest:
                    shutil.copyfileobj(data, dest)
        return output / 'PickDoll.apk'
    destination = output / 'PickDoll-Windows-x64.zip'
    with zipfile.ZipFile(source) as original, zipfile.ZipFile(destination, 'w', zipfile.ZIP_DEFLATED) as archive:
        names = original.namelist()
        games = [n for n in names if Path(n).name == 'PickDoll.exe']
        if len(games) != 1:
            raise CloudError('Expected PickDoll.exe in Windows artifact.')
        prefix = games[0][:-len('PickDoll.exe')]
        if prefix + 'UnityPlayer.dll' not in names or prefix + 'PickDoll_Data/globalgamemanagers' not in names:
            raise CloudError('Windows artifact is missing runtime files.')
        for info in original.infolist():
            name = info.filename
            if info.is_dir() or not name.startswith(prefix):
                continue
            relative = name[len(prefix):]
            if '..' in Path(relative).parts or relative.startswith(('/', '\\')):
                raise CloudError('Unsafe archive path.')
            if Path(relative).suffix.lower() in {'.pdb', '.mdb', '.dbg', '.keystore', '.jks', '.ulf'} or 'DoNotShip' in relative:
                continue
            archive.writestr(relative, original.read(info))
        archive.writestr('Start-PickDoll.cmd', '@echo off\r\ncd /d "%~dp0"\r\nstart "" "%~dp0PickDoll.exe" -screen-fullscreen 0 -screen-width 540 -screen-height 960\r\n')
    with zipfile.ZipFile(destination) as archive:
        if archive.testzip():
            raise CloudError('Windows ZIP integrity check failed.')
    return destination

def build(client, summary, platform, output, revision, branch, timeout):
    if len(revision) != 40 or any(c not in '0123456789abcdef' for c in revision):
        raise CloudError('A full Git commit SHA is required.')
    targets = select_targets(summary, platform)
    records = []
    for target in targets:  # Sequential builds avoid unnecessary machine concurrency.
        path = project_path(summary['organization'], summary['project']) + '/buildtargets/' + segment(target['id']) + '/builds'
        attempts = client.request(path, {'clean': False, 'commit': revision, 'branch': branch})
        if not isinstance(attempts, list) or len(attempts) != 1:
            raise CloudError('Unexpected build submission response; do not retry blindly.')
        number = attempts[0]['build']
        status_path = path + '/' + segment(number)
        print(f"Started {target['platform']} build #{number} for {revision}", flush=True)
        deadline = time.monotonic() + timeout * 60
        previous = None
        while True:
            status = client.request(status_path)
            state = status.get('buildStatus')
            if state != previous:
                print(f"{target['platform']} #{number}: {state}", flush=True)
                previous = state
            if state in TERMINAL:
                break
            if time.monotonic() >= deadline:
                raise CloudError(f'Polling timed out for build #{number}. The cloud build may still run; check Unity Dashboard before retrying.')
            time.sleep(25)
        if state != 'success':
            # Failure stages/messages are intentionally not dumped: provider responses may contain credentials.
            raise CloudError(f"Unity build #{number} ended with {state}; see Unity Dashboard for details.")
        if status.get('lastBuiltRevision') != revision:
            raise CloudError('Unity built a different revision; refusing to publish its files.')
        link = status.get('links', {}).get('download_primary', {})
        if not link.get('href'):
            raise CloudError('Successful build did not provide a primary artifact link.')
        raw = output / ('primary-' + target['platform'] + '.zip')
        client.download(link['href'], raw)
        result = package_primary(raw, output, target['platform'])
        raw.unlink()
        with result.open('rb') as artifact:
            digest = hashlib.file_digest(artifact, 'sha256').hexdigest()
        records.append({'platform': target['platform'], 'build': number, 'commit': revision,
                        'unity_version': status.get('unityVersion'), 'file': result.name,
                        'sha256': digest})
        (output / 'builds.json').write_text(json.dumps(records, indent=2) + '\n', encoding='utf-8')
        (output / 'SHA256SUMS.txt').write_text(''.join(r['sha256'] + '  ' + r['file'] + '\n' for r in records), encoding='utf-8')
    print('Cloud builds and artifact integrity checks completed.', flush=True)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--operation', choices=['inspect', 'configure', 'build'], default='inspect')
    parser.add_argument('--platform', choices=['both', 'windows', 'android'], default='both')
    parser.add_argument('--output', default='ci-output')
    parser.add_argument('--timeout', type=int, default=90)
    args = parser.parse_args()
    client = Client(os.getenv('UNITY_BUILD_API_KEY', ''))
    summary = discover(client, configure=args.operation == 'configure')
    output = Path(args.output)
    output.mkdir(parents=True, exist_ok=True)
    (output / 'inspection.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(summary, indent=2), flush=True)
    if args.operation == 'build':
        build(client, summary, args.platform, output, os.environ['GITHUB_SHA'], os.environ['GITHUB_REF_NAME'], args.timeout)
    else:
        print('Configuration inspection completed. No builds started.', flush=True)

if __name__ == '__main__':
    try:
        main()
    except CloudError as exc:
        print('::error::' + str(exc), file=sys.stderr)
        sys.exit(1)
