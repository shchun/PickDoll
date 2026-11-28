# PickDoll Android — Unity

현재 작업 씬은 `Assets/PickDoll/Scenes/Week09.unity`다. 획득 피드백·음향/진동 설정과 사진 기반 집게·외장의 PC 최종 검사를 통과했다. S26에 0.9.0 / code 11을 설치·실행하고 기존 영수증 20개 보존 및 진동 권한을 확인했다. 품평에서 집계·성공 효과·진동 체감·미세 회전 개선 요청을 받았고, 사용자 지시로 10주차에 반영한다. 9주차 현재 범위로 PR을 준비한다. `-Week 9` 빌드와 `tools/Test-Week09.ps1`을 사용한다. [9주차 보고서](../docs/android/reports/android-week-9-results.html)에 실제 검증 상태를 기록한다. 아래 설명은 이전 검증 버전의 기록이다.

Unity 6.5 (`6000.5.5f1`) / URP 17.5.0 / Input System 1.19.0 / uGUI 2.5.0.

다음 1주차 설명은 초기 구현 기록이다. 1주차는 **기계와 이동 조작을 검증하는 프로토타입**이다. 인형 집기, 획득, 도감, 저장, Google Play Games는 아직 구현하지 않는다. 화면의 곰 3개는 물리 검증 전 임시 모델이며 최종 인형이 아니다. UI는 시스템 글꼴의 재배포를 피하기 위해 Unity 기본 글꼴과 영문을 임시 사용한다.

## 현재 7주차 실행

Unity Hub에서 `Assets/PickDoll/Scenes/Week07.unity`를 실행한다. `tools/Run-Unity.ps1 -Week 7 -Task Generate|Validate|Windows|Android`를 지원한다.

- 조이스틱: 드래그량에 따른 이동과 미세 조정. 시작 시 배출구 위이므로 이동한 뒤 GRAB
- GRAB: 자동 집기·복귀·배출 / NEW ROUND: 7개 재배치, 누적 기록 유지
- 한 손가락: 시점 / 두 손가락: 1–2배 줌 / VIEW: 시점 전환·줌 초기화
- Windows: 방향키·Shift·Enter/Space·R·마우스 드래그·휠 줌
- 검사: `tools/Test-Week07.ps1`, UI 비율 검사 `-UiOnly -Width 906 -Height 1088`
- 실행 검사 파일: `Builds/Week07/Windows/Checks/`; 저장 검사는 별도 임시 폴더로 격리해 실제 수집 기록을 건드리지 않음
- 로컬 저장: `Application.persistentDataPath/collection-v1.json`, 이전 유효 파일 `.bak`. 총수와 종류별 수는 영수증에서 계산
- 앱 데이터 삭제·재설치·다른 기기 복원은 아직 지원하지 않음. 일반 덮어쓰기 설치와 프로세스 재실행에서는 유지
- 인형 데이터: `Assets/PickDoll/Prizes/`, 씬 생성과 임시 모델: `WeekSevenBuilder`

## 1주차 기록: 열기

Unity Hub → Add → 이 `Unity` 폴더 선택 → `6000.5.5f1`로 연다. `Assets/PickDoll/Scenes/Week01.unity`를 열고 Play를 누른다. 씬을 다시 만들려면 `PickDoll > Generate Week 01 Scene`을 실행한다. 다시 생성하면 해당 씬의 수동 편집을 덮어쓰므로 기존 수정이 있다면 먼저 복사한다.

## 1주차 기록: 조작

- 방향 버튼 길게 누르기 / PC 방향키: 기계 정면 기준 이동. BACK/↑는 뒤쪽(+Z), FRONT/↓는 앞쪽(-Z).
- PRECISION / PC Shift: 저속 이동. 1주차는 연속 저속 방식이며 1mm 단위 누르기 이동은 후속 작업이다.
- TURN L/R / PC Z/X: 집게 수직축 회전.
- 기계 화면 드래그: 연속 시점 변경. VIEW: 고정 시점 전환.
- RESET / PC R: 이동 위치와 회전 초기화.
- 조준원은 상단 이동 장치 바로 아래를 표시한다.

## 코드

| 위치 | 역할 |
| --- | --- |
| `Assets/PickDoll/Scripts/Runtime/MachineController.cs` | 이동 범위·속도·기계 좌표·입력 해제 |
| `HoldControl.cs` | 다중 터치 방향 버튼과 눌림 상태 |
| `OrbitView.cs` | 드래그 카메라·화면 비율에 따른 거리 |
| `PrototypeHud.cs` | 터치 UI·Safe Area·화면 크기 변경 |
| `Assets/PickDoll/Scripts/Editor/WeekOneBuilder.cs` | 씬 생성·Android/Windows 빌드 |
| `WeekOneChecks.cs` | 에디터 씬에서 이동·범위·조준원·UI 검증 |

## 빌드·검증

Unity 메뉴의 `PickDoll > Validate Week 01 Scene`로 이동 로직과 씬 연결을 검사한다. 실제 UI 초기화는 빌드된 플레이어에서 별도로 확인한다. 결과는 `Builds/Checks/week01-validation.txt`와 Console에 남는다. 성공 여부는 실제 실행 로그로 확인한다.

`tools/Run-Unity.ps1 -Task Generate|Validate|Windows|Android`로 에디터를 닫은 상태에서 CLI로 실행할 수도 있다. `Generate`는 씬 재생성, `Validate`는 에디터 씬 검증이다. 생성물과 로그는 Git에서 제외한다.

Android는 Unity Hub에서 **Android Build Support + Android SDK & NDK Tools + OpenJDK**를 설치해야 한다. 임의의 시스템 Java/NDK를 조합하는 대신 Unity 권장 도구를 우선 사용한다. ARM64 / IL2CPP / API 26 이상 / 임시 패키지명 `com.precipi.pickdoll` / 버전 `0.1.0` / code 1. 공개 출시 이름·패키지명·최소 API는 추후 확정한다.

- `PickDoll > Build Android APK`: `Builds/Android/PickDoll-week01.apk`
- `PickDoll > Build Windows Preview`: `Builds/Windows/PickDoll.exe`
- APK는 개발용 디버그 서명이다. 공개 출시용 서명·AAB는 후속 작업이다.
- APK 생성 후 `tools/Install-Apk.ps1 -Serial <adb기기번호>`로 사용자가 지정한 기기에 설치한다. USB 디버깅 허용 창은 휴대폰에서 직접 승인한다.

## 실기기 체크

S26 울트라와 폴드5 각각에서 시작·종료·재실행, 네 방향 이동, 경계, 저속 이동, 회전, 드래그 시점, 앱 전환 후 입력 해제를 확인한다. 폴드5는 외부·내부 화면 및 접고 펼치기를 확인한다. 기기가 연결되지 않은 경우 실기기 통과로 기록하지 않는다.

## 후속 작업

2주차: 집게 하강·닫힘·접촉·상승·운반을 우선 검증한다. 이후 배출·획득, 인형 확장, 도감·업적·클라우드 저장 순으로 진행한다. Play Games는 Play Console 게임 리소스, 패키지명·서명 인증서 지문, 테스트 계정 구성이 필요하다. 이번 주에는 실제 계정 리소스를 만들지 않는다.

첫 Android APK는 모바일 URP 기본 설정과 OpenGL ES 3로 빌드한다. Vulkan은 초기 조작 검증 뒤 성능 비교 대상으로 남긴다. PC 미리보기는 템플릿의 PC 품질 설정이므로 조명 표현이 다를 수 있다.

## 1주차 실기기 검증 메모

S26 울트라와 폴드5에 0.1.0(code 1) 개발용 APK를 설치·실행했다. S26은 화면 캡처 및 사용자 정상 이동 피드백을 확인했고, 폴드5도 정상 동작 피드백을 받았다. 폴드5는 접고 펼친 뒤에도 버튼 배치·이동·드래그가 정상이라는 사용자 확인을 받았다. 추가로 갤럭시 탭 SM-X820에도 동일 APK를 설치하고 화면·정상 동작 피드백을 확인했다.

시작 로그에는 Google Play Asset Delivery의 AssetPackManager 클래스 누락 오류가 기록된다. 현재 앱은 자산 분할 다운로드를 사용하지 않고 화면·이동은 정상 확인했지만, 로그 오류가 없는 상태로 간주하지 않는다. 공개 출시 전 엔진 버전·배포 설정과 원인을 재검증한다. Google Play Games 업적 연동과는 별도 기능이다.


## 2주차 집기 물리 프로토타입

`Assets/PickDoll/Scenes/Week02.unity`에서 100g 임시 곰 3개를 관절·접촉·마찰로 집는다. 방향 이동 → GRAB(Enter/Space) → 자동 하강·닫힘·상승 → 수동 운반 → OPEN 순서다. RESET(R)은 인형과 집게를 초기화한다. 인형을 집게에 붙이는 고정 조인트는 사용하지 않는다.

- `tools/Run-Unity.ps1 -Week 2 -Task Generate|Validate|Windows|Android`
- Windows: `Builds/Week02/Windows/PickDoll.exe`
- Android: `Builds/Week02/Android/PickDoll-week02.apk` (0.2.0 / code 2)
- 설치: `tools/Install-Apk.ps1 -Week 2 -Serial <adb기기번호>`
- Windows 실행 검증: EXE에 `--pickdoll-week02-test` 인자를 주면 실제 플레이어 물리 검사 후 종료한다. 결과·스크린샷은 실행 파일 옆 `Checks/`에 생성한다.

정렬한 3개 인형의 집기·기본 속도 운반·놓기, 동작 중 초기화와 빈 공간 집기를 검증했다. 약한 힘은 관찰 조건이며 강제 낙하를 요구하지 않는다. 갤럭시 탭 설치·실행은 완료했으며 실제 터치 집기 사용자 확인은 대기 중이다. 자세한 수치와 화면은 [2주차 보고서](../docs/android/reports/android-week-2-results.html)에 있다.

수집함·획득 판정·저장은 아직 없다. 하강 중 이동, 수동 회전 제거와 관성 회전, 집게 디자인 개선, 구석 인형 집기는 3주차 시작 시 다시 검토·적용한다.


## 3주차: 매달린 집게와 구석 접근

`Assets/PickDoll/Scenes/Week03.unity`는 별도 씬이다. GRAB 후 내려가는 동안에도 방향·PRECISION 조작이 가능하다. 닫힘·상승·열림 중에는 수평 이동을 잠그고, 상승 완료 후 수동 운반·OPEN을 유지한다. 자동 복귀·하부 배출은 5주차 예정이다.

- TURN 버튼과 Z·X 회전 입력은 3주차에서 제거했다. 방향은 계속 기계 정면 기준이다.
- 승강하는 kinematic 지지점에 dynamic 집게 허브를 ConfigurableJoint로 매단다. 중력·질량·가감속·관절·감쇠가 흔들림과 회전을 만든다. 원형 흔들림을 애니메이션으로 강제하지 않는다.
- 매달림은 길이 0.14m의 강체 관절 근사이며, 실제로 휘는 줄 모델은 아니다. 기울기 제한은 각 축 약 28도, 수직축 회전은 자유다. 물리는 100Hz, 방향 입력은 FixedUpdate에서 적용한다.
- 이동 범위를 ±0.18m에서 ±0.345m로 넓혔다. 집게는 원통형 하우징·관절 핀·가는 곡선형 블레이드와 고무 끝단으로 구성하고 충돌체도 같은 경로를 따른다. 벽 충돌과 손가락별 HingeJoint를 유지한다.
- 집게가 인형을 고정하거나 강제로 떨어뜨리는 로직은 없다. 벽·구석의 모든 조준에서 성공을 보장하지 않는다.

빌드: `tools/Run-Unity.ps1 -Week 3 -Task Generate|Validate|Windows|Android`. 출력은 `Builds/Week03/Windows/PickDoll.exe` 및 `Builds/Week03/Android/PickDoll-week03.apk` (0.3.0 / code 3). 설치는 `tools/Install-Apk.ps1 -Week 3 -Serial <기기번호>`. Windows 실제 물리 검사는 `--pickdoll-week03-test` 인자로 실행하며 `Checks/physics-runtime.txt`와 화면을 남긴다. 테스트는 중앙·네 벽·네 모서리에서 접근과 여러 손가락 닫힘, 벽 안쪽 유지, 놓기, 원형 운동·감쇠 및 초기화를 확인한다.

Windows 미리보기는 모바일 품질 단계를 기본으로 맞췄다. 플랫폼 기본 품질과 빌드 리소스의 불일치로 발생하던 SSAO 리소스 누락을 피하고 Android와 같은 기본 렌더러로 검증한다.

3주차 최종 Windows 실행 검사 PASS: 9개 지정 위치 중 6회 들어 올림, 들어 올린 인형 모두 OPEN 후 낙하, 미세 운반·직선 반전·원형 흔들림·정지 감쇠·빈 집게 세 손가락 닫힘·RESET 확인. 폴드5에 APK 0.3.0(code 3)을 설치하고 사용자의 정상 동작 확인을 받았다. [3주차 보고서](../docs/android/reports/android-week-3-results.html)에 최종 화면·실기기 화면·검사 원문을 기록했다. 이번 버전에서도 AssetPackManager 시작 로그는 관찰됐다. 일부 구석의 단일 조준 실패는 남아 있어 4주차에 일관성을 튜닝한다.


## 4주차: 물리·속도·그립 안정화

`Week04.unity`는 3주차를 보존한 별도 씬이다. 집게의 벌어진 중간 부분을 종 모양 곡선으로 좁히고 고무 끝단 폭과 닫힘 목표를 조정했다. 입력 해제와 미세 이동은 즉시 반응하며, 일반 출발·반전 및 승강 가감속을 완만하게 처리한다. 최대 조준 0.32 / 수동 운반 0.40 / 저속 0.035 / 하강 0.30 / 상승 0.42 월드 단위/초다. 자동 복귀는 5주차다.

GRAB에서 25% 확률로 약한 범주(스프링 강도 배수 0.40–0.65)를, 나머지는 일반 범주(0.95–1.05)를 선택한다. 배수는 집기·상승·운반 중 고정된다. 약한 범주의 선택 확률이 실패 확률을 뜻하지 않으며, 물리적으로 받쳐지면 유지할 수 있다. OPEN은 정상 열기 강도로 동작한다.

- `tools/Run-Unity.ps1 -Week 4 -Task Generate|Validate|Windows|Android`
- `Builds/Week04/Windows/PickDoll.exe`, `Builds/Week04/Android/PickDoll-week04.apk` (0.4.0 / code 4)
- `tools/Install-Apk.ps1 -Week 4 -Serial <기기번호>`
- Windows 실제 입력·물리 검사: `--pickdoll-week04-test`, 결과는 실행 파일 옆 `Checks/physics-runtime.txt`

최종 PC 검사는 정지 조준·일반 배수 1.0의 18회 모두 집기·OPEN을 통과했다. 실제 플레이 승률과 다르며, 이동 직후 실패와 약한 힘으로도 유지되는 사례를 따로 관찰했다. 지지점까지 함께 초기화하는 새 검사 방식은 3주차 보고서의 수치와 직접 비교하지 않는다. 자세한 조건과 실기기 상태는 [4주차 결과](../docs/android/reports/android-week-4-results.html)에 기록한다.

S26 울트라 SM-S948N에 0.4.0(code 4) 설치·실행 및 사용자 정상 동작·조작감 확인 완료. APK v2 서명을 검증했고 실기기 화면을 보고서에 추가했다. 기존 AssetPackManager 시작 로그는 이번 버전에서도 관찰돼 공개 출시 전 추적 대상으로 남긴다.

## 5주차: 자동 복귀·하부 배출·세션 획득

하강 전 열림 준비 → 하강·닫힘·상승 → 배출 구멍 복귀 → 높은 위치에서 흔들림 대기 → 자동 열기 → 중앙 대기로 진행한다. 준비·하강 중 조준은 가능하고 자동 복귀·배출 중 입력은 잠근다. 충분히 열리지 않으면 공간 확보 안내 후 하강을 중단한다.

실제로 분리한 바닥 판의 구멍과 빈 캐비닛·경사 통로를 거쳐 인형이 앞 아래 투명 받침대로 나온다. 굴러 들어가도 통로를 거쳐 인형 충돌체 전체가 외부 확인 지점을 통과하면 획득한다. 표식별 1회 집계하며 `PrizeReceiver.Acquired` 이벤트를 제공한다. NEW ROUND / R은 새 재고 표식을 부여하고 세션 획득 수를 유지한다. 앱 재실행 후 보존은 7주차 로컬 저장에서 구현한다.

- `tools/Run-Unity.ps1 -Week 5 -Task Generate|Validate|Windows|Android`
- `Builds/Week05/Windows/PickDoll.exe`, `Builds/Week05/Android/PickDoll-week05.apk` (0.5.2 / code 7)
- 설치: `tools/Install-Apk.ps1 -Week 5 -Serial <기기번호>`
- Windows 검사: `--pickdoll-week05-test`, 실행 파일 옆 `Checks/delivery-runtime.txt` 및 화면 생성

자동 획득 3회, 세 가지 낙하 자세, 굴림·조기/중복 집계 방지, 빈 사이클 복귀, 닫힌 상태의 하강 전 열림을 실제 플레이어에서 확인했다. 에디터에서는 3개 연속 적재도 확인했다. 자세한 결과와 실기기 상태는 [5주차 보고서](../docs/android/reports/android-week-5-results.html)에 있다.

피드백 반영: 출구 구멍 네 면의 투명 가드, 전시 공간 크기를 유지한 하부 캐비닛 높이 절반 축소, 56 크기의 흰색 굵은 획득 숫자와 짙은 배경. 중심점 기반 감지의 누락을 줄이도록 인형 충돌체로 통로 통과를 판정하며, 전체 몸체가 앞면 밖으로 나와야 획득한다. 최종 0.5.2를 S26에 설치했고 시작 관절 안정성과 큰 카운터 화면을 확인했다. 실제 배출·카운트 증가 사용자 재확인은 대기 중이다.

0.5.2에서는 축소 후 장면에 저장되는 집게·매달림 기준점의 좌표를 수정했다. 재배치 전 시작 안정성 검사와 낮아진 조작판 아래 통과 검사를 포함한 최종 Windows 실행 검사 PASS. 배출 통로 시작 높이는 0.19로 낮춰 머리 걸림을 줄였다.

갤럭시 탭 SM-X820에도 동일한 0.5.2(code 7)를 설치·실행하고 집게·큰 카운터·조작 패널 표시를 확인했다. 실제 배출·집계 사용자 재확인은 별도 대기 중이다.

## 6주차 구현: 집게·터치 조작·상단 시야 개선

최신 추가 요구사항은 [전체 계획의 6주차 항목](../docs/android/plans/android-project-plan.html#week6-feedback)에 기록했다. 이번 산출물의 구현 범위와 검증 결과는 6주차 보고서에 기록했다.

- 갈고리 길이를 현재의 약 67%로 줄이고 실제 끝단 개방 직경 확대. 충돌체·하강 높이·그립 함께 재검증
- 관성 회전은 유지하고 정지 후 흔들림의 감쇠 강화
- 방향 버튼과 PRECISION 토글을 터치 드래그 조이스틱으로 교체. 드래그량으로 미세 이동, 큰 GRAB과 조작부 디자인 개선
- 두 손가락 핀치 줌: 기본 1배부터 최대 약 2배, VIEW에서 기본 배율 복귀. 카메라 드래그·조이스틱·GRAB과 제스처 충돌 방지
- 기계 상단 판넬 투명도 대폭 증가, 모서리 기둥 축소

주당 6시간 기준으로 기존 6주차의 저장·임시 3종·7개 랜덤 배치는 7주차로 이동하고 후속 일정도 한 주씩 조정했다. 이번 6주차에서 구현·PC 검증 및 실기기 품평을 거쳐 현재 상태를 산출물로 마감했다.

제품성·유료화 검토는 [전체 계획서](../docs/android/plans/android-project-plan.html#monetization)에 정리했다. 광고·판매 방식·가격은 미정이며 조작감과 자발적 재방문 검증을 우선한다. 현재 구현 범위에는 광고·결제를 추가하지 않는다.

6주차 최종 Windows 입력·줌·물리 검사와 폴드 외부·내부·태블릿 비율 모사 UI 검사 PASS. S26 울트라에 0.6.0(code 8)을 설치·실행하고 정상 시작 화면을 확인했다. 실기기 조작감의 정량 평가는 별도로 수행하지 않았다. APK v2 서명 검증 완료.

S26 실제 플레이 로그에서 인형 2개 배출과 누적 1→2 증가를 확인했고, 게임 화면의 누적 2 표시도 확인했다. 실기기 증거는 실행 화면과 실제 획득 로그다.

갤럭시 탭 SM-X820에도 최종 0.6.0(code 8) 설치·실행 및 버전 확인을 완료했다. 이후 캡처 중 USB 연결이 끊겨 탭 화면 캡처·사용자 조작감 확인은 대기 중이다.

## 7주차 품평 반영: 최초 시작 위치

앱 최초 시작 시 집게를 중앙 대신 배출구 중심 위의 상단 대기 위치에 배치한다. 본체·손가락·매달림 기준점·이동 장치·조준원을 함께 초기화해 관절 튐을 방지하고 이동 후 집기·배출을 검증한다. [다음 주차 요구사항](../docs/android/plans/android-project-plan.html#week7-feedback)에 기록했으며 이번 6주차 실행 버전에는 아직 적용하지 않았다.

7주차 추가 품평: 상단 왼쪽 타이틀·오른쪽 COLLECTED를 축소하고 그 외 영역을 투명하게 한다. 카메라가 상단 뒤까지 렌더링하도록 영역을 함께 조정하고, 숫자 가독성·텍스트 잔상·핀치/드래그 입력 간섭을 검증한다.

## 10주차 진행

최종 받침대 바닥 접촉 획득, 흑백 토끼 마스코트·인형, 천 표면·작은 눌림, 중앙 축하 연출·이중 진동과 미세 관성 회전을 적용한다. Windows 통합 검사·Android 빌드 및 서명 검사 통과. 갤럭시 탭 설치·실행과 기존 27개 기록 보존 확인, 실기기 품평 대기. [10주차 진행 기록](../docs/android/reports/android-week-10-results.html).

`./tools/Run-Unity.ps1 -Week 10 -Task Windows` 또는 `-Task Android`로 빌드하고, `./tools/Test-Week10.ps1`로 Windows 통합 검사를 실행한다.

## 11주차

신규 인형 4종을 추가해 기존 인형과 7종·7개로 구성합니다. COLLECTION은 미획득 실루엣·종류별 수·드래그 회전 미리보기를 제공합니다. 도감에서 물리와 게임 입력을 일시 정지하며 콤보 시간은 실시간 기준으로 흐릅니다. 새 판·앱 전환 시 콤보만 종료하고 수집 기록은 유지합니다.

실제 획득 사이 10초 이내면 2 COMBO부터 증가합니다. 성공 카드 큐와 독립적으로 계산하며 같은 획득 ID는 중복 집계하지 않습니다. 토출구 경사로와 윗턱을 조정해 긴 토끼가 서서 통과할 여유도 확보했습니다.

`./tools/Run-Unity.ps1 -Week 11 -Task Windows` 또는 `-Task Android`로 빌드합니다. `./tools/Test-Week11.ps1`은 물리·도감·콤보 통합 검사, `-UiOnly -Width 876 -Height 1400`은 도감 안전 영역 검사, `-Targeting`은 당근 토끼의 별도 접근 위치 검사입니다.

[11주차 진행 기록](../docs/android/reports/android-week-11-results.html) · [인형 확장 계획](../docs/android/plans/android-project-plan.html#week11-plush-feedback)

Week 11 품평 보완(0.11.1): 배출 완료 인형은 1.4초 뒤 정리하고, 7개 모두 배출된 후 자동 보충합니다. 성공·콤보·보충 카드는 게임 영역 중앙에 표시합니다. 원본 아케이드 효과음 7개는 `tools/Generate-ArcadeAudio.py`(numpy/scipy)로 재생성하며 SOUND 설정에 연동됩니다.

## 12주차 · 토끼 장난감집

1번 시안을 기준으로 얇은 둥근 프레임·작은 귀·크림/핑크 외장과 얕은 조작 선반을 적용합니다. 높은 시점에서는 앞 간판이 반투명해지고 카메라는 전시 공간을 더 크게 보여줍니다. 새 장식은 렌더링 전용이며 기존 집기·배출 충돌 구조를 보존합니다.

`./tools/Run-Unity.ps1 -Week 12 -Task Preview`는 플레이어 빌드 없이 에디터 미리보기와 배출·안정성 검사를 진행합니다. `-Task Windows` / `-Task Android`는 마감 빌드, `./tools/Test-Week12.ps1`는 기존 게임플레이 회귀 검사입니다.

[12주차 진행 기록](../docs/android/reports/android-week-12-results.html) · [선택 시안](../docs/android/plans/cabinet-concepts.html)

## GitHub Actions 클라우드 빌드

GitHub Actions의 `Unity Cloud Build`를 수동 실행하면 Unity 공식 클라우드에서 커밋된 씬을 Android·Windows 순서로 빌드하고 결과 파일·체크섬을 7일 보관합니다. 로컬 PC는 빌드하지 않습니다. [설정·자격 정보·실행 안내](../docs/android/setup/github-actions.html)를 참고하세요.


## Week 13 local checks

`tools/Run-Unity.ps1 -Week 13 -Task Validate` runs the claw constraint comparison and 15-second combo boundaries without a player build.

`tools/Test-Week13.ps1` checks actual grabbing, delivery, collection and restocking in the Windows player.

`tools/Get-DeviceDiagnostics.ps1 -Serial <device>` retrieves the two bounded local diagnostic files. Match screenshot timestamps with the ISO timestamp and UTC offset in each line. No automatic upload occurs.

Week 13 Android builds also run `python tools/Verify-AndroidIcons.py Builds/Week13/Android/PickDoll-week13.apk` (requires Pillow). This checks the packaged adaptive icon layers at every density so an opaque foreground cannot hide the mascot.
