<p align="center"><img src="../.github/icon.png" width="128" height="128" alt="Biểu tượng ứng dụng"></p>

<h1 align="center">Công cụ cấu hình bàn phím macro cho Windows</h1>

<p align="center">
  <img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white">
  <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6">
  <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white">
  <a href="../LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"></a>
</p>

<p align="center"><a href="../README.md">English</a> · <a href="README.ko.md">한국어</a> · <a href="README.de.md">Deutsch</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a> · <a href="README.id.md">Bahasa Indonesia</a> · <a href="README.it.md">Italiano</a> · <a href="README.ja.md">日本語</a> · <a href="README.pl.md">Polski</a> · <a href="README.pt-BR.md">Português (Brasil)</a> · <b>Tiếng Việt</b> · <a href="README.zh-Hans.md">简体中文</a> · <a href="README.zh-Hant.md">繁體中文</a></p>

Ứng dụng Windows dùng để ghi thiết lập phím vào những bàn phím macro USB cỡ nhỏ — loại được bán từ bản chỉ có một núm
xoay cho đến bản 21 phím, với tối đa bốn núm xoay. Với từng phím và từng thao tác của núm xoay (xoay sang trái, nhấn,
xoay sang phải), bạn có thể gán thao tác nhấn phím, phím đa phương tiện hoặc tác vụ chuột cho lớp 1–3, và đặt cả đèn
của bàn phím. Đây là phiên bản Windows của một ứng dụng macOS được làm theo cùng các quy tắc, và dùng cùng các lệnh.

<p align="center"><img src="../.github/devices.png" width="560" alt="Hai bàn phím macro USB nhỏ: 6 phím và 1 núm xoay, 12 phím và 2 núm xoay"></p>

| Bàn phím | Bố cục | Đọc lại | Độ trễ | Đèn | Thử thực tế |
|---|---|---|---|---|---|
| `1189:8890` | bạn tự đặt — phím (tối đa 12), số phím mỗi hàng, núm xoay (tối đa 2) | không | không có | ba chế độ | đã thử — 6 phím, 1 núm xoay |
| nhà sản xuất `1189` hoặc `514C`, sản phẩm `8840` · `8842` · `8830`–`8833` · `8850` · `8851`, chương trình cơ sở mới | theo thông tin bàn phím báo — 26 bố cục | ngay khi kết nối | cho từng lần nhấn | một chế độ, và màu cho từng phím 1–16 | đã thử — `514C:8850`, 12 phím, 2 núm xoay |
| cùng loại bàn phím, chương trình cơ sở cũ | theo thông tin bàn phím báo | ngay khi kết nối | mỗi phím một giá trị | màu và chế độ | chưa |
| cùng loại bàn phím, không cho biết loại | bạn tự chọn — 26 bố cục | không | không có | màu và chế độ | chưa |

`1189:8890` là bàn phím mà công cụ cấu hình Windows của nhà sản xuất, "MINI KeyBoard", hỗ trợ; các bàn phím còn lại là
những bàn phím mà ứng dụng đa nền tảng của nhà sản xuất, "MINI_KEYBOARD", tìm kiếm. Ứng dụng dùng lệnh của cả hai công
cụ. Bàn phím loại thứ hai thường cho biết có bao nhiêu phím, bao nhiêu núm xoay và đang chạy chương trình cơ sở nào, nên
ứng dụng vẽ đúng bố cục mà ứng dụng của nhà sản xuất vẽ; bàn phím không cho biết thì dùng bố cục bạn chọn trong số đó.
Ứng dụng của nhà sản xuất không biết đến `1189:8890`, và chính công cụ của nó cũng không vẽ hình dạng, nên không thể
biết hình dạng thực tế. Vì vậy, bạn chỉ cần đặt số phím, số núm xoay và số phím trên mỗi hàng, ứng dụng sẽ vẽ ra một bố
cục ngay ngắn đúng như vậy.

Ứng dụng có 13 ngôn ngữ — tiếng Anh, tiếng Hàn, tiếng Nhật, tiếng Trung (Giản thể và Phồn thể), tiếng Đức, tiếng Tây Ban
Nha, tiếng Pháp, tiếng Ý, tiếng Bồ Đào Nha (Brazil), tiếng Ba Lan, tiếng Indonesia và tiếng Việt. Ứng dụng theo ngôn ngữ
của Windows (hoặc ngôn ngữ bạn chọn trong Cài đặt); với các ngôn ngữ khác, ứng dụng hiển thị bằng tiếng Anh.

Mã nguồn trong kho này được cấp phép MIT. Một số phần của ứng dụng phát hành — tên, biểu tượng và các liên kết — không
nằm trong mã nguồn này. Biểu tượng ở đầu trang chỉ để minh họa: biểu tượng này không thuộc phạm vi giấy phép MIT mà được
cấp phép CC BY-SA 4.0 ([`LICENSES/CC-BY-SA-4.0.txt`](../LICENSES/CC-BY-SA-4.0.txt)), và không có trong ứng dụng được
build từ kho này.

## macOS, Windows, Linux và web

MINIKeyboard có bốn phiên bản làm theo cùng quy tắc — cùng lệnh, cùng 13 ngôn ngữ. Ba ứng dụng dùng cùng các bàn phím, còn phiên bản web, chạy trong trình duyệt, dùng tất cả trừ `1189:8890`.

| Kho mã | GitHub |
|---|---|
| **MINIKeyboardApple**<br><img alt="macOS 14+" src="https://img.shields.io/badge/macOS-14%2B-0A84FF?logo=apple&logoColor=white"> <img alt="Universal: Apple Silicon + Intel" src="https://img.shields.io/badge/Universal-Apple%20Silicon%20%2B%20Intel-5E5CE6"> <img alt="Swift 6" src="https://img.shields.io/badge/Swift-6-F05138?logo=swift&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardApple](https://github.com/hobbyworker/MINIKeyboardApple) |
| **MINIKeyboardWin**<br><img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white"> <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6"> <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardWin](https://github.com/hobbyworker/MINIKeyboardWin) |
| **MINIKeyboardLinux**<br><img alt="Linux" src="https://img.shields.io/badge/Linux-GTK%204%20·%20libadwaita-4A86CF?logo=linux&logoColor=white"> <img alt="Flatpak: x86_64 · aarch64" src="https://img.shields.io/badge/Flatpak-x86__64%20·%20aarch64-4A90D9?logo=flatpak&logoColor=white"> <img alt="Rust" src="https://img.shields.io/badge/Rust-2024-B7410E?logo=rust&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardLinux](https://github.com/hobbyworker/MINIKeyboardLinux) |
| **MINIKeyboardWeb**<br><img alt="Chrome · Edge" src="https://img.shields.io/badge/Chrome%20·%20Edge-WebHID-4285F4?logo=googlechrome&logoColor=white"> <img alt="Windows · macOS · Linux" src="https://img.shields.io/badge/Windows%20·%20macOS%20·%20Linux-5E5CE6"> <img alt="TypeScript · Svelte 5" src="https://img.shields.io/badge/TypeScript-Svelte%205-3178C6?logo=typescript&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardWeb](https://github.com/hobbyworker/MINIKeyboardWeb) |

## Tải xuống

Hãy tải trình cài đặt phù hợp với PC của bạn từ trang [Releases](https://github.com/hobbyworker/MINIKeyboardWin/releases)
rồi chạy — `MINIKeyboard-<phiên bản>-x64-Setup.exe` cho Intel và AMD, `-arm64-Setup.exe` cho ARM. Ứng dụng được cài vào
thư mục người dùng của bạn mà không cần quyền quản trị, kèm lối tắt trên màn hình nền và trong menu Bắt đầu. Trình cài
đặt không được ký, nên lần chạy đầu tiên, Windows sẽ hiện "Windows đã bảo vệ PC của bạn": hãy chọn **Thông tin thêm**,
rồi chọn **Vẫn chạy**.

Ứng dụng tự cập nhật. Ứng dụng tìm phiên bản mới lúc khởi chạy và mỗi ngày một lần
(**Cài đặt ▸ Ứng dụng ▸ Tự động kiểm tra bản cập nhật**), và cả khi bạn chọn ⋯ ▸ **Kiểm tra bản cập nhật…**. Khi có
phiên bản mới, hãy chọn **Cập nhật và khởi chạy lại** trên thanh ở đầu cửa sổ. Thiết lập của bạn vẫn được giữ nguyên.
Các tệp `.nupkg` và `releases.*.json` trên trang Releases là những tệp ứng dụng tải về khi cập nhật; bạn không cần tải
chúng. Để gỡ ứng dụng, hãy dùng Cài đặt ▸ Ứng dụng ▸ Ứng dụng đã cài đặt của Windows.

## Yêu cầu

- Windows 10 phiên bản 2004 trở lên, hoặc Windows 11, trên x64 hoặc ARM64
- Không cần trình điều khiển và không cần cấp quyền: cả hai loại đều được mở dưới dạng thiết bị HID.
  - `1189:8890` được ghi qua kênh cấu hình của nó (giao diện USB 1), giống như công cụ của nhà sản xuất. Ứng dụng
    không bao giờ đọc nội dung bạn gõ.
  - Các bàn phím còn lại được ghi qua giao diện USB đầu tiên, giống như ứng dụng của nhà sản xuất. Trong lúc đó, bàn
    phím vẫn gõ bình thường.

## Build

Cần .NET 10 SDK (10.0.402 trở lên) — không cần Visual Studio. Ứng dụng chỉ build được trên Windows; ba thư viện và các
kiểm thử build được trên mọi hệ điều hành.

```powershell
copy Brand.props.example Brand.props                                 # một lần — rồi điền BrandName
dotnet build src/MINIKeyboard/MINIKeyboard.csproj -p:Platform=x64    # trên PC ARM, -p:Platform=ARM64
dotnet test --project tests/MINIKeyboard.Domain.Tests                # cả Data.Tests, Presentation.Tests — không đụng đến bàn phím
```

Ứng dụng sau khi build nằm ở `src/MINIKeyboard/bin/<x64 hoặc ARM64>/Debug/net10.0-windows10.0.26100.0/MINIKeyboard.exe`.

Tên, biểu tượng và các liên kết của ứng dụng không có trong kho — hãy điền của riêng bạn vào `Brand.props`. Nếu không có
tên (`BrandName`), chỉ riêng ứng dụng dừng lại và yêu cầu tên; các thư viện và kiểm thử vẫn build như bình thường. Biểu
tượng, dòng bản quyền và các liên kết là tùy chọn — nếu không có biểu tượng, ứng dụng dùng biểu tượng mặc định của
Windows.

Mỗi bản build là một trong ba loại (`-p:Distribution=…`). `Source`, loại mặc định, không có tính năng cập nhật trong ứng
dụng. `Direct` được cài và cập nhật bằng [Velopack](https://velopack.io): `BrandUpdateUrl` cho biết nơi có các phiên bản
mới — một kho GitHub (trang Releases của kho đó), hoặc bất kỳ địa chỉ hay thư mục nào khác chứa feed của Velopack.
`Store` là chỗ dành cho bản build Microsoft Store, hiện vẫn đang được chuẩn bị.

⋯ ▸ **Trình mô phỏng** cho phép dùng thử ứng dụng mà không cần bàn phím đó: chọn `1189:8890`, một bố cục của chương
trình cơ sở mới, hoặc bàn phím không cho biết loại, ứng dụng sẽ hoạt động như thể bàn phím đang được cắm vào — với cùng
các lệnh, không có gì được gửi qua USB. Trong lúc dùng, một thanh màu cam ở đầu cửa sổ sẽ báo điều đó, và
**Quay lại bàn phím qua USB** đưa bạn trở lại. Bản build Debug cho chọn mọi loại (các ID báo cáo khác của `1189:8890`,
chương trình cơ sở cũ), và `MINIKEYBOARD_SIMULATED_KEYBOARD` (ví dụ `8890-3`) khởi động ứng dụng với bàn phím đó. Trong
⋯ ▸ **Nhật ký thiết bị** (Ctrl+Alt+L), bạn có thể xem mọi báo cáo đã ghi và mọi phản hồi nhận được.

## Cách dùng

1. Kết nối bàn phím qua USB. Ứng dụng tìm bàn phím và hỏi loại nếu đó là bàn phím loại thứ hai. Nếu bàn phím trả lời,
   thiết lập được đọc ngay.
2. Với bàn phím không cho biết hình dạng, lần đầu bạn chọn bố cục — một trong các bố cục của ứng dụng nhà sản xuất, hoặc
   với `1189:8890` là số phím, số phím mỗi hàng và số núm xoay — cùng hướng đặt bàn phím trước mặt bạn. Ứng dụng ghi nhớ
   theo từng USB ID. Sau này bạn có thể đổi bằng **Hình dạng…** và **Xoay** (Ctrl+T). Bàn phím đã cho biết hình dạng thì
   chỉ xoay được.
3. Chọn lớp, rồi chọn phím hoặc thao tác núm xoay trên hình bàn phím.
4. Gán tác vụ.
   - **Phím** — giữ phím bổ trợ (Ctrl, Alt, Shift, Win và các phím bên phải), rồi nhấp vào phím. Mỗi lần nhấp thêm một
     lần nhấn. **Chỉ phím bổ trợ** biến phím đó thành phím bổ trợ. **QWERTY** xếp phím như bàn phím đầy đủ, còn **ABC**
     xếp theo nhóm. Chỉ nhấp được những phím mà bàn phím nhận.
   - **Đa phương tiện**, **Chuột** — thay thế tác vụ trước đó của phím.
   - **LED** — đèn của lớp. Với chương trình cơ sở mới, chọn một trong các phím 1–16 để đặt màu cho phím đó.
   - **Độ trễ** — trên chương trình cơ sở có ghi nhớ độ trễ.
5. **Lưu** (Ctrl+S) ghi những gì đã thay đổi. **Đọc** (Ctrl+R) lấy thiết lập hiện tại từ bàn phím đọc được.

Trong ⋯ ▸ **Cài đặt** (Ctrl+,), bạn chọn ngôn ngữ giao diện (một trong 13 ngôn ngữ, hoặc ngôn ngữ của Windows) và chế độ
sáng hoặc tối. Ứng dụng tải từ trang Releases còn có **Ứng dụng ▸ Tự động kiểm tra bản cập nhật** ở đó. Trong
**Giới thiệu** ở dưới cùng, bạn xem được phiên bản, các liên kết và **Phần mềm đi kèm** — giấy phép của phần mềm đi kèm
ứng dụng.

Trong **Cài đặt ▸ Khắc phục lỗi firmware**, **Ngăn lặp phím sau khi thay đổi đèn** được bật sẵn. Firmware của
`1189:8890` lặp lại không ngừng ký tự đầu tiên của phím sau khi lưu đèn (phần mềm của nhà sản xuất cũng vậy), nên ứng
dụng gửi thêm một lệnh lưu không kèm phím nào sau đèn. Khi tắt, ứng dụng chỉ gửi đúng những gì phần mềm của nhà sản xuất
gửi. Bàn phím này cũng quên cài đặt đèn khi rút ra.

## Cấu trúc

Các quy tắc nằm trong ba thư viện. Phụ thuộc chỉ hướng vào trong, và lớp nào vươn ra ngoài sẽ không biên dịch được.

```
src/
├── MINIKeyboard.Domain/        bàn phím, tác vụ, các nút điều khiển của từng bố cục và những gì mỗi bàn phím chứa được; port; use case (chỉ thư viện lớp cơ sở của .NET)
├── MINIKeyboard.Data/          lệnh của từng bàn phím và số hiệu các nút điều khiển; tìm bàn phím và ghi qua HID (SetupAPI, hid.dll); bàn phím mô phỏng
├── MINIKeyboard.Presentation/  view model và các giá trị màn hình vẽ — hình vẽ bố cục, bảng phím và màu (không có framework UI)
├── MINIKeyboard.Views/         WinUI 3 — chỉ thấy presentation; bảng dịch cho từng ngôn ngữ (Translations/)
└── MINIKeyboard/               phần lắp ráp (composition root) — dự án duy nhất biết mọi dự án khác. Direct/ chỉ có trong bản tải trực tiếp (Velopack)
tests/                          xUnit — byte của mọi lệnh, bố cục, chỉnh sửa, trình chỉnh sửa
Brand.props.example             tên, biểu tượng, liên kết và giá trị của từng loại build — sao chép thành Brand.props rồi điền vào
LICENSES/                       giấy phép của biểu tượng ở đầu trang (CC BY-SA 4.0)
```

Tiêu chuẩn là không có cảnh báo nào. Cảnh báo được sửa chứ không bị tắt đi.

## Hạn chế

- Đến nay, việc ghi đã được kiểm tra trên hai bàn phím, trên Windows 11: `1189:8890` có 6 phím và 1 núm xoay, cả trên
  PC ARM64 lẫn x64, và `514C:8850` có 12 phím và 2 núm xoay, chạy chương trình cơ sở mới, trên ARM64. Việc ghi chưa
  được thử trên Windows 10. Các bàn phím còn lại được ghi đúng theo cách ứng dụng của nhà sản xuất ghi — các lệnh
  giống hệt lệnh của ứng dụng macOS đến từng byte và được kiểm thử giữ đúng, nhưng chưa bàn phím nào được thử trên
  thiết bị thật.
- `1189:8890` và các bàn phím không cho biết loại không báo lại thiết lập hiện tại, nên chỉ thấy được những gì đã đặt
  trong ứng dụng.
- Mỗi lần chỉ mở một bàn phím. Nếu cắm nhiều bàn phím, nút bên cạnh trạng thái kết nối dùng để chọn; ứng dụng mở bàn
  phím đã chọn lần trước, nếu không thì bàn phím tìm thấy đầu tiên — `1189:8890` được ưu tiên.
- Không hỗ trợ di chuyển chuột và kéo thả.

## Giấy phép

MIT — xem [LICENSE](../LICENSE). Biểu tượng ở đầu trang: CC BY-SA 4.0 — xem
[LICENSES/CC-BY-SA-4.0.txt](../LICENSES/CC-BY-SA-4.0.txt).

Ứng dụng còn đi kèm các thành phần của Microsoft — Windows App SDK và Windows SDK for .NET — không theo giấy phép MIT
mà theo điều khoản riêng của Microsoft. Các điều khoản đó nằm trong ứng dụng, ở Cài đặt ▸ Giới thiệu ▸
**Phần mềm đi kèm**, và việc dùng ứng dụng nghĩa là chấp nhận chúng đối với các thành phần đó.
