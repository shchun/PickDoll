"""Wait for one Actions run and notify the user-selected Slack incoming webhook."""
import argparse
import json
import os
import sys
import time
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

class NotifyError(RuntimeError):
    pass

def get_run(run_id):
    url = f"https://api.github.com/repos/{os.environ['GITHUB_REPOSITORY']}/actions/runs/{run_id}"
    request = Request(url, headers={'Authorization': 'Bearer ' + os.environ['GH_TOKEN'],
                                   'Accept': 'application/vnd.github+json',
                                   'X-GitHub-Api-Version': '2022-11-28'})
    for attempt in range(3):
        try:
            with urlopen(request, timeout=30) as response:
                return json.load(response)
        except (HTTPError, URLError):
            if attempt == 2:
                raise NotifyError('Cannot read the Actions run status; credentials were not logged.') from None
            time.sleep(3)

def slack_text(run):
    conclusion = run.get('conclusion')
    labels = {'success': '성공', 'failure': '실패', 'cancelled': '취소', 'timed_out': '시간 초과'}
    result = labels.get(conclusion, conclusion or '감시 시간 내 미완료')
    sha = (run.get('head_sha') or '')[:7]
    url = run.get('html_url', '')
    if not url.startswith('https://github.com/'):
        raise NotifyError('Unexpected Actions result URL.')
    text = f"PickDoll 클라우드 빌드: {result}\n커밋: {sha}\n<{url}|Actions 결과·산출물 확인>"
    if conclusion == 'success':
        text += '\n결과 파일은 실행 페이지의 Artifacts에서 받을 수 있습니다.'
    elif conclusion in {'failure', 'timed_out'}:
        text += '\n재실행 전 Unity Build History에서 기존 빌드 상태를 확인해 주세요.'
    return text

def send(text):
    webhook = os.getenv('SLACK_WEBHOOK_URL', '')
    if not webhook.startswith('https://hooks.slack.com/services/'):
        raise NotifyError('SLACK_WEBHOOK_URL Secret is missing or invalid.')
    request = Request(webhook, data=json.dumps({'text': text}, ensure_ascii=False).encode('utf-8'),
                      headers={'Content-Type': 'application/json'}, method='POST')
    try:
        with urlopen(request, timeout=30) as response:
            if response.read().decode().strip() != 'ok':
                raise NotifyError('Slack returned an unexpected result; webhook URL omitted.')
    except (HTTPError, URLError):
        raise NotifyError('Slack delivery failed; webhook URL omitted.') from None

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--run-id', type=int, required=True)
    parser.add_argument('--timeout-minutes', type=int, default=210)
    args = parser.parse_args()
    if args.run_id <= 0:
        raise NotifyError('A positive Actions run ID is required.')
    deadline = time.monotonic() + args.timeout_minutes * 60
    previous = None
    while True:
        run = get_run(args.run_id)
        status = run.get('status')
        if status != previous:
            print('Actions run status: ' + str(status), flush=True)
            previous = status
        if status == 'completed' or time.monotonic() >= deadline:
            break
        time.sleep(40)
    send(slack_text(run))
    print('Slack notification delivered. Webhook and tokens were not printed.', flush=True)

if __name__ == '__main__':
    try:
        main()
    except NotifyError as exc:
        print('::error::' + str(exc), file=sys.stderr)
        sys.exit(1)
