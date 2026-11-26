import importlib.util
import io
import os
import unittest
from pathlib import Path
from unittest.mock import patch
from urllib.error import URLError

spec = importlib.util.spec_from_file_location('notify_slack', Path(__file__).with_name('notify_slack.py'))
notify = importlib.util.module_from_spec(spec)
spec.loader.exec_module(notify)

class NotificationTests(unittest.TestCase):
    def run_info(self, conclusion):
        return {'conclusion': conclusion, 'head_sha': 'a' * 40,
                'html_url': 'https://github.com/shchun/PickDoll/actions/runs/1'}

    def test_success_and_failure_messages_link_to_actions(self):
        self.assertIn('성공', notify.slack_text(self.run_info('success')))
        self.assertIn('실패', notify.slack_text(self.run_info('failure')))
        self.assertIn('Unity Build History', notify.slack_text(self.run_info('timed_out')))

    def test_external_result_links_are_rejected(self):
        run = self.run_info('success')
        run['html_url'] = 'https://example.com/phishing'
        with self.assertRaises(notify.NotifyError):
            notify.slack_text(run)

    def test_delivery_failure_does_not_expose_webhook(self):
        webhook = 'https://hooks.slack.com/services/example/test/secret'
        with patch.dict(os.environ, {'SLACK_WEBHOOK_URL': webhook}), \
             patch.object(notify, 'urlopen', side_effect=URLError(webhook)):
            with self.assertRaises(notify.NotifyError) as error:
                notify.send('Build completed')
        self.assertNotIn(webhook, str(error.exception))

    def test_slack_acknowledgement_is_checked(self):
        with patch.dict(os.environ, {'SLACK_WEBHOOK_URL': 'https://hooks.slack.com/services/example/test/secret'}), \
             patch.object(notify, 'urlopen', return_value=io.BytesIO(b'ok')):
            notify.send('Build completed')

if __name__ == '__main__':
    unittest.main()
