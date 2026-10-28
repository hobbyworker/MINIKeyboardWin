<p align="center"><img src="../.github/icon.png" width="128" height="128" alt="Icona dell’app"></p>

<h1 align="center">Configuratore di tastiere macro per Windows</h1>

<p align="center">
  <img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white">
  <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6">
  <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white">
  <a href="../LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"></a>
</p>

<p align="center"><a href="../README.md">English</a> · <a href="README.ko.md">한국어</a> · <a href="README.de.md">Deutsch</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a> · <a href="README.id.md">Bahasa Indonesia</a> · <b>Italiano</b> · <a href="README.ja.md">日本語</a> · <a href="README.pl.md">Polski</a> · <a href="README.pt-BR.md">Português (Brasil)</a> · <a href="README.vi.md">Tiếng Việt</a> · <a href="README.zh-Hans.md">简体中文</a> · <a href="README.zh-Hant.md">繁體中文</a></p>

Un’app per Windows che scrive le impostazioni dei tasti su piccole tastiere macro USB – quelle in commercio che vanno
da una sola manopola fino a 21 tasti, con al massimo quattro manopole. Per ogni azione di un tasto o di una manopola
(ruota a sinistra, premi, ruota a destra) imposta sui livelli 1–3 pressioni di tasti, un tasto multimediale o
un’azione del mouse, oltre all’illuminazione della tastiera. È la versione per Windows di un’app per macOS costruita
sulle stesse regole, e usa gli stessi comandi.

<p align="center"><img src="../.github/devices.png" width="560" alt="Due piccole tastiere macro USB: sei tasti e una manopola, dodici tasti e due manopole"></p>

| Tastiera | Disposizione | Lettura | Ritardo | Illuminazione | Provata |
|---|---|---|---|---|---|
| `1189:8890` | la decidi tu – tasti (fino a 12), tasti per riga, manopole (fino a 2) | no | nessuno | tre modalità | sì – 6 tasti e una manopola |
| produttore `1189` o `514C`, prodotto `8840` · `8842` · `8830`–`8833` · `8850` · `8851`, firmware più recente | secondo quanto indica la tastiera – 26 disposizioni | subito alla connessione | prima di ogni pressione | una modalità e un colore per ciascuno dei tasti 1–16 | sì – `514C:8850`, 12 tasti e 2 manopole |
| le stesse, firmware precedente | secondo quanto indica la tastiera | subito alla connessione | uno per tasto | un colore e una modalità | no |
| le stesse, senza indicare il proprio tipo | la scegli tu – 26 disposizioni | no | nessuno | un colore e una modalità | no |

La `1189:8890` è la tastiera gestita dallo strumento Windows del produttore, “MINI KeyBoard”; le altre sono quelle che
cerca l’app multipiattaforma del produttore, “MINI_KEYBOARD”. L’app usa i comandi di entrambi gli strumenti. La
maggior parte delle tastiere del secondo tipo indica quanti tasti e manopole ha e quale firmware usa, e l’app disegna
la disposizione che disegnerebbe l’app del produttore; una tastiera che non lo indica riceve, tra queste disposizioni,
quella che scegli tu. L’app del produttore non conosce la `1189:8890` e il suo stesso strumento non ne disegna la
forma, quindi la sua forma reale non è nota: indichi all’app quanti tasti e manopole ha e quanti tasti ci sono in una
riga, e l’app ne disegna una disposizione ordinata.

L’app è disponibile in 13 lingue – inglese, coreano, giapponese, cinese (semplificato e tradizionale), tedesco,
spagnolo, francese, italiano, portoghese (Brasile), polacco, indonesiano e vietnamita – e segue la lingua di Windows (o
quella che scegli nelle Impostazioni). Nelle altre lingue appare in inglese.

Il codice sorgente di questo repository è distribuito con licenza MIT. Alcune parti dell’app distribuita non fanno
parte di questo codice: il nome, l’icona e i link. L’icona in alto è mostrata solo a titolo illustrativo: non è
coperta dalla licenza MIT ma dalla licenza CC BY-SA 4.0 ([`LICENSES/CC-BY-SA-4.0.txt`](../LICENSES/CC-BY-SA-4.0.txt)),
e non è inclusa nelle build ottenute da questo repository.

## macOS, Windows e Linux

MINIKeyboard esiste in tre edizioni costruite sulle stesse regole – le stesse tastiere, gli stessi comandi, le stesse 13 lingue.

| Repository | GitHub |
|---|---|
| **MINIKeyboardApple**<br><img alt="macOS 14+" src="https://img.shields.io/badge/macOS-14%2B-0A84FF?logo=apple&logoColor=white"> <img alt="Universal: Apple Silicon + Intel" src="https://img.shields.io/badge/Universal-Apple%20Silicon%20%2B%20Intel-5E5CE6"> <img alt="Swift 6" src="https://img.shields.io/badge/Swift-6-F05138?logo=swift&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardApple](https://github.com/hobbyworker/MINIKeyboardApple) |
| **MINIKeyboardWin**<br><img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white"> <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6"> <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardWin](https://github.com/hobbyworker/MINIKeyboardWin) |
| **MINIKeyboardLinux**<br><img alt="Linux" src="https://img.shields.io/badge/Linux-GTK%204%20·%20libadwaita-4A86CF?logo=linux&logoColor=white"> <img alt="Flatpak: x86_64 · aarch64" src="https://img.shields.io/badge/Flatpak-x86__64%20·%20aarch64-4A90D9?logo=flatpak&logoColor=white"> <img alt="Rust" src="https://img.shields.io/badge/Rust-2024-B7410E?logo=rust&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardLinux](https://github.com/hobbyworker/MINIKeyboardLinux) |

## Download

Scarica dalla pagina [Releases](https://github.com/hobbyworker/MINIKeyboardWin/releases) il programma di installazione
adatto al tuo PC ed eseguilo – `MINIKeyboard-<versione>-x64-Setup.exe` per Intel e AMD, `-arm64-Setup.exe` per ARM.
Si installa nella tua cartella utente senza diritti di amministratore, con i collegamenti sul desktop e nel menu Start.
Il programma di installazione non è firmato, quindi la prima volta Windows mostra “Windows ha protetto il PC”: scegli
**Ulteriori informazioni**, poi **Esegui comunque**.

L’app si aggiorna da sola. Cerca una nuova versione all’avvio e una volta al giorno
(**Impostazioni ▸ App ▸ Cerca aggiornamenti automaticamente**), oltre che quando scegli ⋯ ▸ **Cerca aggiornamenti…**.
Se ce n’è una, scegli **Aggiorna e riapri** nella barra in cima alla finestra. Le tue impostazioni restano. I file
`.nupkg` e `releases.*.json` della pagina Releases sono ciò che l’app scarica per aggiornarsi; non ti servono. Per
rimuovere l’app, usa Impostazioni ▸ App ▸ App installate di Windows.

## Requisiti

- Windows 10 versione 2004 o successive, oppure Windows 11, su x64 o ARM64
- Nessun driver e nessuna autorizzazione: entrambi i tipi vengono aperti come dispositivi HID.
  - Sulla `1189:8890` si scrive tramite la sua interfaccia di configurazione (interfaccia USB 1), come fa lo
    strumento del produttore. L’app non legge mai ciò che digiti.
  - Le altre tastiere vengono scritte tramite la loro prima interfaccia USB, come fa l’app del produttore. Nel
    frattempo la tastiera continua a digitare normalmente.

## Compilazione

Serve l’SDK di .NET 10 (10.0.402 o successivo) – Visual Studio non è necessario. L’app si compila solo su Windows; le
tre librerie e i test si compilano ovunque.

```powershell
copy Brand.props.example Brand.props                                 # una volta sola – poi inserisci BrandName
dotnet build src/MINIKeyboard/MINIKeyboard.csproj -p:Platform=x64    # su un PC ARM, -p:Platform=ARM64
dotnet test --project tests/MINIKeyboard.Domain.Tests                # e anche Data.Tests e Presentation.Tests – non si tocca nessuna tastiera
```

L’app compilata si trova in `src/MINIKeyboard/bin/<x64 o ARM64>/Debug/net10.0-windows10.0.26100.0/MINIKeyboard.exe`.

Il nome, l’icona e i link dell’app non sono nel repository – inserisci i tuoi in `Brand.props`. Senza un nome
(`BrandName`) si ferma solo la build dell’app, che ne chiede uno; le librerie e i test si compilano così come sono.
L’icona, la riga del copyright e i link sono facoltativi – senza un’icona l’app riceve quella generica di Windows.

Una build è di uno di tre tipi (`-p:Distribution=…`). `Source`, il predefinito, non ha aggiornamenti propri. `Direct`
si installa e si aggiorna con [Velopack](https://velopack.io): `BrandUpdateUrl` indica dove si trovano le nuove
versioni – un repository GitHub (le sue Releases) oppure qualsiasi altro indirizzo o cartella che contenga il feed di
Velopack. `Store` è riservato a una build per il Microsoft Store, non ancora pronta.

⋯ ▸ **Simulatore** permette di provare l’app senza la tastiera in questione: scegli una `1189:8890`, una disposizione
del firmware più recente oppure una tastiera che non indica il proprio tipo, e l’app si comporta come se fosse
collegata – con gli stessi comandi, senza che nulla passi per l’USB. Mentre è in uso, una barra arancione in cima alla
finestra lo segnala, e **Torna alla tastiera via USB** riporta indietro. Le build Debug offrono ogni tipo (gli altri
ID report della `1189:8890`, il firmware più vecchio), e con `MINIKEYBOARD_SIMULATED_KEYBOARD` (per esempio `8890-3`)
l’app si avvia direttamente con una di queste. ⋯ ▸ **Registro dispositivo** (Ctrl+Alt+L) mostra ogni report scritto e
ogni risposta.

## Utilizzo

1. Collega la tastiera tramite USB. L’app la trova e, se è del secondo tipo, le chiede che tastiera è; se
   risponde, viene letta subito.
2. Per una tastiera che non indica la propria forma, la prima volta scegli la disposizione – una di quelle dell’app
   del produttore oppure, per la `1189:8890`, il numero di tasti, di tasti per riga e di manopole – e come è
   posizionata davanti a te. L’app ricorda entrambe le cose per quell’ID USB. In seguito le cambi con **Forma…** e
   **Ruota** (Ctrl+T); una tastiera che ha indicato la propria forma si può solo ruotare.
3. Scegli un livello, poi il tasto o l’azione della manopola sulla tastiera.
4. Assegna un’azione:
   - **Tasti** – tieni premuti i modificatori (Ctrl, Alt, Shift, Win e quelli a destra), poi fai clic sui tasti;
     ogni clic aggiunge una pressione. **Solo modificatori** trasforma il tasto in un tasto modificatore. **QWERTY**
     dispone i tasti come su una tastiera completa, **ABC** per gruppi; si può fare clic solo sui tasti che la
     tastiera accetta.
   - **Multimedia**, **Mouse** – sostituiscono ciò che faceva il tasto.
   - **LED** – l’illuminazione del livello; con il firmware più recente, seleziona uno dei tasti 1–16 per assegnargli
     un colore.
   - **Ritardo** – con i firmware che memorizzano le attese.
5. **Salva** (Ctrl+S) scrive ciò che è cambiato. **Leggi** (Ctrl+R) recupera le impostazioni attuali da una tastiera
   che consente di rileggerle.

In ⋯ ▸ **Impostazioni** (Ctrl+,) scegli la lingua dell’interfaccia (una delle 13, oppure quella di Windows) e
l’aspetto chiaro o scuro. Nell’app scaricata dalla pagina Releases lì trovi anche
**App ▸ Cerca aggiornamenti automaticamente**. In **Informazioni**, in fondo, trovi la versione, i link e **Software incluso**, cioè le licenze del software
fornito con l’app.

In **Impostazioni ▸ Soluzioni per i bug del firmware**, **Evita la ripetizione dei tasti dopo aver cambiato
l’illuminazione** è attivo per impostazione predefinita. Dopo il salvataggio dell’illuminazione, il firmware della
`1189:8890` ripete all’infinito il primo carattere di un tasto (il software del produttore fa lo stesso), quindi l’app
invia un ulteriore comando di salvataggio, senza tasti, dopo l’illuminazione. Se lo disattivi, l’app invia solo ciò
che invia il software del produttore. Questa tastiera, inoltre, dimentica l’illuminazione quando la scolleghi.

## Struttura

Le regole stanno in tre librerie le cui dipendenze puntano solo verso l’interno: uno strato che cerca di raggiungere
l’esterno non compila.

```
src/
├── MINIKeyboard.Domain/        tastiere, azioni, i controlli di ogni disposizione e ciò che ogni tastiera può contenere; le porte; casi d’uso (solo la libreria di classi di base)
├── MINIKeyboard.Data/          i comandi di ogni tastiera e i numeri dei suoi controlli; trovare la tastiera e scrivere via HID (SetupAPI, hid.dll); tastiere simulate
├── MINIKeyboard.Presentation/  view model e valori che le schermate disegnano – la geometria delle disposizioni, le tabelle di tasti e colori (nessun framework UI)
├── MINIKeyboard.Views/         WinUI 3 – vede solo Presentation; una tabella di traduzione per lingua (Translations/)
└── MINIKeyboard/               la composition root – l’unico progetto che li vede tutti. Direct/ entra solo nel download diretto (Velopack)
tests/                          xUnit – i byte di ogni comando, le disposizioni, le modifiche, l’editor
Brand.props.example             il nome, l’icona, i link e i valori di ogni build – da copiare in Brand.props e completare
LICENSES/                       la licenza dell’icona in alto (CC BY-SA 4.0)
```

Zero avvisi è il requisito. Gli avvisi si correggono, non si disattivano.

## Limiti

- Finora la scrittura è stata provata su due tastiere, su Windows 11: una `1189:8890` con sei tasti e una manopola,
  sia su PC ARM64 sia su PC x64, e una `514C:8850` con dodici tasti e due manopole, con firmware recente, su ARM64.
  Non è stata ancora provata su Windows 10. Le altre tastiere vengono scritte come le scrive l’app del loro produttore
  – i comandi sono quelli dell’app per macOS, byte per byte, e i test li fissano –, ma nessuna è stata ancora provata
  su una tastiera reale.
- La `1189:8890` e le tastiere che non indicano il proprio tipo non possono riferire le loro impostazioni, quindi si
  vede solo ciò che è stato impostato nell’app.
- Una tastiera aperta alla volta. Se ne sono collegate diverse, il pulsante accanto allo stato della connessione
  sceglie quale; l’app apre quella scelta l’ultima volta, altrimenti la prima che trova – prima una `1189:8890`.
- Lo spostamento del mouse e il trascinamento non sono disponibili.

## Licenza

MIT – vedi [LICENSE](../LICENSE). L’icona in alto: CC BY-SA 4.0 – vedi
[LICENSES/CC-BY-SA-4.0.txt](../LICENSES/CC-BY-SA-4.0.txt).

L’app contiene anche componenti Microsoft – il Windows App SDK e il Windows SDK for .NET – che non sono sotto licenza
MIT ma sotto le condizioni di Microsoft. Le condizioni si trovano nell’app, in Impostazioni ▸ Informazioni ▸
**Software incluso**, e usare l’app significa accettarle per quei componenti.
