"""Unity Build Automation client. Never serialize raw API responses or credentials."""
import argparse
import json
import os
import sys
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.parse import quote
from urllib.request import Request, urlopen

BASE = 'https://build-api.cloud.unity3d.com/api/v1'

class CloudError(RuntimeError):
    pass

class Client:
    def __init__(self, key):
        if not key:
            raise CloudError('UNITY_BUILD_API_KEY Secret is missing.')
        self.key = key.strip()
        self.scheme = 'Basic'

    def request(self, path, payload=None):
        headers = {'Authorization': self.scheme + ' ' + self.key, 'Accept': 'application/json'}
        data = None if payload is None else json.dumps(payload).encode()
        if data is not None:
            headers['Content-Type'] = 'application/json'
        for attempt in range(2):
            headers['Authorization'] = self.scheme + ' ' + self.key
            try:
                with urlopen(Request(BASE + path, data=data, headers=headers), timeout=60) as response:
                    return json.load(response)
            except HTTPError as exc:
                # The API docs describe Basic; the OpenAPI schema also allows Bearer.
                # Retry a read-only authentication probe only, never repeat a build POST.
                if exc.code == 401 and payload is None and attempt == 0 and self.scheme == 'Basic':
                    self.scheme = 'Bearer'
                    continue
                raise CloudError(f'Unity API returned HTTP {exc.code} for {path}. Response body omitted.') from None
            except URLError:
                raise CloudError('Unity API connection failed; no credentials were logged.') from None
        raise CloudError('Unity API authentication failed.')

def segment(value):
    return quote(str(value), safe='')

def project_path(org, project):
    return f'/orgs/{segment(org)}/projects/{segment(project)}'

def discover(client):
    projects = client.request('/projects')
    if isinstance(projects, dict):
        projects = projects.get('projects', [])
    wanted = os.getenv('UNITY_CLOUD_PROJECT_ID', '').strip()
    matches = [p for p in projects if (str(p.get('projectid')) == wanted if wanted else p.get('name', '').casefold() == 'pickdoll')]
    if len(matches) != 1:
        raise CloudError('Expected one PickDoll project. Set UNITY_CLOUD_PROJECT_ID repository variable to select it.')
    p = matches[0]
    org, project = p.get('orgid'), p.get('projectid')
    if not org or not project:
        raise CloudError('Unity API did not return organization/project identifiers.')
    targets = client.request(project_path(org, project) + '/buildtargets')
    summary = {'organization': org, 'project': project, 'name': p['name'], 'targets': []}
    for target in targets:
        tid = target.get('buildtargetid')
        if not tid:
            continue
        target = client.request(project_path(org, project) + '/buildtargets/' + segment(tid))
        settings = target.get('settings', {})
        advanced = settings.get('advanced', {})
        scm = settings.get('scm', {})
        summary['targets'].append({
            'id': tid, 'name': target.get('name'), 'platform': target.get('platform'),
            'enabled': target.get('enabled'), 'branch': scm.get('branch'),
            'subfolder': settings.get('projectSubfolder') or settings.get('projectSubfolderPath') or settings.get('subfolder') or scm.get('subfolder'),
            'auto_build': settings.get('autoBuild'), 'unity_version': settings.get('unityVersion'),
            'auto_detect_version': settings.get('autoDetectUnityVersion'),
            'machine_type': settings.get('machineType') or advanced.get('machineType'),
            'operating_system': settings.get('operatingSystemSelected'),
            'setting_fields': sorted(settings.keys()),
            'advanced_fields': sorted(advanced.keys()),
            'scm_fields': sorted(scm.keys()),
            'signing_configured': bool(target.get('credentials', {}).get('signing')),
        })
    return summary

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', default='ci-output')
    args = parser.parse_args()
    summary = discover(Client(os.getenv('UNITY_BUILD_API_KEY', '')))
    output = Path(args.output)
    output.mkdir(parents=True, exist_ok=True)
    (output / 'inspection.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(summary, indent=2))
    print('Read-only Unity connection inspection completed. No builds started.')

if __name__ == '__main__':
    try:
        main()
    except CloudError as exc:
        print('::error::' + str(exc), file=sys.stderr)
        sys.exit(1)
