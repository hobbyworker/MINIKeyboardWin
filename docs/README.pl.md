<p align="center"><img src="../.github/icon.png" width="128" height="128" alt="Ikona aplikacji"></p>

<h1 align="center">Konfigurator klawiatur makro na Windows</h1>

<p align="center">
  <img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white">
  <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6">
  <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white">
  <a href="../LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"></a>
</p>

<p align="center"><a href="../README.md">English</a> · <a href="README.ko.md">한국어</a> · <a href="README.de.md">Deutsch</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a> · <a href="README.id.md">Bahasa Indonesia</a> · <a href="README.it.md">Italiano</a> · <a href="README.ja.md">日本語</a> · <b>Polski</b> · <a href="README.pt-BR.md">Português (Brasil)</a> · <a href="README.vi.md">Tiếng Việt</a> · <a href="README.zh-Hans.md">简体中文</a> · <a href="README.zh-Hant.md">繁體中文</a></p>

Aplikacja na Windows, która zapisuje ustawienia klawiszy w małych klawiaturach makro USB. Takie klawiatury sprzedaje
się w wielu wersjach: od modeli z jednym pokrętłem po modele z 21 klawiszami, z maksymalnie czterema pokrętłami. Dla
każdego klawisza i każdej akcji pokrętła (obrót w lewo, naciśnięcie, obrót w prawo) aplikacja ustawia na warstwach 1–3
naciśnięcia klawiszy, klawisz multimedialny lub akcję myszy, a także podświetlenie klawiatury. To windowsowy
odpowiednik aplikacji na macOS opartej na tych samych regułach; obie używają tych samych poleceń.

<p align="center"><img src="../.github/devices.png" width="560" alt="Dwie małe klawiatury makr USB: sześć klawiszy i pokrętło, dwanaście klawiszy i dwa pokrętła"></p>

| Klawiatura | Układ | Odczyt | Opóźnienie | Podświetlenie | Sprawdzono |
|---|---|---|---|---|---|
| `1189:8890` | ustawiasz go sam — klawisze (do 12), klawisze w rzędzie, pokrętła (do 2) | nie | nie | trzy tryby | tak — 6 klawiszy i pokrętło |
| producent `1189` lub `514C`, produkt `8840` · `8842` · `8830`–`8833` · `8850` · `8851`, nowsze oprogramowanie sprzętowe | według tego, co podaje klawiatura — 26 układów | od razu po podłączeniu | przed każdym naciśnięciem | tryb i kolor dla każdego z klawiszy 1–16 | tak — `514C:8850`, 12 klawiszy i 2 pokrętła |
| te same, starsze oprogramowanie sprzętowe | według tego, co podaje klawiatura | od razu po podłączeniu | jedno na klawisz | kolor i tryb | nie |
| te same, niepodające swojego typu | wybierasz go sam — 26 układów | nie | nie | kolor i tryb | nie |

`1189:8890` to klawiatura, z którą współpracuje windowsowe narzędzie konfiguracyjne producenta „MINI KeyBoard”;
pozostałe to te, których szuka wieloplatformowa aplikacja producenta „MINI_KEYBOARD”. Aplikacja korzysta z poleceń obu
narzędzi. Większość klawiatur drugiego rodzaju podaje, ile ma klawiszy i pokręteł oraz jakie ma oprogramowanie
sprzętowe, więc aplikacja rysuje taki sam układ, jaki narysowałaby aplikacja producenta; dla klawiatury, która tego
nie podaje, używa tego z tych układów, który wybierzesz. Aplikacja producenta nie zna klawiatury `1189:8890`, a
narzędzie przeznaczone dla tej klawiatury nie rysuje żadnego kształtu, więc jej rzeczywisty kształt nie jest znany.
Dlatego to ty podajesz, ile ma klawiszy i pokręteł oraz ile klawiszy jest w rzędzie, a aplikacja rysuje je dokładnie
tak, w równym układzie.

Aplikacja jest dostępna w 13 językach — angielskim, koreańskim, japońskim, chińskim (uproszczonym i tradycyjnym),
niemieckim, hiszpańskim, francuskim, włoskim, portugalskim (Brazylia), polskim, indonezyjskim i wietnamskim — i używa
języka systemu Windows (albo tego, który wybierzesz w Ustawieniach). W pozostałych językach wyświetla się po
angielsku.

Kod źródłowy w tym repozytorium jest objęty licencją MIT. Niektóre elementy rozpowszechnianej aplikacji — jej nazwa,
ikona i linki — nie wchodzą w skład tego kodu. Ikona na górze jest pokazana wyłącznie poglądowo: nie obejmuje jej
licencja MIT, lecz CC BY-SA 4.0 ([`LICENSES/CC-BY-SA-4.0.txt`](../LICENSES/CC-BY-SA-4.0.txt)), i nie ma jej w
kompilacjach zbudowanych z tego repozytorium.

## macOS, Windows, Linux i przeglądarka

MINIKeyboard ma cztery wydania zbudowane według tych samych zasad — te same polecenia, te same 13 języków. Trzy aplikacje obsługują te same klawiatury, a wydanie webowe, działające w przeglądarce, wszystkie oprócz `1189:8890`.

| Repozytorium | GitHub |
|---|---|
| **MINIKeyboardApple**<br><img alt="macOS 14+" src="https://img.shields.io/badge/macOS-14%2B-0A84FF?logo=apple&logoColor=white"> <img alt="Universal: Apple Silicon + Intel" src="https://img.shields.io/badge/Universal-Apple%20Silicon%20%2B%20Intel-5E5CE6"> <img alt="Swift 6" src="https://img.shields.io/badge/Swift-6-F05138?logo=swift&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardApple](https://github.com/hobbyworker/MINIKeyboardApple) |
| **MINIKeyboardWin**<br><img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white"> <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6"> <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardWin](https://github.com/hobbyworker/MINIKeyboardWin) |
| **MINIKeyboardLinux**<br><img alt="Linux" src="https://img.shields.io/badge/Linux-GTK%204%20·%20libadwaita-4A86CF?logo=linux&logoColor=white"> <img alt="Flatpak: x86_64 · aarch64" src="https://img.shields.io/badge/Flatpak-x86__64%20·%20aarch64-4A90D9?logo=flatpak&logoColor=white"> <img alt="Rust" src="https://img.shields.io/badge/Rust-2024-B7410E?logo=rust&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardLinux](https://github.com/hobbyworker/MINIKeyboardLinux) |
| **MINIKeyboardWeb**<br><img alt="Chrome · Edge" src="https://img.shields.io/badge/Chrome%20·%20Edge-WebHID-4285F4?logo=googlechrome&logoColor=white"> <img alt="Windows · macOS · Linux" src="https://img.shields.io/badge/Windows%20·%20macOS%20·%20Linux-5E5CE6"> <img alt="TypeScript · Svelte 5" src="https://img.shields.io/badge/TypeScript-Svelte%205-3178C6?logo=typescript&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardWeb](https://github.com/hobbyworker/MINIKeyboardWeb) |

## Pobieranie

Pobierz instalator dla swojego komputera ze strony [Releases](https://github.com/hobbyworker/MINIKeyboardWin/releases)
i uruchom go — `MINIKeyboard-<wersja>-x64-Setup.exe` dla procesorów Intel i AMD, `-arm64-Setup.exe` dla procesorów
ARM. Aplikacja instaluje się w folderze użytkownika bez uprawnień administratora, a skróty pojawiają się na pulpicie i
w menu Start. Instalator nie jest podpisany, więc za pierwszym razem Windows wyświetla komunikat „System Windows
ochronił ten komputer”: wybierz **Więcej informacji**, a potem **Uruchom mimo to**.

Aplikacja sama się uaktualnia. Sprawdza, czy jest nowa wersja, przy uruchomieniu i raz dziennie (**Ustawienia ▸
Aplikacja ▸ Sprawdzaj uaktualnienia automatycznie**), a także po wybraniu ⋯ ▸ **Sprawdź uaktualnienia…**. Gdy nowa
wersja jest dostępna, wybierz **Uaktualnij i uruchom ponownie** na pasku u góry okna. Twoje ustawienia zostają
zachowane. Pliki `.nupkg` i `releases.*.json` ze strony Releases aplikacja pobiera przy uaktualnianiu; nie są ci
potrzebne. Aby odinstalować aplikację, otwórz w systemie Windows Ustawienia ▸ Aplikacje ▸ Zainstalowane aplikacje.

## Wymagania

- Windows 10 w wersji 2004 lub nowszej albo Windows 11, na x64 lub ARM64
- Nie są potrzebne żadne sterowniki ani uprawnienia: oba rodzaje klawiatur są otwierane jako urządzenia HID.
  - Klawiatura `1189:8890` jest zapisywana przez swój interfejs konfiguracyjny (interfejs USB 1), tak jak robi to
    narzędzie producenta. Aplikacja nigdy nie odczytuje tego, co piszesz.
  - Pozostałe klawiatury są zapisywane przez ich pierwszy interfejs USB, tak jak robi to aplikacja producenta. W tym
    czasie klawiatura nadal normalnie pisze.

## Budowanie

Potrzebny jest .NET 10 SDK (10.0.402 lub nowszy) — Visual Studio nie jest wymagane. Aplikację można zbudować tylko w
systemie Windows; trzy biblioteki i testy zbudujesz w dowolnym systemie.

```powershell
copy Brand.props.example Brand.props                                 # raz — potem wpisz BrandName
dotnet build src/MINIKeyboard/MINIKeyboard.csproj -p:Platform=x64    # na komputerze z ARM: -p:Platform=ARM64
dotnet test --project tests/MINIKeyboard.Domain.Tests                # oraz Data.Tests, Presentation.Tests — żadna klawiatura nie jest ruszana
```

Aplikacja trafia do `src/MINIKeyboard/bin/<x64 lub ARM64>/Debug/net10.0-windows10.0.26100.0/MINIKeyboard.exe`.

Nazwy, ikony i linków aplikacji nie ma w repozytorium — wpisz własne w `Brand.props`. Bez nazwy (`BrandName`) tylko
aplikacja zatrzymuje się i prosi o nią; biblioteki i testy budują się bez zmian. Ikona, wiersz praw autorskich i linki
są opcjonalne — bez ikony aplikacja ma ogólną ikonę systemu Windows.

Każda kompilacja jest jednego z trzech rodzajów (`-p:Distribution=…`). `Source`, domyślna, nie ma własnych
uaktualnień. `Direct` jest instalowana i uaktualniana przez [Velopack](https://velopack.io): `BrandUpdateUrl` wskazuje,
gdzie są nowe wersje — repozytorium GitHub (jego Releases) albo dowolny inny adres lub folder z kanałem Velopack.
`Store` to miejsce na kompilację dla Microsoft Store, która nie jest jeszcze gotowa.

⋯ ▸ **Symulator** pozwala wypróbować aplikację bez danej klawiatury: wybierz `1189:8890`, układ z nowszym
oprogramowaniem sprzętowym albo klawiaturę, która nie podaje swojego typu, a aplikacja zachowa się tak, jakby
klawiatura była podłączona — z tymi samymi poleceniami, bez wysyłania czegokolwiek przez USB. Gdy symulator jest w
użyciu, mówi o tym pomarańczowy pasek u góry okna, a **Wróć do klawiatury przez USB** przywraca prawdziwą klawiaturę.
Kompilacje Debug oferują każdy rodzaj (pozostałe ID raportu `1189:8890`, starsze oprogramowanie sprzętowe), a ze
zmienną `MINIKEYBOARD_SIMULATED_KEYBOARD` (na przykład `8890-3`) aplikacja od razu uruchamia się z wybraną
klawiaturą. ⋯ ▸ **Dziennik urządzenia** (Ctrl+Alt+L) pokazuje wszystkie zapisane raporty i wszystkie odpowiedzi.

## Korzystanie

1. Podłącz klawiaturę przez USB. Aplikacja ją znajduje, a klawiaturę drugiego rodzaju pyta, czym jest; taką, która
   odpowie, od razu odczytuje.
2. Dla klawiatury, która nie podaje swojego kształtu, przy pierwszym podłączeniu wybierasz układ — jeden z układów
   aplikacji producenta, a dla `1189:8890` liczbę klawiszy, klawiszy w rzędzie i pokręteł — oraz to, jak leży przed
   tobą. Aplikacja zapamiętuje oba ustawienia dla danego ID USB. Później zmienisz je poleceniami **Kształt…** i
   **Obróć** (Ctrl+T). Klawiaturę, która podała swój kształt, można tylko obracać.
3. Wybierz warstwę, a potem na rysunku klawiatury klawisz lub akcję pokrętła.
4. Przypisz akcję:
   - **Klawisze** — przytrzymaj modyfikatory (Ctrl, Alt, Shift, Win oraz te po prawej stronie) i klikaj klawisze;
     każde kliknięcie dodaje jedno naciśnięcie. **Tylko modyfikatory** zmienia klawisz w klawisz modyfikujący.
     **QWERTY** rozmieszcza klawisze jak na pełnowymiarowej klawiaturze, a **ABC** — w grupach. Kliknąć można tylko
     klawisze, które klawiatura obsługuje.
   - **Multimedia**, **Mysz** — zastępują to, co robił klawisz.
   - **LED** — podświetlenie warstwy; w nowszym oprogramowaniu sprzętowym wybierz jeden z klawiszy 1–16, aby nadać mu
     kolor.
   - **Opóźnienie** — w oprogramowaniu sprzętowym, które zapamiętuje opóźnienia.
5. **Zachowaj** (Ctrl+S) zapisuje zmiany. **Odczytaj** (Ctrl+R) pobiera bieżące ustawienia z klawiatury, która pozwala
   je odczytać.

⋯ ▸ **Ustawienia** (Ctrl+,) pozwalają wybrać język interfejsu (jeden z 13 lub język systemu Windows) oraz tryb jasny
lub ciemny. W aplikacji ze strony Releases jest tam też **Aplikacja ▸ Sprawdzaj uaktualnienia automatycznie**. Na
dole, w sekcji **Informacje**, są wersja, linki i **Dołączone oprogramowanie** — licencje oprogramowania dołączonego do
aplikacji.

Opcja **Zapobiegaj powtarzaniu klawiszy po zmianie podświetlenia** (**Ustawienia ▸ Obejścia błędów oprogramowania
układowego**) jest domyślnie włączona. Oprogramowanie układowe `1189:8890` po zachowaniu podświetlenia bez końca
powtarza pierwszy znak klawisza (oprogramowanie producenta działa tak samo), dlatego aplikacja po podświetleniu wysyła
jeszcze jedno polecenie zapisu, bez żadnego klawisza. Gdy opcja jest wyłączona, aplikacja wysyła tylko to, co
oprogramowanie producenta. Ta klawiatura zapomina też podświetlenie po odłączeniu.

## Struktura

Reguły znajdują się w trzech bibliotekach, których zależności są skierowane tylko do środka — warstwa sięgająca na
zewnątrz się nie kompiluje.

```
src/
├── MINIKeyboard.Domain/        klawiatury, akcje, kontrolki każdego układu i to, co mieści każda klawiatura; porty; przypadki użycia (tylko biblioteka klas bazowych)
├── MINIKeyboard.Data/          polecenia każdej klawiatury i numeracja jej kontrolek; wyszukiwanie klawiatury i zapis przez HID (SetupAPI, hid.dll); symulowane klawiatury
├── MINIKeyboard.Presentation/  modele widoków i wartości rysowane przez ekrany — geometria układów, tabele klawiszy i kolorów (bez frameworka UI)
├── MINIKeyboard.Views/         WinUI 3 — widzi tylko Presentation; tabela tłumaczeń dla każdego języka (Translations/)
└── MINIKeyboard/               composition root — jedyny projekt, który widzi wszystkie pozostałe. Direct/ trafia tylko do wersji pobieranej bezpośrednio (Velopack)
tests/                          xUnit — bajty każdego polecenia, układy, edycje, edytor
Brand.props.example             nazwa, ikona, linki i wartości dla każdego rodzaju kompilacji — skopiuj do Brand.props i uzupełnij
LICENSES/                       licencja ikony na górze (CC BY-SA 4.0)
```

Zero ostrzeżeń to standard. Ostrzeżenia się poprawia, a nie wyłącza.

## Ograniczenia

- Zapis sprawdzono dotąd na dwóch klawiaturach w systemie Windows 11: `1189:8890` z sześcioma klawiszami i pokrętłem —
  na komputerach ARM64 i x64 — oraz `514C:8850` z dwunastoma klawiszami i dwoma pokrętłami, z nowszym oprogramowaniem
  sprzętowym, na ARM64. W systemie Windows 10 jeszcze go nie sprawdzono. Pozostałe klawiatury są zapisywane tak, jak
  robi to aplikacja ich producenta: polecenia są bajt w bajt takie same jak w aplikacji na macOS i pilnują ich testy,
  ale żadnej z nich nie sprawdzono jeszcze na prawdziwej klawiaturze.
- Klawiatura `1189:8890` i klawiatury, które nie podają swojego typu, nie przekazują swoich bieżących ustawień, więc
  aplikacja pokazuje tylko to, co w niej ustawiono.
- Naraz otwarta jest jedna klawiatura. Jeśli podłączonych jest kilka, przycisk obok stanu połączenia wybiera którą;
  aplikacja otwiera ostatnio wybraną, a jeśli takiej nie ma, pierwszą znalezioną — w pierwszej kolejności `1189:8890`.
- Nie ma akcji ruchu myszy ani przeciągania.

## Licencja

MIT — zobacz [LICENSE](../LICENSE). Ikona na górze: CC BY-SA 4.0 — zobacz
[LICENSES/CC-BY-SA-4.0.txt](../LICENSES/CC-BY-SA-4.0.txt).

Aplikacja zawiera też składniki Microsoft — Windows App SDK i Windows SDK for .NET — objęte nie licencją MIT, lecz
własnymi warunkami Microsoft. Warunki te są w aplikacji, w Ustawienia ▸ Informacje ▸ **Dołączone oprogramowanie**,
a korzystanie z aplikacji oznacza ich akceptację w odniesieniu do tych składników.
