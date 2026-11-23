[English](../README.md) | [Bahasa Indonesia](README.id.md) | [ไทย](README.th.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | **한국어** | [Deutsch](README.de.md)

# Lost and Found Co. - 로컬 화면 분할 협동 모드 (Split-Screen Co-op)

BepInEx 5 기반의 **Lost and Found Co.** (Windows x64) 로컬 화면 분할 협동 모드입니다. 한 화면에서 2명부터 4명까지 동시에 플레이할 수 있으며, 플레이어별 독립 카메라와 줌, 가상 커서, 입력 장치 분리, 컷신 자동 전체 화면 전환, 다국어 모드 UI를 지원합니다.

> **테스트 완료 게임 버전**: **Lost and Found Co. v1.2.2** (Steam, Windows x64). 게임 내부의 카메라, 입력 또는 조명 클래스가 크게 변경되지 않는 한 소규모 패치에서도 보통 호환됩니다.

## 주요 기능

- **2~4인 로컬 화면 분할**:
  - **2인**: 좌우 분할 또는 상하 분할 지원 (`F11`로 언제든지 전환 가능).
  - **3인**: 2x2 격자 + P4 참가 대기 카드 표시.
  - **4인**: 4분할 전체 2x2 화면.
- **입력 장치 분리 및 혼합 지원**:
  - 기본값으로 P1은 **마우스 + 키보드**를 사용하고 연결된 게임패드는 자동으로 P2~P4에 배정됩니다 (`F7`로 게임패드 1을 P1에 할당 가능).
  - 연결된 게임패드에서 `Start` 버튼을 누르면 즉시 참가할 수 있으며, `F6`을 눌러 단일 키보드로 P2 테스트(`IJKL` 커서, 방향키 화면 이동, `U`/`O` 줌, `Enter` 클릭)도 가능합니다.
- **독립 카메라 및 2D 레이캐스트**:
  - 각 플레이어 화면은 별도의 `WorldCamera`로 동작하며 개별 클릭/드래그와 줌을 지원합니다 (`Y` 또는 `L3`를 누른 상태에서 전체 화면 HUD 및 할 일 목록 조작 가능).
- **컷신 자동 전체 화면 전환 (`AutoFullscreenOnCutscene`)**:
  - 컷신, 스냅샷 이벤트, 대화 카메라 잠금, 층 이동 시 일시적으로 전체 화면으로 전환했다가 종료되면 자동으로 화면 분할로 복귀합니다.
- **URP HDR Bloom 및 SmartLighting2D 지원**:
  - HDR 포맷(`RGB111110Float`)과 `FunkyCode` SmartLighting2D 라이트맵 동기화로 분할 화면에서도 조명을 유지합니다.
- **게임 언어 연동 다국어 UI**:
  - 게임의 언어 설정(`en`, `th`, `zh-CN`, `zh-TW`, `ja`, `ko`, `de`, 인도네시아어 모드 설치 시 `id`, 예비 슬롯 `fr`, `es-ES`, `it`, `ru`, `sv-SE`, `tr`, `pt-BR`, `vi`)을 자동으로 따라가며 Windows 시스템 글꼴(`Malgun Gothic` 등)을 자동 선택합니다.

## 함께 제작한 모드 및 호환성 안내

함께 제작한 **[Lost and Found Co. 인도네시아어 로컬라이제이션 모드](https://github.com/AkmalHidayat/LostAndFound-IndonesianMod)**(`LostAndFound_Indonesian.dll` / `com.teteh.lostandfoundco.indonesian`, 총 16,718개 항목을 **Patch 1.2.2**까지 번역 지원하며 이후 추가되는 새 스테이지는 사전 업데이트 필요)와 충돌 없이 함께 동작합니다:

- 게임 언어가 인도네시아어로 설정되면 `F10` 제어 패널, 모서리 배지, 대기 화면 및 알림이 자동으로 인도네시아어로 전환됩니다.
- 2인 배치 전환 단축키는 `F11`만 사용하여 인도네시아어 모드의 사전 리로드 단축키(`F9`)와 겹치지 않습니다.
- 설치 스크립트(`Install_SplitScreenCoop.bat`)와 제거 스크립트(`Uninstall_SplitScreenCoop.bat`)는 기존 로더나 다른 플러그인을 덮어쓰거나 삭제하지 않습니다.

---

## 설치 방법

### 방법 1: 자동 설치 ZIP (권장)

1. [GitHub Releases](https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop/releases)에서 **`LostAndFound-SplitScreenCoop-v1.0.4-AutoInstall.zip`** 파일을 다운로드합니다.
2. 압축 파일의 모든 내용을 **Lost and Found Co.** 설치 폴더(`Lost and Found Co.exe` 옆)에 풉니다.
3. **`Install_SplitScreenCoop.bat`** 파일을 더블 클릭하여 실행합니다(설치기 안내는 영어로 표시됩니다).
   - BepInEx 5가 없다면 비어 있는 Unity Doorstop 프록시 슬롯(`winhttp.dll`)을 통해 기존 파일을 덮어쓰지 않고 설치합니다.
   - BepInEx 5나 다른 모드가 이미 설치되어 있다면 기존 로더를 건드리지 않고 `BepInEx\plugins\LostAndFound_SplitScreenCoop.dll`만 추가합니다.
4. Steam에서 게임을 평소처럼 실행합니다.

모드를 제거하려면 게임 폴더의 **`Uninstall_SplitScreenCoop.bat`**을 실행하세요. SHA-256 해시를 확인한 후 안전하게 삭제하며, 다른 플러그인이 남아 있으면 BepInEx는 유지됩니다.

### 방법 2: 수동 DLL 복사

이미 **BepInEx 5.4.x (x64)**가 설치되어 있는 경우:

1. [GitHub Releases](https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop/releases)에서 **`LostAndFound_SplitScreenCoop.dll`**을 다운로드합니다.
2. `<게임 폴더>\BepInEx\plugins\LostAndFound_SplitScreenCoop.dll` 위치에 복사합니다.

### 방법 3: 소스 코드에서 직접 빌드

자세한 방법은 [docs/BUILDING.md](BUILDING.md)를 참고하세요:

```powershell
git clone https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop.git
cd LostAndFound-SplitScreenCoop
powershell -ExecutionPolicy Bypass -File ".\scripts\build.ps1" -Deploy
```

---

## 단축키 및 게임패드 조작

| 키 | 동작 |
| --- | --- |
| `F8` | 화면 분할 협동 모드 켜기 / 끄기 |
| `F11` | 2인 분할 배치 전환 (좌/우 또는 상/하) |
| `F7` | P1 입력 전환 (마우스 + 키보드 / 게임패드 1) |
| `F10` | 협동 제어 패널 열기 / 닫기 |
| `F6` | P2 키보드 테스트 모드 전환 (`IJKL` 커서, 방향키 이동, `U`/`O` 줌, `Enter` 클릭) |

게임패드 조작(P2~P4 및 게임패드 1 모드 사용 시 P1): `왼쪽 스틱` 커서 이동, `오른쪽 스틱` 카메라 이동, `LT`/`LB` 및 `RT`/`RB` 줌, `A` 클릭/드래그, `Y` 또는 `L3` 홀드 시 전체 화면 HUD/할 일 목록 조작, `R3` 카메라 위치 초기화, `Start` 협동 참가.

---

## 설정 및 라이선스

- 설정 파일 경로: `<게임 폴더>\BepInEx\config\com.teteh.lostandfound.splitscreencoop.cfg` (`[UI]` 항목의 `UiLanguage`로 언어 고정 가능).
- 모드 소스 코드는 [MIT License](../LICENSE)(Copyright (c) 2026 AkmalHidayat)를 따르며, 자동 설치 ZIP에 포함된 BepInEx 5 / HarmonyX / Unity Doorstop 라이선스 고지는 [docs/THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)에서 확인할 수 있습니다.

> **면책 조항**: 이 모드는 비공식 커뮤니티 프로젝트이며 **Bit Egg Inc.**와 관련이 없습니다.
