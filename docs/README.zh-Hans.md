<p align="center"><img src="../.github/icon.png" width="128" height="128" alt="应用图标"></p>

<h1 align="center">Windows 宏键盘设置工具</h1>

<p align="center">
  <img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white">
  <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6">
  <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white">
  <a href="../LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"></a>
</p>

<p align="center"><a href="../README.md">English</a> · <a href="README.ko.md">한국어</a> · <a href="README.de.md">Deutsch</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a> · <a href="README.id.md">Bahasa Indonesia</a> · <a href="README.it.md">Italiano</a> · <a href="README.ja.md">日本語</a> · <a href="README.pl.md">Polski</a> · <a href="README.pt-BR.md">Português (Brasil)</a> · <a href="README.vi.md">Tiếng Việt</a> · <b>简体中文</b> · <a href="README.zh-Hant.md">繁體中文</a></p>

这是一款向小型 USB 宏键盘写入按键设置的 Windows 应用。这类键盘在售的型号从只有一个旋钮到多达 21 个按键不等，旋钮最多 4 个。对于每个按键和旋钮的动作（向左旋转、按下、向右旋转），可以在第 1–3 层设置按键输入、多媒体键或鼠标动作，还可以设置键盘的灯光。它是按同样规则打造的 macOS 应用的 Windows 版，使用相同的命令。

| 键盘 | 布局 | 读取 | 延迟 | 灯光 | 实机验证 |
|---|---|---|---|---|---|
| `1189:8890` | 由用户设定：按键（最多 12 个）、每行按键数、旋钮（最多 2 个） | 不支持 | 无 | 三种模式 | 已验证（6 个按键、1 个旋钮） |
| 厂商 `1189` 或 `514C`，产品 `8840`、`8842`、`8830`–`8833`、`8850`（以及 `514C:8851`），新版固件 | 按键盘告知的布局（26 种） | 连接后立即读取 | 每个输入 | 一种模式，以及按键 1–16 的颜色 | 未验证 |
| 同类键盘，旧版固件 | 按键盘告知的布局 | 请求时 | 每个按键一个 | 颜色和模式 | 未验证 |
| 同类键盘，不告知类型的 | 由用户选择（26 种） | 不支持 | 无 | 颜色和模式 | 未验证 |

`1189:8890` 是厂商的 Windows 设置工具“MINI KeyBoard”所支持的键盘，其余则是厂商的跨平台应用“MINI_KEYBOARD”会查找的键盘。本应用同时使用这两个工具的命令。第二类键盘大多会告知自己有几个按键和旋钮、使用什么固件，因此应用会照原样绘制厂商应用所绘制的布局；不告知的键盘则采用用户从这些布局中选择的那一种。`1189:8890` 是厂商应用不认识的键盘，它自己的工具也不绘制外形，因此无法得知其实际形状。所以只要设定按键和旋钮的数量以及每行的按键数，应用就会照此绘制出整齐的排列。

应用提供 13 种语言：英语、韩语、日语、中文（简体、繁体）、德语、西班牙语、法语、意大利语、葡萄牙语（巴西）、波兰语、印度尼西亚语和越南语。应用跟随 Windows 的语言（也可以在设置中选择），其他语言则显示为英语。

本仓库的源代码采用 MIT 许可证。所发布应用的一部分（名称、图标和链接）不包含在此源代码中。顶部的图标仅供展示，它不采用 MIT 许可证，而是采用 CC BY-SA 4.0 许可证（[`LICENSES/CC-BY-SA-4.0.txt`](../LICENSES/CC-BY-SA-4.0.txt)），用本仓库构建的应用中也不包含它。

## 下载

从 [Releases](https://github.com/hobbyworker/MINIKeyboardWin/releases) 下载适合你电脑的安装程序并运行：Intel 和 AMD 用 `MINIKeyboard-<版本>-x64-Setup.exe`，ARM 用 `-arm64-Setup.exe`。它无需管理员权限，会安装到你的用户文件夹，并在桌面和“开始”菜单中创建快捷方式。安装程序没有签名，因此首次运行时 Windows 会显示“Windows 已保护你的电脑”：点击**更多信息**，然后点击**仍要运行**。

应用会自动更新。它会在启动时和每天一次查找新版本（**设置 ▸ App ▸ 自动检查更新**），选择 ⋯ ▸ **检查更新…** 时也会查找。有新版本时，点击窗口顶部横条中的**更新并重启**。你的设置会保留。Releases 中的 `.nupkg` 和 `releases.*.json` 文件是应用更新时下载的，你无需下载它们。要移除应用，请在 Windows 的设置 ▸ 应用 ▸ 安装的应用中卸载。

## 系统要求

- Windows 10 版本 2004 或更高版本，或 Windows 11，x64 或 ARM64
- 无需驱动程序，也无需授予权限：两类键盘都作为 HID 设备打开。
  - `1189:8890` 与厂商工具一样，通过它的设置接口（USB 接口 1）写入。应用不会读取你输入的内容。
  - 其余键盘与厂商应用一样，通过第一个 USB 接口写入。写入期间，键盘仍可照常输入。

## 构建

需要 .NET 10 SDK（10.0.402 或更高版本），不需要 Visual Studio。应用只能在 Windows 上构建；三个库和测试可以在任何操作系统上构建。

```powershell
copy Brand.props.example Brand.props                                 # 仅首次 — 然后填写 BrandName
dotnet build src/MINIKeyboard/MINIKeyboard.csproj -p:Platform=x64    # ARM 电脑用 -p:Platform=ARM64
dotnet test --project tests/MINIKeyboard.Domain.Tests                # 还有 Data.Tests、Presentation.Tests — 不会触碰键盘
```

构建出的应用位于 `src/MINIKeyboard/bin/<x64 或 ARM64>/Debug/net10.0-windows10.0.26100.0/MINIKeyboard.exe`。

应用的名称、图标和链接不在仓库中，请在 `Brand.props` 中填入你自己的。没有名称（`BrandName`）时，只有应用会停下来要求提供名称；库和测试照常构建。图标、版权信息和链接可以不填；没有图标时，应用使用 Windows 的通用图标。

构建分为三种（`-p:Distribution=…`）。默认的 `Source` 没有应用内更新。`Direct` 由 [Velopack](https://velopack.io) 安装和更新：`BrandUpdateUrl` 指明新版本所在的位置——GitHub 仓库（其 Releases），或存放 Velopack 更新源的其他地址或文件夹。`Store` 是为 Microsoft Store 版预留的，尚未就绪。

在 ⋯ ▸ **模拟器**中，可以在没有对应键盘的情况下试用应用。选择 `1189:8890`、新固件的布局，或者不告知类型的键盘中的一个，应用就会像键盘已经插上一样，用同样的命令运行，而不会向 USB 发送任何内容。使用期间，窗口顶部的橙色横条会提示这一点，点击**返回 USB 上的键盘**即可返回。Debug 构建提供所有种类（`1189:8890` 的其他报告 ID、旧固件），设置 `MINIKEYBOARD_SIMULATED_KEYBOARD`（例如 `8890-3`）即可直接以该键盘启动。在 ⋯ ▸ **设备日志**（Ctrl+Alt+L）中可以查看写入的所有报告和收到的所有回复。

## 使用方法

1. 通过 USB 连接键盘。应用会找到键盘，并询问第二类键盘的类型；如果是新版固件，会立即读取设置。
2. 对于不告知形状的键盘，首次使用时要设定布局（厂商应用的布局之一；`1189:8890` 则是按键、每行按键数和旋钮的数量）以及它在你面前的摆放方向。应用会按 USB ID 记住这些设置。之后可以通过**形状…**、**旋转**（Ctrl+T）更改。已告知形状的键盘只能旋转。
3. 选择层，然后在键盘图上选择按键或旋钮动作。
4. 指定动作：
   - **按键**：按住修饰键（Ctrl、Alt、Shift、Win 以及右侧的修饰键）再点击按键，每点击一次就添加一个输入。**仅修饰键**会把该按键变成修饰键。**QWERTY** 按全尺寸键盘的样子排列按键，**ABC** 则分组排列。只能点击键盘支持的按键。
   - **多媒体**、**鼠标**：替换按键原有的动作。
   - **LED**：该层的灯光。新版固件可以从按键 1–16 中选择一个并为其设置颜色。
   - **延迟**：适用于能记住延迟的固件。
5. **保存**（Ctrl+S）会写入更改的内容。**读取**（Ctrl+R）会从支持读取的键盘获取当前设置。

在 ⋯ ▸ **设置**（Ctrl+,）中可以选择界面语言（13 种之一，或跟随 Windows）以及浅色或深色外观。从 Releases 下载的应用在这里还有 **App ▸ 自动检查更新**。底部的**关于**中有版本、链接和**随附的软件**（应用随附软件的许可证）。

**设置 ▸ 固件问题规避**中的**更改灯光后防止按键重复**默认开启。`1189:8890` 的固件在保存灯光后会让按键的第一个字符不停重复（厂商软件也是如此），所以应用会在灯光之后再发送一次不含按键的保存命令。关闭后，应用只发送与厂商软件相同的内容。另外，这款键盘拔下后会忘记灯光设置。

## 结构

规则位于三个库中。依赖只指向内侧，向外依赖的层无法通过编译。

```
src/
├── MINIKeyboard.Domain/        键盘、动作、布局的控件、每种键盘能存储的内容，端口，用例（仅 .NET 基类库）
├── MINIKeyboard.Data/          各键盘的命令与控件编号，通过 HID 查找键盘和写入（SetupAPI、hid.dll），模拟键盘
├── MINIKeyboard.Presentation/  视图模型和界面绘制的值 — 布局的图形、按键和颜色表（不含 UI 框架）
├── MINIKeyboard.Views/         WinUI 3 — 只看 Presentation，每种语言的翻译表（Translations/）
└── MINIKeyboard/               组装（composition root）— 唯一知道所有项目的地方。Direct/ 只用于直接下载版（Velopack）
tests/                          xUnit — 所有命令的字节、布局、编辑、编辑器
Brand.props.example             名称、图标、链接以及各构建的值 — 复制为 Brand.props 后填写
LICENSES/                       顶部图标的许可证（CC BY-SA 4.0）
```

以零警告为标准。警告要修复，而不是关闭。

## 限制

- 目前只在 Windows 11（ARM64）上用一台键盘验证过写入：一台有 6 个按键、1 个旋钮的 `1189:8890`。尚未在 x64 电脑和 Windows 10 上验证。其余键盘按厂商应用的方式写入；命令与 macOS 应用的逐字节相同，并由测试保证，但还没有在任何一台实机上验证过。
- `1189:8890` 和不告知类型的键盘不会告知当前设置，因此只会显示在应用中设定的内容。
- 一次只能使用一个键盘。插入多个时，使用最先找到的那个（`1189:8890` 优先）。
- 不支持鼠标移动和拖动。

## 许可证

MIT — 参见 [LICENSE](../LICENSE)。顶部的图标采用 CC BY-SA 4.0 — 参见 [LICENSES/CC-BY-SA-4.0.txt](../LICENSES/CC-BY-SA-4.0.txt)。

应用还随附 Microsoft 的组件——Windows App SDK 和 Windows SDK for .NET——它们不采用 MIT，而是适用 Microsoft 自己的许可条款。这些条款位于应用的设置 ▸ 关于 ▸ **随附的软件**中，使用本应用即表示就这些组件接受这些条款。
