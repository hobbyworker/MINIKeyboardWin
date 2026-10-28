<p align="center"><img src="../.github/icon.png" width="128" height="128" alt="アプリアイコン"></p>

<h1 align="center">Windows用マクロキーボード設定ツール</h1>

<p align="center">
  <img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white">
  <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6">
  <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white">
  <a href="../LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"></a>
</p>

<p align="center"><a href="../README.md">English</a> · <a href="README.ko.md">한국어</a> · <a href="README.de.md">Deutsch</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a> · <a href="README.id.md">Bahasa Indonesia</a> · <a href="README.it.md">Italiano</a> · <b>日本語</b> · <a href="README.pl.md">Polski</a> · <a href="README.pt-BR.md">Português (Brasil)</a> · <a href="README.vi.md">Tiếng Việt</a> · <a href="README.zh-Hans.md">简体中文</a> · <a href="README.zh-Hant.md">繁體中文</a></p>

小さなUSBマクロキーボードにキー設定を書き込むWindowsアプリです。ノブが1つだけのものからキーが21個あるものまで、ノブは最大4つという構成で売られているキーボードが対象です。キーとノブの操作（反時計回りに回す・押す・時計回りに回す）ごとに、レイヤー1–3にキー入力・マルチメディアキー・マウス操作を割り当て、キーボードのライティングも設定します。同じルールで作られたmacOSアプリのWindows版で、同じコマンドを使います。

<p align="center"><img src="../.github/devices.png" width="560" alt="小さなUSBマクロキーボード2台 — キー6個とノブ1つ、キー12個とノブ2つ"></p>

| キーボード | 配列 | 読み込み | 遅延 | ライティング | 実機での確認 |
|---|---|---|---|---|---|
| `1189:8890` | ユーザーが決める：キー（最大12個）・1列のキー数・ノブ（最大2個） | 不可 | なし | モード3つ | 済み（キー6個・ノブ1つ） |
| ベンダー`1189`または`514C`、製品`8840`・`8842`・`8830`–`8833`・`8850`・`8851`、新しいファームウェア | キーボードが知らせるとおり（26種類） | 接続時にすぐ | 入力ごと | モード1つとキー1–16の色 | 済み（`514C:8850`、キー12個・ノブ2つ） |
| 同じキーボード、旧ファームウェア | キーボードが知らせるとおり | 接続時にすぐ | キーごとに1つ | 色とモード | 未確認 |
| 同じキーボードで、種類を知らせないもの | ユーザーが選ぶ（26種類） | 不可 | なし | 色とモード | 未確認 |

`1189:8890`はメーカーのWindows用設定ツール「MINI KeyBoard」が扱うキーボードで、それ以外はメーカーのマルチプラットフォームアプリ「MINI_KEYBOARD」が探すキーボードです。このアプリは両方のツールのコマンドを使います。2つ目のグループのキーボードはたいてい、キーとノブの数やファームウェアの種類を知らせてくるので、メーカーのアプリが描く配列をそのまま描きます。知らせてこないキーボードには、それらの配列の中からユーザーが選んだものを使います。`1189:8890`はメーカーのアプリが知らないキーボードで、専用のツールも形を描かないため、実際の形はわかりません。そこで、キーとノブの数と1列のキー数を決めると、アプリがそのとおりに整った形で描きます。

アプリは13の言語で表示されます：英語・韓国語・日本語・中国語（簡体字・繁体字）・ドイツ語・スペイン語・フランス語・イタリア語・ポルトガル語（ブラジル）・ポーランド語・インドネシア語・ベトナム語。Windowsの言語に従い（設定で選ぶこともできます）、それ以外の言語では英語で表示されます。

このリポジトリのソースはMITライセンスです。配布しているアプリの一部（名前・アイコン・リンク）は、このソースに含まれていません。冒頭のアイコンは紹介のために置いた画像で、MITライセンスではなくCC BY-SA 4.0です（[`LICENSES/CC-BY-SA-4.0.txt`](../LICENSES/CC-BY-SA-4.0.txt)）。このリポジトリからビルドしたアプリには含まれません。

## macOS・Windows・Linux・Web

MINIKeyboardには、同じルールで作られた4つの版があります。コマンドも、13の言語も同じです。3つのアプリは同じキーボードに対応し、ブラウザで動くWeb版は、そのうち`1189:8890`を除くキーボードに対応します。

| リポジトリ | GitHub |
|---|---|
| **MINIKeyboardApple**<br><img alt="macOS 14+" src="https://img.shields.io/badge/macOS-14%2B-0A84FF?logo=apple&logoColor=white"> <img alt="Universal: Apple Silicon + Intel" src="https://img.shields.io/badge/Universal-Apple%20Silicon%20%2B%20Intel-5E5CE6"> <img alt="Swift 6" src="https://img.shields.io/badge/Swift-6-F05138?logo=swift&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardApple](https://github.com/hobbyworker/MINIKeyboardApple) |
| **MINIKeyboardWin**<br><img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white"> <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6"> <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardWin](https://github.com/hobbyworker/MINIKeyboardWin) |
| **MINIKeyboardLinux**<br><img alt="Linux" src="https://img.shields.io/badge/Linux-GTK%204%20·%20libadwaita-4A86CF?logo=linux&logoColor=white"> <img alt="Flatpak: x86_64 · aarch64" src="https://img.shields.io/badge/Flatpak-x86__64%20·%20aarch64-4A90D9?logo=flatpak&logoColor=white"> <img alt="Rust" src="https://img.shields.io/badge/Rust-2024-B7410E?logo=rust&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardLinux](https://github.com/hobbyworker/MINIKeyboardLinux) |
| **MINIKeyboardWeb**<br><img alt="Chrome · Edge" src="https://img.shields.io/badge/Chrome%20·%20Edge-WebHID-4285F4?logo=googlechrome&logoColor=white"> <img alt="Windows · macOS · Linux" src="https://img.shields.io/badge/Windows%20·%20macOS%20·%20Linux-5E5CE6"> <img alt="TypeScript · Svelte 5" src="https://img.shields.io/badge/TypeScript-Svelte%205-3178C6?logo=typescript&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardWeb](https://github.com/hobbyworker/MINIKeyboardWeb) |

## ダウンロード

[Releases](https://github.com/hobbyworker/MINIKeyboardWin/releases)から、お使いのPCに合ったインストーラーを入手して実行します。Intel・AMDは`MINIKeyboard-<バージョン>-x64-Setup.exe`、ARMは`-arm64-Setup.exe`です。管理者権限なしでユーザーフォルダーにインストールされ、デスクトップとスタートメニューにショートカットが作成されます。インストーラーには署名がないため、初めて実行するときにWindowsが「Windows によって PC が保護されました」を表示します。**詳細情報**をクリックしてから、**実行**をクリックします。

アプリは自動でアップデートします。起動時と1日1回新しいバージョンを確認し（**設定 ▸ アプリ ▸ アップデートを自動的に確認**）、⋯ ▸ **アップデートを確認…**でも確認できます。新しいバージョンがあれば、ウィンドウ上部の帯で**アップデートして再起動**をクリックします。設定はそのまま残ります。Releasesにある`.nupkg`・`releases.*.json`は、アプリがアップデートするときに取得するファイルなので、自分で入手する必要はありません。アンインストールは、Windowsの設定 ▸ アプリ ▸ インストールされているアプリから行います。

## 動作環境

- Windows 10 バージョン2004以降、またはWindows 11（x64またはARM64）
- ドライバーも許可も必要ありません。どちらの種類もHIDデバイスとして開きます。
  - `1189:8890`には、メーカーのツールと同じく設定用インターフェイス（USBインターフェイス1）を通して書き込みます。入力した内容は読み取りません。
  - それ以外のキーボードには、メーカーのアプリと同じく1つ目のUSBインターフェイスを通して書き込みます。その間もキーボードはいつもどおり入力できます。

## ビルド

.NET 10 SDK（10.0.402以降）が必要です。Visual Studioはなくてもかまいません。アプリはWindowsでしかビルドできませんが、3つのライブラリとテストはどのOSでもビルドできます。

```powershell
copy Brand.props.example Brand.props                                 # 最初に1回 — そのあとBrandNameを記入する
dotnet build src/MINIKeyboard/MINIKeyboard.csproj -p:Platform=x64    # ARMのPCでは-p:Platform=ARM64
dotnet test --project tests/MINIKeyboard.Domain.Tests                # Data.Tests・Presentation.Testsも — キーボードには触れない
```

ビルドしたアプリは`src/MINIKeyboard/bin/<x64またはARM64>/Debug/net10.0-windows10.0.26100.0/MINIKeyboard.exe`にできます。

アプリの名前・アイコン・リンクはリポジトリにありません。`Brand.props`に自分のものを記入します。名前（`BrandName`）がないと、アプリだけが名前を求めて止まり、ライブラリとテストはそのままビルドできます。アイコン・著作権表示・リンクはなくてもかまいません。アイコンがなければ、アプリにはWindowsの汎用アイコンが付きます。

ビルドは3種類のいずれかです（`-p:Distribution=…`）。既定の`Source`には、アプリ内のアップデートがありません。`Direct`は[Velopack](https://velopack.io)でインストール・アップデートする版です。`BrandUpdateUrl`で新しいバージョンの置き場所を指定します。GitHubのリポジトリ（そのReleases）か、Velopackのフィードを置いたほかのアドレスやフォルダーです。`Store`はMicrosoft Store版のための枠で、まだ準備中です。

⋯ ▸ **シミュレータ**で、対象のキーボードがなくてもアプリを試せます。`1189:8890`、新しいファームウェアの配列、種類を知らせないキーボードのいずれかを選ぶと、アプリは接続されているかのように同じコマンドで動作し、USBには何も送りません。使っている間はウィンドウ上部のオレンジ色の帯がそのことを知らせ、**USB に接続したキーボードに戻る**で元に戻ります。Debugビルドではすべての種類（`1189:8890`のほかのレポートID、古いファームウェア）を選べ、`MINIKEYBOARD_SIMULATED_KEYBOARD`（例：`8890-3`）を指定すると、そのキーボードで起動します。⋯ ▸ **デバイスログ**（Ctrl+Alt+L）で、書き込んだレポートと受け取った応答をすべて確認できます。

## 使い方

1. キーボードをUSBで接続します。アプリがキーボードを見つけ、2つ目のグループのキーボードには種類を問い合わせます。種類を知らせてきた場合は、すぐに設定を読み込みます。
2. 形を知らせないキーボードでは、最初に配列（メーカーのアプリの配列のいずれか。`1189:8890`ではキー・1列のキー・ノブの数）と、手前に置いた向きを決めます。アプリはUSB IDごとにこれを記憶します。あとから**形…**、**回転**（Ctrl+T）で変更できます。形を知らせたキーボードは回転だけができます。
3. レイヤーを選び、キーボードの図でキーやノブの操作を選びます。
4. 操作を割り当てます。
   - **キー**：修飾キー（Ctrl・Alt・Shift・Winと右側のもの）を押したままキーをクリックします。クリックするたびに入力が1つずつ追加されます。**修飾キーのみ**は、そのキーを修飾キーにします。**QWERTY**はキーをフルサイズキーボードのように、**ABC**はグループごとに並べます。クリックできるのは、キーボードが受け付けるキーだけです。
   - **マルチメディア**・**マウス**：キーにあった操作を置き換えます。
   - **LED**：レイヤーのライティングです。新しいファームウェアでは、キー1–16のいずれかを選んで色を付けます。
   - **遅延**：遅延を記憶するファームウェアで使えます。
5. **保存**（Ctrl+S）で変更を書き込みます。**読み込む**（Ctrl+R）は、読み込みに対応したキーボードから現在の設定を取得します。

⋯ ▸ **設定**（Ctrl+,）では、表示言語（13言語のいずれか、またはWindowsの言語）とライト・ダークを選びます。Releasesから入手したアプリには、ここに**アプリ ▸ アップデートを自動的に確認**もあります。いちばん下の**情報**に、バージョンとリンク、そして**同梱ソフトウェア**（アプリに同梱されているソフトウェアのライセンス）があります。

**設定 ▸ ファームウェアの不具合への対処**の**ライティング変更後のキーの連続入力を防ぐ**は、初期状態でオンです。`1189:8890` のファームウェアには、ライティングを保存するとキーの最初の文字が止まらずに繰り返されるバグがあるため（メーカーのソフトウェアでも同じです）、アプリはライティングのあとにキーを含まない保存コマンドをもう一度送ります。オフにすると、メーカーのソフトウェアと同じ内容だけを送ります。このキーボードは、抜き差しするとライティングが消えます。

## 構成

ルールは3つのライブラリにあります。依存関係は内側にだけ向かい、外側に手を伸ばすレイヤーはコンパイルできません。

```
src/
├── MINIKeyboard.Domain/        キーボード・操作・配列のコントロール・キーボードごとに保持できるもの、ポート、ユースケース（.NETの基本クラスライブラリのみ）
├── MINIKeyboard.Data/          キーボードごとのコマンドとコントロールの番号、HIDでのキーボードの検出と書き込み（SetupAPI・hid.dll）、シミュレータのキーボード
├── MINIKeyboard.Presentation/  ビューモデルと画面が描く値 — 配列の図形、キー・色の表（UIフレームワークなし）
├── MINIKeyboard.Views/         WinUI 3 — Presentationだけを参照。言語ごとの翻訳表（Translations/）
└── MINIKeyboard/               組み立て（composition root）— すべてのプロジェクトを知る唯一の場所。Direct/は直接配布版にだけ入る（Velopack）
tests/                          xUnit — すべてのコマンドのバイト、配列、編集、エディター
Brand.props.example             名前・アイコン・リンク・ビルドごとの値 — Brand.propsにコピーして記入する
LICENSES/                       冒頭のアイコンのライセンス（CC BY-SA 4.0）
```

警告ゼロが基準です。警告は抑制せずに修正します。

## 制限事項

- 書き込みはこれまで2台のキーボードを使ってWindows 11で確認しました。キー6個・ノブ1つの`1189:8890`はARM64とx64の両方のPCで、新しいファームウェアのキー12個・ノブ2つの`514C:8850`はARM64で確認しています。Windows 10ではまだ確認していません。それ以外のキーボードには、メーカーのアプリと同じように書き込みます。コマンドはmacOSアプリのものとバイト単位まで同じで、テストで守っていますが、実機ではまだ確認できていません。
- `1189:8890`と種類を知らせないキーボードは現在の設定を知らせないため、アプリで設定したものだけが表示されます。
- 一度に開くキーボードは1台です。複数接続すると、接続状態の横のボタンで選びます。アプリは前回選んだものを、なければ最初に見つけたもの（`1189:8890`が優先）を開きます。
- マウスの移動とドラッグには対応していません。

## ライセンス

MIT — [LICENSE](../LICENSE)を参照してください。冒頭のアイコンはCC BY-SA 4.0 — [LICENSES/CC-BY-SA-4.0.txt](../LICENSES/CC-BY-SA-4.0.txt)を参照してください。

アプリにはMicrosoftのコンポーネント（Windows App SDKとWindows SDK for .NET）も同梱されており、これらはMITではなくMicrosoft独自のライセンス条項に従います。その条項はアプリの設定 ▸ 情報 ▸ **同梱ソフトウェア**にあり、アプリを使うことは、これらのコンポーネントについてその条項に同意することを意味します。
