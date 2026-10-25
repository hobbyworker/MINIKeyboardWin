<p align="center"><img src=".github/icon.png" width="128" height="128" alt="App icon"></p>

<h1 align="center">Macro keyboard configurator for Windows</h1>

<p align="center">
  <img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white">
  <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6">
  <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white">
  <a href="LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"></a>
</p>

<p align="center"><b>English</b> · <a href="docs/README.ko.md">한국어</a> · <a href="docs/README.de.md">Deutsch</a> · <a href="docs/README.es.md">Español</a> · <a href="docs/README.fr.md">Français</a> · <a href="docs/README.id.md">Bahasa Indonesia</a> · <a href="docs/README.it.md">Italiano</a> · <a href="docs/README.ja.md">日本語</a> · <a href="docs/README.pl.md">Polski</a> · <a href="docs/README.pt-BR.md">Português (Brasil)</a> · <a href="docs/README.vi.md">Tiếng Việt</a> · <a href="docs/README.zh-Hans.md">简体中文</a> · <a href="docs/README.zh-Hant.md">繁體中文</a></p>

A Windows app that writes key settings to small USB macro keyboards — the ones sold with anything from a single
knob to 21 keys, and up to four knobs. For each key and knob action (rotate left, press, rotate right) it sets
key presses, a media key or a mouse action on layers 1–3, and the keyboard's lighting. It is the Windows
counterpart of a macOS app built on the same rules, and speaks the same commands.

| Keyboard | Layout | Reads back | Waits | Lighting | Tried |
|---|---|---|---|---|---|
| `1189:8890` | arranged by you — keys (up to 12), keys per row, knobs (up to 2) | no | no | three modes | yes — 6 keys and a knob |
| vendor `1189` or `514C`, product `8840` · `8842` · `8830`–`8833` · `8850` (and `514C:8851`), newer firmware | from what the keyboard says — 26 layouts | when connected | before each press | a mode, and a colour for each of keys 1–16 | no |
| the same, older firmware | from what the keyboard says | when you ask | one per key | a colour and a mode | no |
| the same, not saying what it is | chosen by you — 26 layouts | no | no | a colour and a mode | no |

The `1189:8890` is the keyboard the vendor's Windows tool "MINI KeyBoard" works with; the others are the ones the
vendor's cross-platform app "MINI_KEYBOARD" looks for. The app speaks both tools' commands. Most of the second
kind say how many keys and knobs they have and which firmware they run, and the app draws the layout the vendor's
app would; one that doesn't say gets the one of those layouts you pick. The vendor's app doesn't know the
`1189:8890` and its own tool draws no shape, so its real shape isn't known: you tell the app how many keys and
knobs it has and how many keys are in a row, and it draws a tidy arrangement of that.

The app is available in 13 languages — English, Korean, Japanese, Chinese (Simplified and Traditional), German,
Spanish, French, Italian, Portuguese (Brazil), Polish, Indonesian and Vietnamese — and follows the language of
Windows (or the one you pick in Settings). Other languages get English.

The source in this repository is MIT licensed. A few parts of the shipped app are not part of this source — its
name, its icon and its links. The icon at the top is shown for reference: it is not covered by the MIT license
but by CC BY-SA 4.0 ([`LICENSES/CC-BY-SA-4.0.txt`](LICENSES/CC-BY-SA-4.0.txt)), and builds from this repository
don't include it.

## Download

Get the installer for your PC from the [Releases](https://github.com/hobbyworker/MINIKeyboardWin/releases) page and
run it — `MINIKeyboard-<version>-x64-Setup.exe` for Intel and AMD, `-arm64-Setup.exe` for ARM. It installs into your
user folder without administrator rights, with shortcuts on the desktop and in the Start menu. The installer isn't
signed, so the first time Windows shows "Windows protected your PC": choose **More info**, then **Run anyway**.

The app updates itself. It looks for a new version at launch and once a day (**Settings ▸ App ▸ Check for updates
automatically**), and when you choose ⋯ ▸ **Check for Updates…**. When there is one, choose **Update and Relaunch**
in the bar at the top of the window. Your settings stay. The `.nupkg` and `releases.*.json` files on the Releases page
are what the app downloads to update; you don't need them. To remove the app, use Windows' Settings ▸ Apps ▸
Installed apps.

## Requirements

- Windows 10 version 2004 or later, or Windows 11, on x64 or ARM64
- No driver and no permission: both kinds are opened as HID devices.
  - The `1189:8890` is written through its configuration interface (USB interface 1), as the vendor's tool does.
    The app never reads what you type.
  - The others are written through their first USB interface, as the vendor's app does. The keyboard keeps typing
    meanwhile.

## Building

It needs the .NET 10 SDK (10.0.402 or later) — Visual Studio isn't required. The app builds on Windows only; the
three libraries and the tests build anywhere.

```powershell
copy Brand.props.example Brand.props                                 # once — then fill in BrandName
dotnet build src/MINIKeyboard/MINIKeyboard.csproj -p:Platform=x64    # on an ARM PC, -p:Platform=ARM64
dotnet test --project tests/MINIKeyboard.Domain.Tests                # and Data.Tests, Presentation.Tests — no keyboard is touched
```

The app lands in `src/MINIKeyboard/bin/<x64 or ARM64>/Debug/net10.0-windows10.0.26100.0/MINIKeyboard.exe`.

The app's name, icon and links are not in the repository — put your own in `Brand.props`. Without a name
(`BrandName`) only the app stops and asks for one; the libraries and tests build as they are. The icon, copyright
line and links are optional — without an icon the app has Windows' generic one.

A build is one of three (`-p:Distribution=…`). `Source`, the default, has no updates of its own. `Direct` is installed
and updated by [Velopack](https://velopack.io): `BrandUpdateUrl` says where the new versions are — a GitHub repository
(its Releases), or any other address or folder that holds Velopack's feed. `Store` is the place of a Microsoft Store
build, not ready yet.

⋯ ▸ **Simulator** lets you try the app without the keyboard in question: pick a `1189:8890`, a layout on newer
firmware, or a keyboard that doesn't say what it is, and the app acts as if it were plugged in — through the same
commands, nothing going to USB. While one is in use, an orange bar at the top of the window says so, and **Back to the
Keyboard on USB** returns. Debug builds offer every kind (the `1189:8890`'s other report IDs, older firmware), and
`MINIKEYBOARD_SIMULATED_KEYBOARD` (for example `8890-3`) starts on one. ⋯ ▸ **Device Log** (Ctrl+Alt+L) shows every
report written and every answer.

## Using it

1. Connect the keyboard over USB. The app finds it, and asks a keyboard of the second kind what it is; one with
   newer firmware is read at once.
2. A keyboard that doesn't say its shape asks for its layout the first time — one of the vendor app's layouts, or
   for the `1189:8890` its keys, keys per row and knobs — and how it sits in front of you. The app remembers both
   for that USB ID. **Shape…** and **Rotate** (Ctrl+T) change them later; a keyboard that said its shape only
   turns.
3. Pick a layer, then the key or knob action on the keyboard.
4. Give it an action:
   - **Keys** — hold modifiers (Ctrl, Alt, Shift, Win, and the right-hand ones), then click keys; each click adds
     a press. **Modifiers Only** turns the key into a modifier key. **QWERTY** lays the keys out as on a full-size
     keyboard, **ABC** in groups; only the keys the keyboard takes can be clicked.
   - **Multimedia**, **Mouse** — replace what the key did.
   - **LED** — the layer's lights; on newer firmware, select one of keys 1–16 to give it a colour.
   - **Delay** — on firmware that keeps waits.
5. **Save** (Ctrl+S) writes what changed. **Read** (Ctrl+R) asks a keyboard that reads back what it holds.

⋯ ▸ **Settings** (Ctrl+,) picks the interface language (one of the 13, or Windows') and light or dark. The app from
the Releases page also has **App ▸ Check for updates automatically** there. **About**, at the bottom, has the version,
the links and **Included Software** — the licenses of what the app comes with.

**Settings ▸ Firmware Workarounds** has **Prevent key repeat after changing the lighting**, on by default. After its
lighting is saved, the `1189:8890`'s firmware repeats a key's first stroke without end — its maker's software does the
same — so the app sends one more save, with no key in it, after the lighting. Off, the app sends just what the maker's
software sends. That keyboard also forgets its lighting when it's unplugged.

## Structure

The rules live in three libraries whose dependencies point inwards — a layer that reaches outwards doesn't compile.

```
src/
├── MINIKeyboard.Domain/        keyboards, actions, the controls of each layout and what each keyboard can hold; the ports; use cases (the base class library only)
├── MINIKeyboard.Data/          the commands of each keyboard and the numbers its controls go by; finding it and writing over HID (SetupAPI, hid.dll); simulated keyboards
├── MINIKeyboard.Presentation/  view models and the values the screens draw — the layouts' geometry, key and colour tables (no UI framework)
├── MINIKeyboard.Views/         WinUI 3 — sees the presentation only; a translation table per language (Translations/)
└── MINIKeyboard/               the composition root: the only project that sees them all. Direct/ goes into the direct download only (Velopack)
tests/                          xUnit — the bytes of every command, the layouts, the edits, the editor
Brand.props.example             the name, icon, links and each build's values — copy to Brand.props and fill in
LICENSES/                       the licence of the icon at the top (CC BY-SA 4.0)
```

Zero warnings is the bar. Warnings are fixed, not switched off.

## Limitations

- Writing has been tried on one keyboard so far, a `1189:8890` with six keys and a knob, on Windows 11, on both
  ARM64 and x64 PCs. It hasn't been tried on Windows 10 yet. The other keyboards are written as their vendor's app writes
  them — the commands are the macOS app's, byte for byte, and tests pin them — but none has been tried yet.
- The `1189:8890`, and keyboards that don't say what they are, can't report their settings, so the app shows only
  what was set in it.
- One keyboard at a time. With several plugged in, the app takes the first it finds, a `1189:8890` first.
- Mouse movement and dragging are not offered.

## License

MIT — see [LICENSE](LICENSE). The icon at the top: CC BY-SA 4.0 — see [LICENSES/CC-BY-SA-4.0.txt](LICENSES/CC-BY-SA-4.0.txt).

The app also carries Microsoft components — the Windows App SDK and the Windows SDK for .NET — that are licensed
under Microsoft's own terms, not MIT. Their terms are in the app under Settings ▸ About ▸ **Included Software**, and
using the app means accepting them for those components.
