<p align="center"><img src="../.github/icon.png" width="128" height="128" alt="Ikon aplikasi"></p>

<h1 align="center">Alat pengatur papan ketik makro untuk Windows</h1>

<p align="center">
  <img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white">
  <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6">
  <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white">
  <a href="../LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"></a>
</p>

<p align="center"><a href="../README.md">English</a> · <a href="README.ko.md">한국어</a> · <a href="README.de.md">Deutsch</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a> · <b>Bahasa Indonesia</b> · <a href="README.it.md">Italiano</a> · <a href="README.ja.md">日本語</a> · <a href="README.pl.md">Polski</a> · <a href="README.pt-BR.md">Português (Brasil)</a> · <a href="README.vi.md">Tiếng Việt</a> · <a href="README.zh-Hans.md">简体中文</a> · <a href="README.zh-Hant.md">繁體中文</a></p>

Aplikasi Windows untuk menulis pengaturan tombol ke papan ketik makro USB berukuran kecil — papan ketik yang dijual
mulai dari yang hanya punya satu kenop hingga yang punya 21 tombol, dengan kenop sampai empat buah. Untuk setiap
tombol dan setiap tindakan kenop (putar ke kiri, tekan, putar ke kanan), Anda dapat mengatur tekanan tombol, tombol
multimedia, atau tindakan tetikus di lapisan 1–3, serta pencahayaan papan ketik. Aplikasi ini adalah versi Windows
dari aplikasi macOS yang dibuat dengan aturan yang sama, dan memakai perintah yang sama.

<p align="center"><img src="../.github/devices.png" width="560" alt="Dua papan ketik makro USB kecil: 6 tombol dan 1 kenop, 12 tombol dan 2 kenop"></p>

| Papan ketik | Tata letak | Membaca kembali | Tunda | Pencahayaan | Diuji langsung |
|---|---|---|---|---|---|
| `1189:8890` | Anda atur sendiri — tombol (hingga 12), tombol per baris, kenop (hingga 2) | tidak bisa | tidak ada | tiga mode | sudah — 6 tombol, 1 kenop |
| produsen `1189` atau `514C`, produk `8840` · `8842` · `8830`–`8833` · `8850` (dan `514C:8851`), firmware baru | sesuai yang dilaporkan papan ketik — 26 tata letak | langsung saat terhubung | per tekanan | satu mode, dan warna untuk tombol 1–16 | belum |
| papan ketik yang sama, firmware lama | sesuai yang dilaporkan papan ketik | saat Anda minta | satu per tombol | warna dan mode | belum |
| papan ketik yang sama, tidak memberi tahu jenisnya | Anda pilih sendiri — 26 tata letak | tidak bisa | tidak ada | warna dan mode | belum |

`1189:8890` adalah papan ketik yang ditangani alat pengaturan Windows dari produsen, "MINI KeyBoard"; sisanya adalah
papan ketik yang dicari oleh aplikasi lintas platform dari produsen, "MINI_KEYBOARD". Aplikasi ini memakai perintah dari
kedua alat tersebut. Papan ketik jenis kedua biasanya memberi tahu jumlah tombol dan kenopnya serta firmware yang
dipakainya, sehingga aplikasi menggambar tata letaknya persis seperti aplikasi produsen; papan ketik yang tidak memberi
tahu memakai salah satu tata letak itu yang Anda pilih. `1189:8890` tidak dikenal oleh aplikasi produsen, dan alatnya
sendiri pun tidak menggambar bentuknya, jadi bentuk aslinya tidak diketahui. Karena itu, cukup tentukan jumlah tombol
dan kenopnya serta jumlah tombol dalam satu baris, lalu aplikasi akan menggambarnya dengan rapi sesuai angka itu.

Aplikasi ini tersedia dalam 13 bahasa — Inggris, Korea, Jepang, Tionghoa (Sederhana dan Tradisional), Jerman, Spanyol,
Prancis, Italia, Portugis (Brasil), Polandia, Indonesia, dan Vietnam. Aplikasi mengikuti bahasa Windows (atau bahasa
yang Anda pilih di Pengaturan); untuk bahasa lainnya, aplikasi tampil dalam bahasa Inggris.

Kode sumber di repositori ini berlisensi MIT. Sebagian dari aplikasi yang didistribusikan — nama, ikon, dan tautannya —
tidak termasuk dalam kode sumber ini. Ikon di bagian atas hanya ditampilkan sebagai gambaran: ikon itu tidak dicakup
lisensi MIT, melainkan berlisensi CC BY-SA 4.0 ([`LICENSES/CC-BY-SA-4.0.txt`](../LICENSES/CC-BY-SA-4.0.txt)), dan tidak
disertakan dalam aplikasi yang dibangun dari repositori ini.

## Unduh

Unduh penginstal yang sesuai dengan PC Anda dari [Releases](https://github.com/hobbyworker/MINIKeyboardWin/releases),
lalu jalankan — `MINIKeyboard-<versi>-x64-Setup.exe` untuk Intel dan AMD, serta `-arm64-Setup.exe` untuk ARM. Aplikasi
dipasang di folder pengguna Anda tanpa hak administrator, dan pintasannya dibuat di desktop serta di menu Mulai.
Penginstal ini tidak ditandatangani, jadi saat pertama kali dijalankan, Windows menampilkan "Windows melindungi PC Anda".
Pilih **Info selengkapnya**, lalu **Tetap jalankan**.

Aplikasi memperbarui dirinya sendiri. Aplikasi mencari versi baru saat dijalankan dan sekali sehari
(**Pengaturan ▸ Aplikasi ▸ Periksa pembaruan secara otomatis**), dan juga saat Anda memilih ⋯ ▸ **Periksa Pembaruan…**.
Jika ada versi baru, pilih **Perbarui dan Luncurkan Ulang** di bilah di bagian atas jendela. Pengaturan Anda tetap
tersimpan. File `.nupkg` dan `releases.*.json` di Releases adalah file yang diunduh aplikasi saat memperbarui diri; Anda
tidak perlu mengunduhnya. Untuk menghapus aplikasi, buka Pengaturan ▸ Aplikasi ▸ Aplikasi yang diinstal di Windows.

## Persyaratan

- Windows 10 versi 2004 atau yang lebih baru, atau Windows 11, pada x64 atau ARM64
- Tidak memerlukan driver maupun izin apa pun: kedua jenis papan ketik dibuka sebagai perangkat HID.
  - `1189:8890` ditulis melalui saluran konfigurasinya (antarmuka USB 1), seperti yang dilakukan alat produsen.
    Aplikasi tidak pernah membaca apa yang Anda ketik.
  - Papan ketik lainnya ditulis melalui antarmuka USB pertamanya, seperti yang dilakukan aplikasi produsen. Selama itu,
    papan ketik tetap bisa dipakai mengetik seperti biasa.

## Build

Diperlukan .NET 10 SDK (10.0.402 atau yang lebih baru) — Visual Studio tidak diperlukan. Aplikasinya hanya bisa
dibangun di Windows; ketiga pustaka dan tesnya bisa dibangun di sistem operasi mana pun.

```powershell
copy Brand.props.example Brand.props                                 # sekali saja — lalu isi BrandName
dotnet build src/MINIKeyboard/MINIKeyboard.csproj -p:Platform=x64    # di PC ARM, -p:Platform=ARM64
dotnet test --project tests/MINIKeyboard.Domain.Tests                # juga Data.Tests, Presentation.Tests — tidak ada papan ketik yang disentuh
```

Hasil build aplikasinya ada di `src/MINIKeyboard/bin/<x64 atau ARM64>/Debug/net10.0-windows10.0.26100.0/MINIKeyboard.exe`.

Nama, ikon, dan tautan aplikasi tidak ada di repositori — isi milik Anda sendiri di `Brand.props`. Tanpa nama
(`BrandName`), hanya aplikasinya yang berhenti dan meminta nama; pustaka dan tes tetap dibangun seperti biasa. Ikon,
baris hak cipta, dan tautan tidak wajib diisi — tanpa ikon, aplikasi memakai ikon bawaan Windows.

Setiap build adalah salah satu dari tiga jenis (`-p:Distribution=…`). `Source`, jenis bawaan, tidak punya pembaruan di
dalam aplikasi. `Direct` dipasang dan diperbarui oleh [Velopack](https://velopack.io): `BrandUpdateUrl` menunjukkan
tempat versi baru berada — repositori GitHub (Releases-nya), atau alamat maupun folder lain yang berisi feed Velopack.
`Store` adalah tempat untuk build Microsoft Store, dan masih dalam persiapan.

Dengan ⋯ ▸ **Simulator**, Anda bisa mencoba aplikasi tanpa papan ketik tersebut. Pilih `1189:8890`, tata letak dari
firmware baru, atau papan ketik yang tidak memberi tahu jenisnya, dan aplikasi akan bekerja seolah-olah papan ketik itu
tercolok — dengan perintah yang sama, tanpa ada yang dikirim ke USB. Selama simulator dipakai, bilah oranye di bagian
atas jendela memberi tahu hal itu, dan **Kembali ke papan ketik di USB** membawa Anda kembali. Build Debug menyediakan
semua jenis (ID laporan lain milik `1189:8890`, firmware lama), dan dengan `MINIKEYBOARD_SIMULATED_KEYBOARD` (misalnya
`8890-3`), aplikasi langsung dimulai dengan papan ketik itu. Di ⋯ ▸ **Log Perangkat** (Ctrl+Alt+L), Anda bisa melihat
semua laporan yang ditulis dan semua jawaban yang diterima.

## Cara menggunakan

1. Hubungkan papan ketik melalui USB. Aplikasi menemukan papan ketik dan menanyakan jenisnya jika papan ketik itu dari
   jenis kedua. Jika firmware-nya baru, pengaturannya langsung dibaca.
2. Untuk papan ketik yang tidak memberi tahu bentuknya, pertama kali Anda menentukan tata letaknya — salah satu tata
   letak aplikasi produsen, atau untuk `1189:8890`, jumlah tombol, tombol per baris, dan kenop — serta arah posisinya
   di depan Anda. Aplikasi mengingatnya untuk setiap ID USB. Nanti Anda bisa mengubahnya dengan **Bentuk…** dan
   **Putar** (Ctrl+T). Papan ketik yang sudah memberi tahu bentuknya hanya bisa diputar.
3. Pilih lapisan, lalu pilih tombol atau tindakan kenop pada gambar papan ketik.
4. Berikan tindakannya.
   - **Tombol** — tahan tombol pengubah (Ctrl, Alt, Shift, Win, dan yang di sisi kanan), lalu klik tombol. Setiap
     klik menambahkan satu tekanan. **Hanya pengubah** menjadikan tombol itu sebagai tombol pengubah. **QWERTY**
     menata tombol seperti pada papan ketik ukuran penuh, sedangkan **ABC** menatanya per kelompok. Hanya tombol yang
     diterima papan ketik yang bisa diklik.
   - **Multimedia**, **Tetikus** — menggantikan tindakan tombol sebelumnya.
   - **LED** — pencahayaan lapisan. Pada firmware baru, pilih salah satu tombol 1–16 untuk memberinya warna.
   - **Tunda** — pada firmware yang menyimpan tunda.
5. **Simpan** (Ctrl+S) menulis perubahan. **Baca** (Ctrl+R) mengambil pengaturan saat ini dari papan ketik yang bisa
   dibaca.

Di ⋯ ▸ **Pengaturan** (Ctrl+,), Anda memilih bahasa antarmuka (salah satu dari 13 bahasa, atau bahasa Windows) serta
tampilan terang atau gelap. Aplikasi yang diunduh dari Releases juga punya
**Aplikasi ▸ Periksa pembaruan secara otomatis** di sana. Di **Tentang**, di bagian paling bawah, Anda bisa melihat versi,
tautan, dan **Perangkat Lunak yang Disertakan** — lisensi perangkat lunak yang disertakan bersama aplikasi.

Di **Pengaturan ▸ Penanganan bug firmware**, **Cegah pengulangan tombol setelah mengubah pencahayaan** aktif secara
bawaan. Firmware `1189:8890` mengulang karakter pertama sebuah tombol tanpa henti setelah pencahayaannya disimpan
(perangkat lunak pabrikannya juga begitu), jadi aplikasi mengirim satu perintah simpan lagi, tanpa tombol, setelah
pencahayaan. Jika dinonaktifkan, aplikasi hanya mengirim hal yang sama dengan perangkat lunak pabrikan. Papan ketik ini
juga melupakan pencahayaannya saat dicabut.

## Struktur

Aturannya berada di tiga pustaka. Ketergantungannya hanya mengarah ke dalam, dan lapisan yang menjangkau ke luar tidak
akan terkompilasi.

```
src/
├── MINIKeyboard.Domain/        papan ketik, tindakan, kontrol setiap tata letak, dan apa yang bisa ditampung tiap papan ketik; port; use case (hanya pustaka kelas dasar .NET)
├── MINIKeyboard.Data/          perintah setiap papan ketik dan nomor kontrolnya; menemukan papan ketik dan menulis lewat HID (SetupAPI, hid.dll); papan ketik simulasi
├── MINIKeyboard.Presentation/  view model dan nilai yang digambar layar — gambar tata letak, tabel tombol dan warna (tanpa framework UI)
├── MINIKeyboard.Views/         WinUI 3 — hanya melihat presentation; tabel terjemahan per bahasa (Translations/)
└── MINIKeyboard/               composition root — satu-satunya proyek yang mengenal semuanya. Direct/ hanya masuk ke versi unduhan langsung (Velopack)
tests/                          xUnit — byte setiap perintah, tata letak, pengeditan, editor
Brand.props.example             nama, ikon, tautan, dan nilai untuk setiap jenis build — salin ke Brand.props lalu isi
LICENSES/                       lisensi ikon di bagian atas (CC BY-SA 4.0)
```

Standarnya adalah nol peringatan. Peringatan diperbaiki, bukan dimatikan.

## Batasan

- Sejauh ini, penulisan baru diperiksa pada satu papan ketik, yaitu `1189:8890` dengan 6 tombol dan 1 kenop, di
  Windows 11, baik pada PC ARM64 maupun x64. Penulisan belum diperiksa di Windows 10. Papan ketik lainnya ditulis persis
  seperti cara aplikasi produsennya menulis — perintahnya sama dengan milik aplikasi macOS sampai ke byte-nya, dan tes
  menjaganya — tetapi belum ada satu pun yang diperiksa langsung pada perangkatnya.
- `1189:8890` dan papan ketik yang tidak memberi tahu jenisnya tidak melaporkan pengaturan saat ini, jadi yang terlihat
  hanya apa yang diatur di aplikasi.
- Hanya satu papan ketik dalam satu waktu. Jika beberapa papan ketik tercolok, aplikasi memakai yang pertama ditemukan —
  `1189:8890` didahulukan.
- Gerakan dan seret tetikus tidak tersedia.

## Lisensi

MIT — lihat [LICENSE](../LICENSE). Ikon di bagian atas: CC BY-SA 4.0 — lihat
[LICENSES/CC-BY-SA-4.0.txt](../LICENSES/CC-BY-SA-4.0.txt).

Aplikasi ini juga membawa komponen Microsoft — Windows App SDK dan Windows SDK for .NET — yang tidak berlisensi MIT,
melainkan tunduk pada ketentuan Microsoft sendiri. Ketentuan tersebut ada di aplikasi, di Pengaturan ▸ Tentang ▸
**Perangkat Lunak yang Disertakan**, dan menggunakan aplikasi berarti menerima ketentuan itu untuk komponen tersebut.
