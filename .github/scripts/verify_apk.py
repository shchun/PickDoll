"""Check cloud APK identity/version/signature without needing the private key."""
import argparse
import os
import re
import subprocess
from pathlib import Path

CERT = '705d4618d4d96e83b4b62f4dc7c3ebfb44c6d9cbfa8b94ebb73f1b67176b5f42'

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('apk', type=Path)
    args = parser.parse_args()
    sdk = Path(os.environ.get('ANDROID_HOME') or os.environ.get('ANDROID_SDK_ROOT', ''))
    tools = [p for p in (sdk / 'build-tools').glob('*') if (p / 'apksigner').is_file()]
    if not tools:
        raise SystemExit('Android build-tools are required to verify the cloud APK.')
    tools = sorted(tools, key=lambda p: [int(x) for x in re.findall(r'\d+', p.name)])[-1]
    signed = subprocess.run([str(tools / 'apksigner'), 'verify', '--print-certs', str(args.apk)],
                            check=True, capture_output=True, text=True).stdout
    matches = re.findall(r'certificate SHA-256 digest: ([0-9a-f]+)', signed)
    if matches != [CERT]:
        raise SystemExit('APK signer differs from the installed PickDoll test app; do not uninstall the existing app.')
    badging = subprocess.run([str(tools / 'aapt'), 'dump', 'badging', str(args.apk)],
                             check=True, capture_output=True, text=True).stdout
    settings = Path('Unity/ProjectSettings/ProjectSettings.asset').read_text(encoding='utf-8-sig')
    version = re.search(r'^  bundleVersion: (.+)$', settings, re.M).group(1).strip()
    code = re.search(r'^  AndroidBundleVersionCode: (\d+)$', settings, re.M).group(1)
    package = re.search(r"package: name='([^']+)' versionCode='([^']+)' versionName='([^']+)'", badging)
    if not package or package.groups() != ('com.precipi.pickdoll', code, version):
        raise SystemExit('APK package/version differs from the source project.')
    if "native-code: 'arm64-v8a'" not in badging:
        raise SystemExit('Expected an ARM64 APK.')
    print('APK signature, package, project version and ARM64 architecture verified.')

if __name__ == '__main__':
    main()
