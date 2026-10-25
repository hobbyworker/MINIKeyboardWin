<p align="center"><img src="../.github/icon.png" width="128" height="128" alt="앱 아이콘"></p>

<h1 align="center">Windows용 매크로 키보드 설정 도구</h1>

<p align="center">
  <img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white">
  <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6">
  <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white">
  <a href="../LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"></a>
</p>

<p align="center"><a href="../README.md">English</a> · <b>한국어</b> · <a href="README.de.md">Deutsch</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a> · <a href="README.id.md">Bahasa Indonesia</a> · <a href="README.it.md">Italiano</a> · <a href="README.ja.md">日本語</a> · <a href="README.pl.md">Polski</a> · <a href="README.pt-BR.md">Português (Brasil)</a> · <a href="README.vi.md">Tiếng Việt</a> · <a href="README.zh-Hans.md">简体中文</a> · <a href="README.zh-Hant.md">繁體中文</a></p>

작은 USB 매크로 키보드에 키 설정을 쓰는 Windows 앱입니다. 노브 하나뿐인 것부터 키 21개까지, 노브는 4개까지로 팔리는
키보드들입니다. 키와 노브의 동작(왼쪽으로 회전 · 누르기 · 오른쪽으로 회전)마다 레이어 1–3 에 키 입력 · 멀티미디어 키 ·
마우스 동작을 정하고, 키보드의 조명도 정합니다. 같은 규칙으로 만든 macOS 앱의 Windows 판이고, 같은 명령을 씁니다.

| 키보드 | 배열 | 읽어 오기 | 지연 | 조명 | 실기 확인 |
|---|---|---|---|---|---|
| `1189:8890` | 사용자가 정함 — 키(12개까지) · 한 줄의 키 · 노브(2개까지) | 안 됨 | 없음 | 모드 셋 | 했음 — 키 6개 · 노브 하나 |
| 제조사 `1189` 또는 `514C`, 제품 `8840` · `8842` · `8830`–`8833` · `8850` (그리고 `514C:8851`), 새 펌웨어 | 키보드가 알려 준 대로 — 26가지 | 연결하면 바로 | 입력마다 | 모드 하나와 키 1–16 의 색 | 안 했음 |
| 같은 키보드, 옛 펌웨어 | 키보드가 알려 준 대로 | 요청할 때 | 키마다 하나 | 색과 모드 | 안 했음 |
| 같은 키보드, 종류를 알려 주지 않는 것 | 사용자가 고름 — 26가지 | 안 됨 | 없음 | 색과 모드 | 안 했음 |

`1189:8890` 은 제조사의 Windows 설정 도구 "MINI KeyBoard" 가 다루는 키보드이고, 나머지는 제조사의 여러 운영체제용 앱
"MINI_KEYBOARD" 가 찾는 키보드입니다. 앱은 두 도구의 명령을 모두 씁니다. 두 번째 쪽 키보드는 대개 키와 노브가 몇 개인지,
펌웨어가 무엇인지 알려 주므로 제조사 앱이 그리는 배열을 그대로 그리고, 알려 주지 않는 키보드는 그 배열 가운데 사용자가
고른 것을 씁니다. `1189:8890` 은 제조사 앱이 모르는 키보드이고 그 자신의 도구도 모양을 그리지 않아 실제 모양을 알 수
없습니다. 그래서 키와 노브가 몇 개인지, 한 줄에 키가 몇 개인지를 정하면 앱이 그대로 반듯하게 그립니다.

앱은 13개 언어로 나옵니다 — 영어 · 한국어 · 일본어 · 중국어(간체 · 번체) · 독일어 · 스페인어 · 프랑스어 · 이탈리아어 ·
포르투갈어(브라질) · 폴란드어 · 인도네시아어 · 베트남어. Windows 의 언어를 따르고(설정에서 직접 고를 수도 있습니다), 그 밖의
언어에서는 영어로 나옵니다.

이 저장소의 소스는 MIT 라이선스입니다. 배포되는 앱의 몇 가지 — 이름 · 아이콘 · 링크 — 는 이 소스에 없습니다. 맨 위의
아이콘은 보여 주려고 둔 것으로 MIT 가 아니라 CC BY-SA 4.0 이고([`LICENSES/CC-BY-SA-4.0.txt`](../LICENSES/CC-BY-SA-4.0.txt)), 이
저장소의 빌드에는 들어가지 않습니다.

## 내려받기

[Releases](https://github.com/hobbyworker/MINIKeyboardWin/releases) 에서 PC 에 맞는 설치 파일을 받아 실행합니다 — Intel · AMD 는
`MINIKeyboard-<버전>-x64-Setup.exe`, ARM 은 `-arm64-Setup.exe`. 관리자 권한 없이 사용자 폴더에 깔리고, 바탕 화면과 시작 메뉴에
바로 가기가 생깁니다. 설치 파일에 서명이 없어서 처음 실행할 때 Windows 가 "Windows의 PC 보호" 를 띄웁니다 — **추가 정보** 를
누른 뒤 **실행** 을 누릅니다.

앱은 스스로 업데이트합니다. 실행할 때와 하루에 한 번 새 버전을 찾고(**설정 ▸ 앱 ▸ 업데이트 자동 확인**), ⋯ ▸ **업데이트
확인…** 으로도 찾습니다. 새 버전이 있으면 창 위의 띠에서 **업데이트하고 다시 실행** 을 누릅니다. 설정은 그대로 남습니다.
Releases 의 `.nupkg` · `releases.*.json` 은 앱이 업데이트할 때 받는 파일이라 직접 받을 필요가 없습니다. 지울 때는 Windows 의
설정 ▸ 앱 ▸ 설치된 앱에서 지웁니다.

## 요구 사항

- Windows 10 버전 2004 이상 또는 Windows 11, x64 또는 ARM64
- 드라이버도 허용도 필요 없습니다. 두 종류 모두 HID 장치로 엽니다.
  - `1189:8890` 은 설정 통로(USB 인터페이스 1)로, 제조사 도구처럼 씁니다. 입력하는 내용은 읽지 않습니다.
  - 나머지 키보드는 제조사 앱처럼 첫 번째 USB 인터페이스로 씁니다. 그동안에도 키보드는 그대로 입력됩니다.

## 빌드

.NET 10 SDK(10.0.402 이상)가 필요합니다 — Visual Studio 는 없어도 됩니다. 앱은 Windows 에서만 빌드되고, 라이브러리 셋과
테스트는 어느 운영체제에서나 빌드됩니다.

```powershell
copy Brand.props.example Brand.props                                 # 처음 한 번 — 그리고 BrandName 을 적는다
dotnet build src/MINIKeyboard/MINIKeyboard.csproj -p:Platform=x64    # ARM PC 는 -p:Platform=ARM64
dotnet test --project tests/MINIKeyboard.Domain.Tests                # Data.Tests · Presentation.Tests 도 — 키보드는 건드리지 않는다
```

빌드한 앱은 `src/MINIKeyboard/bin/<x64 또는 ARM64>/Debug/net10.0-windows10.0.26100.0/MINIKeyboard.exe` 입니다.

앱의 이름 · 아이콘 · 링크는 저장소에 없습니다 — `Brand.props` 에 자기 것을 적습니다. 이름(`BrandName`)이 없으면 앱만 이름을 요구하며
멈추고, 라이브러리 · 테스트는 그대로 빌드됩니다. 아이콘 · 저작권 · 링크는 없어도 됩니다 — 아이콘이 없으면 Windows 의 기본 아이콘입니다.

빌드는 셋 가운데 하나입니다(`-p:Distribution=…`). 기본인 `Source` 에는 앱 안 업데이트가 없습니다. `Direct` 는
[Velopack](https://velopack.io) 으로 깔고 스스로 업데이트하는 판입니다 — `BrandUpdateUrl` 이 새 버전이 있는 곳으로, GitHub
저장소(그 Releases)나 Velopack 의 목록이 있는 다른 주소 · 폴더입니다. `Store` 는 Microsoft Store 판의 자리이고 아직 준비
중입니다.

⋯ ▸ **시뮬레이터** 로 그 키보드 없이 앱을 써 볼 수 있습니다. `1189:8890`, 새 펌웨어의 배열들, 종류를 알려 주지 않는
키보드 가운데 하나를 고르면 앱이 꽂힌 것처럼 같은 명령으로 동작하고, USB 로는 아무것도 나가지 않습니다. 쓰는 동안은 창 위의 주황
띠가 그렇다고 알리고, **USB에 꽂힌 키보드로 돌아가기** 로 돌아옵니다. Debug 빌드는 모든 종류(`1189:8890` 의 다른 리포트 ID ·
옛 펌웨어)를 고를 수 있고, `MINIKEYBOARD_SIMULATED_KEYBOARD`(예: `8890-3`)로 그 키보드로 시작합니다. ⋯ ▸ **장치 기록**
(Ctrl+Alt+L)에서 쓴 리포트와 받은 답을 모두 볼 수 있습니다.

## 쓰는 법

1. 키보드를 USB 로 연결합니다. 앱이 키보드를 찾고, 두 번째 쪽 키보드에는 종류를 묻습니다. 새 펌웨어면 바로 읽어 옵니다.
2. 모양을 알려 주지 않는 키보드는 처음에 배열 — 제조사 앱의 배열 가운데 하나, `1189:8890` 은 키 · 한 줄의 키 · 노브의
   개수 — 과 앞에 놓인 방향을 정합니다. 앱은 USB ID 마다 기억합니다. 나중에 **모양…** 과 **회전**(Ctrl+T)으로 바꿉니다.
   모양을 알려 준 키보드는 돌리기만 합니다.
3. 레이어를 고르고, 키보드 그림에서 키나 노브 동작을 고릅니다.
4. 동작을 줍니다.
   - **키** — 보조 키(Ctrl · Alt · Shift · Win 과 오른쪽 것들)를 잡고 키를 누릅니다. 누를 때마다 입력이 하나씩 붙습니다.
     **보조 키만** 은 그 키를 보조 키로 만듭니다. **QWERTY** 는 키를 풀사이즈 키보드처럼, **ABC** 는 묶음으로 늘어놓습니다.
     키보드가 받는 키만 누를 수 있습니다.
   - **멀티미디어** · **마우스** — 키에 있던 동작을 대신합니다.
   - **LED** — 레이어의 조명. 새 펌웨어는 키 1–16 가운데 하나를 골라 색을 줍니다.
   - **지연** — 지연을 기억하는 펌웨어에서.
5. **저장**(Ctrl+S)이 바뀐 것을 씁니다. **읽기**(Ctrl+R)는 읽어 올 수 있는 키보드에서 지금 설정을 가져옵니다.

⋯ ▸ **설정**(Ctrl+,)에서 화면 언어(13개 가운데 하나, 또는 Windows 의 언어)와 화면 모드(라이트 · 다크)를 고릅니다. 직접 받은
앱에는 **앱 ▸ 업데이트 자동 확인** 도 있습니다. 맨 아래의 **정보** 에 버전 · 링크와 **함께 들어 있는 소프트웨어**(앱에 함께 들어
있는 소프트웨어의 라이선스)가 있습니다.

**설정 ▸ 펌웨어 버그 우회** 의 **조명을 바꾼 뒤 키 반복 막기** 는 처음부터 켜져 있습니다. `1189:8890` 은 조명을 저장하면 그
뒤로 키의 첫 입력이 끝없이 반복되는 펌웨어 버그가 있어서(제조사 프로그램도 같습니다), 앱이 조명 뒤에 키 없는 저장을 한 번
더 보냅니다. 끄면 제조사 프로그램과 똑같이 보냅니다. 이 키보드는 뽑았다 꽂으면 조명이 꺼집니다.

## 구조

규칙은 라이브러리 셋에 있습니다. 의존은 안쪽으로만 향하고, 바깥으로 손을 뻗는 레이어는 컴파일되지 않습니다.

```
src/
├── MINIKeyboard.Domain/        키보드 · 동작 · 배열의 컨트롤 · 키보드마다 담을 수 있는 것, 포트, 유스케이스 (.NET 기본 라이브러리만)
├── MINIKeyboard.Data/          키보드마다의 명령과 컨트롤의 번호, HID 로 키보드 찾기와 쓰기(SetupAPI · hid.dll), 시뮬레이터 키보드
├── MINIKeyboard.Presentation/  뷰모델과 화면이 그리는 값 — 배열의 그림, 키 · 색 표 (UI 프레임워크 없음)
├── MINIKeyboard.Views/         WinUI 3 — Presentation 만 본다. 언어마다의 번역 표(Translations/)
└── MINIKeyboard/               조립(composition root) — 모든 프로젝트를 아는 유일한 곳. Direct/ 는 직접 배포 판에만(Velopack)
tests/                          xUnit — 명령의 바이트, 배열, 편집, 편집기
Brand.props.example             이름 · 아이콘 · 링크 · 판마다의 값 — Brand.props 로 복사해 채운다
LICENSES/                       맨 위 아이콘의 라이선스 (CC BY-SA 4.0)
```

경고 0 이 기준입니다. 경고는 끄지 않고 고칩니다.

## 한계

- 쓰기는 지금까지 키보드 한 대, 키 6개 · 노브 하나의 `1189:8890` 으로 Windows 11 의 ARM64 · x64 PC
  둘 다에서 확인했습니다. Windows 10 에서는 아직입니다. 나머지 키보드는 제조사 앱이 쓰는 대로 씁니다 — 명령은 macOS 앱의 것과 바이트까지 같고 테스트가
  지키지만, 실기로는 아직 하나도 확인하지 못했습니다.
- `1189:8890` 과 종류를 알려 주지 않는 키보드는 지금 설정을 알려 주지 않으므로, 앱에서 정한 것만 보입니다.
- 한 번에 키보드 하나. 여럿을 꽂으면 먼저 찾은 것 — `1189:8890` 이 먼저 — 을 씁니다.
- 마우스 이동과 끌기는 없습니다.

## 라이선스

MIT — [LICENSE](../LICENSE) 를 보세요. 맨 위의 아이콘은 CC BY-SA 4.0 — [LICENSES/CC-BY-SA-4.0.txt](../LICENSES/CC-BY-SA-4.0.txt) 를 보세요.

앱에는 Microsoft 의 구성 요소 — Windows App SDK 와 Windows SDK for .NET — 도 함께 들어 있고, 이것들은 MIT 가 아니라
Microsoft 의 사용 조건을 따릅니다. 그 조건은 앱의 설정 ▸ 정보 ▸ **함께 들어 있는 소프트웨어** 에 있고, 앱을 쓰면 그 구성
요소에 대해 그 조건을 받아들이는 것입니다.
