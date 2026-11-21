# PickDoll Android — Unity

Unity 6.5 (`6000.5.5f1`) / URP 17.5.0 / Input System 1.19.0 / uGUI 2.5.0.

1주차는 **기계와 이동 조작을 검증하는 프로토타입**이다. 인형 집기, 획득, 도감, 저장, Google Play Games는 아직 구현하지 않는다. 화면의 곰 3개는 물리 검증 전 임시 모델이며 최종 인형이 아니다. UI는 시스템 글꼴의 재배포를 피하기 위해 Unity 기본 글꼴과 영문을 임시 사용한다.

## 열기

Unity Hub → Add → 이 `Unity` 폴더 선택 → `6000.5.5f1`로 연다. `Assets/PickDoll/Scenes/Week01.unity`를 열고 Play를 누른다. 씬을 다시 만들려면 `PickDoll > Generate Week 01 Scene`을 실행한다. 다시 생성하면 해당 씬의 수동 편집을 덮어쓰므로 기존 수정이 있다면 먼저 복사한다.

## 조작

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
