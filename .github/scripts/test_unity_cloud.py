import importlib.util
import io
import json
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest.mock import patch
from urllib.request import Request

spec = importlib.util.spec_from_file_location('unity_cloud', Path(__file__).with_name('unity_cloud.py'))
cloud = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cloud)

class CloudTests(unittest.TestCase):
    def target(self, platform='android'):
        return dict(id='target', platform=platform, enabled=True, branch='main', subfolder='Unity',
                    auto_build=False, auto_detect_version=True, operating_system='windows',
                    machine_type='win11_micro_v1', signing_configured=True, android_settings={'buildAppBundle': False})

    def test_preflight_prevents_wrong_branch_path_cost_and_unsigned_build(self):
        for field, bad in [('branch', 'old-branch'), ('subfolder', ''), ('enabled', False),
                           ('auto_build', True), ('auto_detect_version', False),
                           ('machine_type', 'win11_premium'), ('signing_configured', False)]:
            target = self.target()
            target[field] = bad
            with self.subTest(field=field), self.assertRaises(cloud.CloudError):
                cloud.select_targets({'targets': [target]}, 'android')

    def test_duplicate_target_is_not_built_twice(self):
        with self.assertRaises(cloud.CloudError):
            cloud.select_targets({'targets': [self.target(), self.target()]}, 'android')

    def test_sanitized_target_does_not_include_credentials(self):
        secret = 'NEVER_LOG_THIS'
        raw = {'name': 'Android', 'credentials': {'signing': {'password': secret}},
               'settings': {'scm': {'branch': 'main', 'password': secret}, 'advanced': {}}}
        self.assertNotIn(secret, json.dumps(cloud.safe_target(raw)))

    def test_redirect_strips_authorization_when_host_changes(self):
        req = Request('https://build-api.cloud.unity3d.com/a', headers={'Authorization': 'Basic private'})
        result = cloud.SafeRedirect().redirect_request(req, None, 302, 'Found', {}, 'https://storage.googleapis.com/a')
        self.assertIsNone(result.get_header('Authorization'))

    def test_api_refuses_credentials_to_external_host(self):
        with self.assertRaises(cloud.CloudError):
            cloud.Client('secret').open('https://example.com/a')

    def test_apk_wrapper_and_direct_apk_preserve_bytes(self):
        with tempfile.TemporaryDirectory() as directory:
            base = Path(directory)
            source = base / 'direct.zip'
            with zipfile.ZipFile(source, 'w') as archive:
                archive.writestr('AndroidManifest.xml', 'fixture')
            expected = source.read_bytes()
            output = base / 'out'
            output.mkdir()
            self.assertEqual(cloud.package_primary(source, output, 'android').read_bytes(), expected)
            wrapper = base / 'wrapper.zip'
            with zipfile.ZipFile(wrapper, 'w') as archive:
                archive.writestr('nested/game.apk', expected)
            self.assertEqual(cloud.package_primary(wrapper, output, 'android').read_bytes(), expected)

    def test_aab_and_ambiguous_apks_fail(self):
        with tempfile.TemporaryDirectory() as directory:
            base = Path(directory)
            source = base / 'artifact.zip'
            for names in [['game.aab'], ['one.apk', 'two.apk']]:
                with zipfile.ZipFile(source, 'w') as archive:
                    for name in names:
                        archive.writestr(name, 'fixture')
                with self.assertRaises(cloud.CloudError):
                    cloud.package_primary(source, base, 'android')

    def test_windows_package_preserves_runtime_and_strips_symbols(self):
        with tempfile.TemporaryDirectory() as directory:
            base = Path(directory)
            source = base / 'source.zip'
            with zipfile.ZipFile(source, 'w') as archive:
                for name in ['PickDoll.exe', 'UnityPlayer.dll', 'PickDoll_Data/globalgamemanagers',
                             'MonoBleedingEdge/runtime.dll', 'Game.pdb', 'private.keystore',
                             'PickDoll_BurstDebugInformation_DoNotShip/symbol.txt']:
                    archive.writestr('wrapper/' + name, 'fixture')
            result = cloud.package_primary(source, base, 'standalonewindows64')
            with zipfile.ZipFile(result) as archive:
                names = archive.namelist()
                self.assertIn('Start-PickDoll.cmd', names)
                self.assertIn('MonoBleedingEdge/runtime.dll', names)
                self.assertNotIn('Game.pdb', names)
                self.assertFalse(any('DoNotShip' in name for name in names))
                self.assertNotIn('private.keystore', names)

    def test_submission_is_not_retried_on_ambiguous_response(self):
        class Fake:
            calls = 0
            def request(self, *args):
                self.calls += 1
                return []
        fake = Fake()
        summary = {'organization': 'org', 'project': 'project', 'targets': [self.target()]}
        with tempfile.TemporaryDirectory() as directory, self.assertRaises(cloud.CloudError):
            cloud.build(fake, summary, 'android', Path(directory), 'a' * 40, 'main', 1)
        self.assertEqual(fake.calls, 1)

    def test_wrong_commit_never_downloads(self):
        class Fake:
            def request(self, path, payload=None):
                return [{'build': 1}] if payload else {'buildStatus': 'success', 'lastBuiltRevision': 'b' * 40}
            def download(self, *args):
                raise AssertionError('Must not download a mismatched revision')
        summary = {'organization': 'org', 'project': 'project', 'targets': [self.target()]}
        with tempfile.TemporaryDirectory() as directory, self.assertRaises(cloud.CloudError):
            cloud.build(Fake(), summary, 'android', Path(directory), 'a' * 40, 'main', 1)

if __name__ == '__main__':
    unittest.main()
