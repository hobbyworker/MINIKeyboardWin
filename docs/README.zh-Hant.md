<p align="center"><img src="../.github/icon.png" width="128" height="128" alt="App 圖像"></p>

<h1 align="center">Windows 巨集鍵盤設定工具</h1>

<p align="center">
  <img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white">
  <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6">
  <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white">
  <a href="../LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"></a>
</p>

<p align="center"><a href="../README.md">English</a> · <a href="README.ko.md">한국어</a> · <a href="README.de.md">Deutsch</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a> · <a href="README.id.md">Bahasa Indonesia</a> · <a href="README.it.md">Italiano</a> · <a href="README.ja.md">日本語</a> · <a href="README.pl.md">Polski</a> · <a href="README.pt-BR.md">Português (Brasil)</a> · <a href="README.vi.md">Tiếng Việt</a> · <a href="README.zh-Hans.md">简体中文</a> · <b>繁體中文</b></p>

這是一款將按鍵設定寫入小型 USB 巨集鍵盤的 Windows App。這類鍵盤在市面上從只有一個旋鈕到多達 21 個按鍵都有，旋鈕最多 4 個。可以為每個按鍵和旋鈕的動作（向左旋轉、按下、向右旋轉）在第 1–3 層設定按鍵輸入、多媒體鍵或滑鼠動作，也可以設定鍵盤的燈光。它是依相同規則打造的 macOS App 的 Windows 版，使用相同的指令。

<p align="center"><img src="../.github/devices.png" width="560" alt="兩台小型 USB 巨集鍵盤：6 個按鍵和 1 個旋鈕，12 個按鍵和 2 個旋鈕"></p>

| 鍵盤 | 佈局 | 讀取 | 延遲 | 燈光 | 實機驗證 |
|---|---|---|---|---|---|
| `1189:8890` | 由使用者設定：按鍵（最多 12 個）、每列按鍵數、旋鈕（最多 2 個） | 不支援 | 無 | 三種模式 | 已驗證（6 個按鍵、1 個旋鈕） |
| 廠商 `1189` 或 `514C`，產品 `8840`、`8842`、`8830`–`8833`、`8850`、`8851`，新版韌體 | 依鍵盤告知的佈局（26 種） | 連接後立即讀取 | 每個輸入 | 一種模式，以及按鍵 1–16 的顏色 | 已驗證（`514C:8850`，12 個按鍵、2 個旋鈕） |
| 同類鍵盤，舊版韌體 | 依鍵盤告知的佈局 | 連接後立即讀取 | 每個按鍵一個 | 顏色和模式 | 未驗證 |
| 同類鍵盤，不告知類型者 | 由使用者選擇（26 種） | 不支援 | 無 | 顏色和模式 | 未驗證 |

`1189:8890` 是廠商的 Windows 設定工具「MINI KeyBoard」所支援的鍵盤，其餘則是廠商的跨平台 App「MINI_KEYBOARD」會尋找的鍵盤。本 App 兩種工具的指令都會使用。第二類鍵盤大多會告知自己有幾個按鍵和旋鈕、使用哪種韌體，因此 App 會照樣繪製廠商 App 所繪製的佈局；不告知的鍵盤則採用使用者從這些佈局中選擇的那一種。`1189:8890` 是廠商 App 不認得的鍵盤，它自己的工具也不繪製外形，因此無從得知實際形狀。所以只要設定按鍵和旋鈕的數量以及每列的按鍵數，App 就會照此繪製出整齊的排列。

App 提供 13 種語言：英文、韓文、日文、中文（簡體、繁體）、德文、西班牙文、法文、義大利文、葡萄牙文（巴西）、波蘭文、印尼文和越南文。App 會依照 Windows 的語言顯示（也可以在設定中選擇），其他語言則顯示英文。

本儲存庫的原始碼採用 MIT 授權條款。發佈的 App 有一部分（名稱、圖像和連結）不包含在此原始碼中。最上方的圖像僅供展示，它不採用 MIT 授權條款，而是採用 CC BY-SA 4.0 授權條款（[`LICENSES/CC-BY-SA-4.0.txt`](../LICENSES/CC-BY-SA-4.0.txt)），以本儲存庫建置的 App 也不包含它。

## macOS、Windows、Linux 和網頁

MINIKeyboard 有四個依相同規則製作的版本——相同的指令、相同的 13 種語言。三個 App 支援相同的鍵盤；在瀏覽器中執行的網頁版支援其中除 `1189:8890` 以外的鍵盤。

| 儲存庫 | GitHub |
|---|---|
| **MINIKeyboardApple**<br><img alt="macOS 14+" src="https://img.shields.io/badge/macOS-14%2B-0A84FF?logo=apple&logoColor=white"> <img alt="Universal: Apple Silicon + Intel" src="https://img.shields.io/badge/Universal-Apple%20Silicon%20%2B%20Intel-5E5CE6"> <img alt="Swift 6" src="https://img.shields.io/badge/Swift-6-F05138?logo=swift&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardApple](https://github.com/hobbyworker/MINIKeyboardApple) |
| **MINIKeyboardWin**<br><img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white"> <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6"> <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardWin](https://github.com/hobbyworker/MINIKeyboardWin) |
| **MINIKeyboardLinux**<br><img alt="Linux" src="https://img.shields.io/badge/Linux-GTK%204%20·%20libadwaita-4A86CF?logo=linux&logoColor=white"> <img alt="Flatpak: x86_64 · aarch64" src="https://img.shields.io/badge/Flatpak-x86__64%20·%20aarch64-4A90D9?logo=flatpak&logoColor=white"> <img alt="Rust" src="https://img.shields.io/badge/Rust-2024-B7410E?logo=rust&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardLinux](https://github.com/hobbyworker/MINIKeyboardLinux) |
| **MINIKeyboardWeb**<br><img alt="Chrome · Edge" src="https://img.shields.io/badge/Chrome%20·%20Edge-WebHID-4285F4?logo=googlechrome&logoColor=white"> <img alt="Windows · macOS · Linux" src="https://img.shields.io/badge/Windows%20·%20macOS%20·%20Linux-5E5CE6"> <img alt="TypeScript · Svelte 5" src="https://img.shields.io/badge/TypeScript-Svelte%205-3178C6?logo=typescript&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardWeb](https://github.com/hobbyworker/MINIKeyboardWeb) |

## 下載

從 [Releases](https://github.com/hobbyworker/MINIKeyboardWin/releases) 下載適合你電腦的安裝程式並執行：Intel 和 AMD 用 `MINIKeyboard-<版本>-x64-Setup.exe`，ARM 用 `-arm64-Setup.exe`。它不需要系統管理員權限，會安裝到你的使用者資料夾，並在桌面和「開始」功能表中建立捷徑。安裝程式沒有簽署，因此第一次執行時 Windows 會顯示「Windows 已保護您的電腦」：按一下**其他資訊**，然後按一下**仍要執行**。

App 會自動更新。它會在啟動時和每天一次尋找新版本（**設定 ▸ App ▸ 自動檢查更新**），選擇 ⋯ ▸ **檢查更新…** 時也會尋找。有新版本時，按一下視窗頂端橫條中的**更新並重新啟動**。你的設定會保留下來。Releases 中的 `.nupkg` 和 `releases.*.json` 檔案是 App 更新時下載的，你不需要下載它們。要移除 App，請在 Windows 的設定 ▸ 應用程式 ▸ 已安裝的應用程式中解除安裝。

## 系統需求

- Windows 10 版本 2004 或以上版本，或 Windows 11，x64 或 ARM64
- 不需要驅動程式，也不需要允許權限：兩類鍵盤都以 HID 裝置開啟。
  - `1189:8890` 和廠商工具一樣，透過它的設定介面（USB 介面 1）寫入。App 不會讀取你輸入的內容。
  - 其餘鍵盤和廠商 App 一樣，透過第一個 USB 介面寫入。這段期間，鍵盤仍可照常輸入。

## 建置

需要 .NET 10 SDK（10.0.402 或以上版本），不需要 Visual Studio。App 只能在 Windows 上建置；三個函式庫和測試可以在任何作業系統上建置。

```powershell
copy Brand.props.example Brand.props                                 # 僅第一次 — 然後填入 BrandName
dotnet build src/MINIKeyboard/MINIKeyboard.csproj -p:Platform=x64    # ARM 電腦用 -p:Platform=ARM64
dotnet test --project tests/MINIKeyboard.Domain.Tests                # 還有 Data.Tests、Presentation.Tests — 不會碰到鍵盤
```

建置出的 App 位於 `src/MINIKeyboard/bin/<x64 或 ARM64>/Debug/net10.0-windows10.0.26100.0/MINIKeyboard.exe`。

App 的名稱、圖像和連結不在儲存庫中，請在 `Brand.props` 中填入你自己的。沒有名稱（`BrandName`）時，只有 App 會停下來要求提供名稱；函式庫和測試照常建置。圖像、版權資訊和連結可以不填；沒有圖像時，App 會使用 Windows 的通用圖像。

建置分為三種（`-p:Distribution=…`）。預設的 `Source` 沒有 App 內更新。`Direct` 由 [Velopack](https://velopack.io) 安裝和更新：`BrandUpdateUrl` 指出新版本所在的位置——GitHub 儲存庫（其 Releases），或存放 Velopack 更新來源的其他位址或資料夾。`Store` 是為 Microsoft Store 版預留的，尚未就緒。

在 ⋯ ▸ **模擬器**中，可以在沒有該鍵盤的情況下試用 App。選擇 `1189:8890`、新韌體的佈局，或不告知類型的鍵盤之一，App 就會像鍵盤已經插上一樣，以相同的指令運作，而不會向 USB 送出任何內容。使用期間，視窗頂端的橘色橫條會提示這一點，按一下**返回 USB 上的鍵盤**即可返回。Debug 建置提供所有種類（`1189:8890` 的其他報告 ID、舊韌體），設定 `MINIKEYBOARD_SIMULATED_KEYBOARD`（例如 `8890-3`）即可直接以該鍵盤啟動。在 ⋯ ▸ **裝置日誌**（Ctrl+Alt+L）中可以查看所有寫入的報告和收到的回應。

## 使用方式

1. 用 USB 連接鍵盤。App 會找到鍵盤，並詢問第二類鍵盤的類型；若鍵盤告知了類型，會立即讀取設定。
2. 對於不告知形狀的鍵盤，第一次使用時要設定佈局（廠商 App 的佈局之一；`1189:8890` 則是按鍵、每列按鍵數和旋鈕的數量）以及它擺在你面前的方向。App 會依 USB ID 記住這些設定。之後可以透過**形狀…**、**旋轉**（Ctrl+T）變更。已告知形狀的鍵盤只能旋轉。
3. 選擇層，然後在鍵盤圖上選擇按鍵或旋鈕動作。
4. 指定動作：
   - **按鍵**：按住變更鍵（Ctrl、Alt、Shift、Win 及右側的變更鍵）再按一下按鍵，每按一下就加入一個輸入。**僅變更鍵**會將該按鍵變成變更鍵。**QWERTY** 依全尺寸鍵盤的樣子排列按鍵，**ABC** 則分組排列。只能按一下鍵盤支援的按鍵。
   - **多媒體**、**滑鼠**：取代按鍵原有的動作。
   - **LED**：該層的燈光。新版韌體可以從按鍵 1–16 中選擇一個並為它設定顏色。
   - **延遲**：適用於會記住延遲的韌體。
5. **儲存**（Ctrl+S）會寫入變更的內容。**讀取**（Ctrl+R）會從支援讀取的鍵盤取得目前的設定。

在 ⋯ ▸ **設定**（Ctrl+,）中可以選擇介面語言（13 種之一，或跟隨 Windows）以及淺色或深色。從 Releases 下載的 App 在這裡還有 **App ▸ 自動檢查更新**。最下方的**關於**中有版本、連結和**隨附的軟體**（App 隨附軟體的授權條款）。

**設定 ▸ 韌體問題規避**中的**變更燈光後防止按鍵重複**預設為開啟。`1189:8890` 的韌體在儲存燈光後，會讓按鍵的第一個字元不停重複（廠商軟體也一樣），所以 App 會在燈光之後再送出一次不含按鍵的儲存指令。關閉後，App 只會送出與廠商軟體相同的內容。另外，這款鍵盤拔掉後會忘記燈光設定。

## 架構

規則位於三個函式庫中。相依性只指向內側，向外依賴的層無法通過編譯。

```
src/
├── MINIKeyboard.Domain/        鍵盤、動作、佈局的控制項、各鍵盤可儲存的內容，Port，使用案例（僅 .NET 基底類別庫）
├── MINIKeyboard.Data/          各鍵盤的指令和控制項編號，透過 HID 尋找鍵盤及寫入（SetupAPI、hid.dll），模擬鍵盤
├── MINIKeyboard.Presentation/  視圖模型和畫面繪製的值 — 佈局的圖形、按鍵和顏色表（不含 UI 框架）
├── MINIKeyboard.Views/         WinUI 3 — 只看 Presentation，每種語言的翻譯表（Translations/）
└── MINIKeyboard/               組裝（composition root）— 唯一知道所有專案的地方。Direct/ 只用於直接下載版（Velopack）
tests/                          xUnit — 所有指令的位元組、佈局、編輯、編輯器
Brand.props.example             名稱、圖像、連結以及各建置的值 — 複製為 Brand.props 後填寫
LICENSES/                       最上方圖像的授權條款（CC BY-SA 4.0）
```

以零警告為標準。警告要修正，而不是關閉。

## 限制

- 目前已在 Windows 11 上用兩台鍵盤確認過寫入：一台有 6 個按鍵、1 個旋鈕的 `1189:8890`，在 ARM64 和 x64 電腦上；以及一台使用新版韌體、有 12 個按鍵和 2 個旋鈕的 `514C:8850`，在 ARM64 上。尚未在 Windows 10 上確認。其餘鍵盤依廠商 App 的方式寫入；指令與 macOS App 的逐位元組相同，並由測試確保，但還沒有在實機上確認過。
- `1189:8890` 和不告知類型的鍵盤不會告知目前的設定，因此只會顯示在 App 中設定的內容。
- 一次只開啟一個鍵盤。插上多個時，用連線狀態旁的按鈕選擇；App 會開啟上次選擇的那個，否則開啟最先找到的那個（`1189:8890` 優先）。
- 不支援滑鼠移動和拖移。

## 授權條款

MIT — 請參閱 [LICENSE](../LICENSE)。最上方的圖像採用 CC BY-SA 4.0 — 請參閱 [LICENSES/CC-BY-SA-4.0.txt](../LICENSES/CC-BY-SA-4.0.txt)。

App 還隨附 Microsoft 的元件——Windows App SDK 和 Windows SDK for .NET——它們不採用 MIT，而是適用 Microsoft 自己的授權條款。這些條款位於 App 的設定 ▸ 關於 ▸ **隨附的軟體**中，使用本 App 即表示就這些元件接受這些條款。
