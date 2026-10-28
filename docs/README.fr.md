<p align="center"><img src="../.github/icon.png" width="128" height="128" alt="Icône de l’app"></p>

<h1 align="center">Configurateur de claviers macro pour Windows</h1>

<p align="center">
  <img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white">
  <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6">
  <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white">
  <a href="../LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"></a>
</p>

<p align="center"><a href="../README.md">English</a> · <a href="README.ko.md">한국어</a> · <a href="README.de.md">Deutsch</a> · <a href="README.es.md">Español</a> · <b>Français</b> · <a href="README.id.md">Bahasa Indonesia</a> · <a href="README.it.md">Italiano</a> · <a href="README.ja.md">日本語</a> · <a href="README.pl.md">Polski</a> · <a href="README.pt-BR.md">Português (Brasil)</a> · <a href="README.vi.md">Tiếng Việt</a> · <a href="README.zh-Hans.md">简体中文</a> · <a href="README.zh-Hant.md">繁體中文</a></p>

Une app Windows qui écrit les réglages des touches sur de petits claviers macro USB – ceux que l’on trouve dans le
commerce, d’un seul bouton rotatif jusqu’à 21 touches, avec au plus quatre boutons rotatifs. Pour chaque action d’une
touche ou d’un bouton rotatif (rotation à gauche, appui, rotation à droite), elle définit sur les couches 1 à 3 des
frappes, une touche multimédia ou une action de la souris, ainsi que l’éclairage du clavier. C’est la version Windows
d’une app macOS construite selon les mêmes règles, et elle utilise les mêmes commandes.

<p align="center"><img src="../.github/devices.png" width="560" alt="Deux petits claviers de macros USB : six touches et un bouton rotatif, douze touches et deux boutons rotatifs"></p>

| Clavier | Disposition | Lecture | Délai | Éclairage | Testé |
|---|---|---|---|---|---|
| `1189:8890` | définie par vous – touches (jusqu’à 12), touches par rangée, boutons rotatifs (jusqu’à 2) | non | aucun | trois modes | oui – 6 touches et un bouton rotatif |
| fabricant `1189` ou `514C`, produit `8840` · `8842` · `8830`–`8833` · `8850` · `8851`, programme interne récent | selon ce qu’indique le clavier – 26 dispositions | dès la connexion | avant chaque frappe | un mode, et une couleur pour chacune des touches 1–16 | oui – `514C:8850`, 12 touches et 2 boutons rotatifs |
| les mêmes, ancien programme interne | selon ce qu’indique le clavier | dès la connexion | un par touche | une couleur et un mode | non |
| les mêmes, sans indiquer leur type | choisie par vous – 26 dispositions | non | aucun | une couleur et un mode | non |

Le `1189:8890` est le clavier que prend en charge l’outil Windows du fabricant, « MINI KeyBoard » ; les autres sont
ceux que recherche l’app multiplateforme du fabricant, « MINI_KEYBOARD ». L’app utilise les commandes des deux outils.
La plupart des claviers du second type indiquent combien de touches et de boutons rotatifs ils possèdent et quel
programme interne ils exécutent : l’app dessine alors la disposition que dessinerait l’app du fabricant. Un clavier
qui ne l’indique pas reçoit celle de ces dispositions que vous choisissez. L’app du fabricant ne connaît pas le
`1189:8890` et son propre outil ne dessine aucune forme, si bien que sa forme réelle est inconnue : vous indiquez à
l’app combien de touches et de boutons rotatifs il possède et combien de touches compte une rangée, et elle en dessine
une disposition bien ordonnée.

L’app est disponible en 13 langues – anglais, coréen, japonais, chinois (simplifié et traditionnel), allemand,
espagnol, français, italien, portugais (Brésil), polonais, indonésien et vietnamien – et suit la langue de Windows (ou
celle que vous choisissez dans les Réglages). Dans les autres langues, elle s’affiche en anglais.

Le code source de ce dépôt est sous licence MIT. Certaines parties de l’app distribuée ne font pas partie de ce code
source : son nom, son icône et ses liens. L’icône en haut de la page n’est présentée qu’à titre d’illustration : elle
n’est pas couverte par la licence MIT mais par la licence CC BY-SA 4.0
([`LICENSES/CC-BY-SA-4.0.txt`](../LICENSES/CC-BY-SA-4.0.txt)), et n’est pas incluse dans les compilations issues de ce
dépôt.

## macOS, Windows et Linux

MINIKeyboard existe en trois éditions construites sur les mêmes règles – les mêmes claviers, les mêmes commandes, les mêmes 13 langues.

| Dépôt | GitHub |
|---|---|
| **MINIKeyboardApple**<br><img alt="macOS 14+" src="https://img.shields.io/badge/macOS-14%2B-0A84FF?logo=apple&logoColor=white"> <img alt="Universal: Apple Silicon + Intel" src="https://img.shields.io/badge/Universal-Apple%20Silicon%20%2B%20Intel-5E5CE6"> <img alt="Swift 6" src="https://img.shields.io/badge/Swift-6-F05138?logo=swift&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardApple](https://github.com/hobbyworker/MINIKeyboardApple) |
| **MINIKeyboardWin**<br><img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white"> <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6"> <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardWin](https://github.com/hobbyworker/MINIKeyboardWin) |
| **MINIKeyboardLinux**<br><img alt="Linux" src="https://img.shields.io/badge/Linux-GTK%204%20·%20libadwaita-4A86CF?logo=linux&logoColor=white"> <img alt="Flatpak: x86_64 · aarch64" src="https://img.shields.io/badge/Flatpak-x86__64%20·%20aarch64-4A90D9?logo=flatpak&logoColor=white"> <img alt="Rust" src="https://img.shields.io/badge/Rust-2024-B7410E?logo=rust&logoColor=white"> <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"> | [github.com/hobbyworker/MINIKeyboardLinux](https://github.com/hobbyworker/MINIKeyboardLinux) |

## Téléchargement

Téléchargez depuis la page [Releases](https://github.com/hobbyworker/MINIKeyboardWin/releases) le programme
d’installation adapté à votre PC et lancez-le – `MINIKeyboard-<version>-x64-Setup.exe` pour Intel et AMD,
`-arm64-Setup.exe` pour ARM. Il s’installe dans votre dossier utilisateur sans droits d’administrateur, avec des
raccourcis sur le bureau et dans le menu Démarrer. Le programme d’installation n’est pas signé : la première fois,
Windows affiche donc « Windows a protégé votre ordinateur » ; choisissez **Informations complémentaires**, puis
**Exécuter quand même**.

L’app se met à jour elle-même. Elle recherche une nouvelle version au lancement et une fois par jour
(**Réglages ▸ App ▸ Rechercher les mises à jour automatiquement**), ainsi que lorsque vous choisissez
⋯ ▸ **Rechercher les mises à jour…**. S’il y en a une, choisissez **Mettre à jour et relancer** dans la barre en haut
de la fenêtre. Vos réglages sont conservés. Les fichiers `.nupkg` et `releases.*.json` de la page Releases sont ce que
l’app télécharge pour se mettre à jour ; vous n’en avez pas besoin. Pour supprimer l’app, passez par
Paramètres ▸ Applications ▸ Applications installées dans Windows.

## Configuration requise

- Windows 10 version 2004 ou ultérieure, ou Windows 11, sur x64 ou ARM64
- Ni pilote ni autorisation : les deux types de claviers sont ouverts comme périphériques HID.
  - Le `1189:8890` est écrit via son interface de configuration (interface USB 1), comme le fait l’outil du
    fabricant. L’app ne lit jamais ce que vous tapez.
  - Les autres claviers sont écrits via leur première interface USB, comme le fait l’app du fabricant. Pendant ce
    temps, le clavier continue de saisir normalement.

## Compilation

Il faut le SDK .NET 10 (10.0.402 ou plus récent) – Visual Studio n’est pas nécessaire. L’app ne se compile que sous
Windows ; les trois bibliothèques et les tests se compilent partout.

```powershell
copy Brand.props.example Brand.props                                 # une seule fois – puis renseignez BrandName
dotnet build src/MINIKeyboard/MINIKeyboard.csproj -p:Platform=x64    # sur un PC ARM, -p:Platform=ARM64
dotnet test --project tests/MINIKeyboard.Domain.Tests                # de même Data.Tests et Presentation.Tests – aucun clavier n’est touché
```

L’app compilée se trouve dans `src/MINIKeyboard/bin/<x64 ou ARM64>/Debug/net10.0-windows10.0.26100.0/MINIKeyboard.exe`.

Le nom, l’icône et les liens de l’app ne figurent pas dans le dépôt – indiquez les vôtres dans `Brand.props`. Sans nom
(`BrandName`), seule la compilation de l’app s’arrête et en réclame un ; les bibliothèques et les tests se compilent
tels quels. L’icône, la ligne de copyright et les liens sont facultatifs – sans icône, l’app reçoit l’icône générique
de Windows.

Une compilation est de l’un des trois types suivants (`-p:Distribution=…`). `Source`, le type par défaut, n’a pas de
mises à jour propres. `Direct` s’installe et se met à jour avec [Velopack](https://velopack.io) : `BrandUpdateUrl`
indique où se trouvent les nouvelles versions – un dépôt GitHub (ses Releases), ou toute autre adresse ou tout autre
dossier qui contient le flux de Velopack. `Store` est réservé à une compilation pour le Microsoft Store, qui n’est pas
encore prête.

⋯ ▸ **Simulateur** permet d’essayer l’app sans le clavier concerné : choisissez un `1189:8890`, une disposition du
programme interne récent ou un clavier qui n’indique pas son type, et l’app se comporte comme s’il était branché – avec
les mêmes commandes, sans que rien ne parte vers l’USB. Tant qu’il est utilisé, une barre orange en haut de la fenêtre
le signale, et **Revenir au clavier en USB** permet de revenir. Les compilations Debug proposent tous les types (les
autres ID de rapport du `1189:8890`, l’ancien programme interne), et `MINIKEYBOARD_SIMULATED_KEYBOARD` (par exemple
`8890-3`) fait démarrer l’app directement avec l’un d’eux. ⋯ ▸ **Journal de l’appareil** (Ctrl+Alt+L) affiche chaque
rapport écrit et chaque réponse.

## Utilisation

1. Branchez le clavier en USB. L’app le trouve et, s’il est du second type, lui demande ce qu’il est ; s’il
   répond, il est lu aussitôt.
2. Pour un clavier qui n’indique pas sa forme, vous choisissez la première fois sa disposition – l’une des
   dispositions de l’app du fabricant ou, pour le `1189:8890`, le nombre de touches, de touches par rangée et de
   boutons rotatifs – et son orientation devant vous. L’app retient les deux pour cet ID USB. **Forme…** et
   **Faire pivoter** (Ctrl+T) permettent de les modifier par la suite ; un clavier qui a indiqué sa forme peut
   seulement pivoter.
3. Choisissez une couche, puis la touche ou l’action du bouton rotatif sur le clavier.
4. Attribuez-lui une action :
   - **Touches** – maintenez des touches de modification (Ctrl, Alt, Shift, Win, et celles de droite), puis cliquez
     sur des touches ; chaque clic ajoute une frappe. **Modificateurs seuls** fait de la touche une touche de
     modification. **QWERTY** dispose les touches comme sur un clavier complet, **ABC** par groupes ; seules les
     touches que le clavier accepte peuvent être cliquées.
   - **Multimédia**, **Souris** – remplacent ce que faisait la touche.
   - **LED** – l’éclairage de la couche ; avec un programme interne récent, sélectionnez l’une des touches 1–16 pour
     lui donner une couleur.
   - **Délai** – avec un programme interne qui conserve les attentes.
5. **Enregistrer** (Ctrl+S) écrit ce qui a changé. **Lire** (Ctrl+R) récupère les réglages actuels d’un clavier qui
   permet de les relire.

Dans ⋯ ▸ **Réglages** (Ctrl+,), vous choisissez la langue de l’interface (l’une des 13, ou celle de Windows) et
l’apparence claire ou sombre. L’app issue de la page Releases y propose aussi
**App ▸ Rechercher les mises à jour automatiquement**. **À propos**, en bas, affiche la version, les liens et **Logiciels inclus** – les licences des logiciels fournis avec
l’app.

Dans **Réglages ▸ Contournements des bogues du firmware**, **Empêcher la répétition des touches après un changement
d’éclairage** est activé par défaut. Après l’enregistrement de l’éclairage, le firmware du `1189:8890` répète sans fin
le premier caractère d’une touche (le logiciel du fabricant fait de même) ; l’app envoie donc une commande
d’enregistrement supplémentaire, sans touche, après l’éclairage. Désactivé, l’app envoie uniquement ce qu’envoie le
logiciel du fabricant. Ce clavier oublie aussi son éclairage quand on le débranche.

## Structure

Les règles se trouvent dans trois bibliothèques dont les dépendances pointent uniquement vers l’intérieur – une couche
qui cherche à atteindre l’extérieur ne compile pas.

```
src/
├── MINIKeyboard.Domain/        claviers, actions, les contrôles de chaque disposition et ce que chaque clavier peut contenir ; les ports ; cas d’utilisation (bibliothèque de classes de base uniquement)
├── MINIKeyboard.Data/          les commandes de chaque clavier et les numéros de ses contrôles ; trouver le clavier et écrire via HID (SetupAPI, hid.dll) ; claviers simulés
├── MINIKeyboard.Presentation/  modèles de vue et valeurs que dessinent les écrans – la géométrie des dispositions, les tables de touches et de couleurs (sans framework d’interface)
├── MINIKeyboard.Views/         WinUI 3 – ne voit que Presentation ; une table de traduction par langue (Translations/)
└── MINIKeyboard/               la racine de composition – le seul projet qui les voit tous. Direct/ n’entre que dans le téléchargement direct (Velopack)
tests/                          xUnit – les octets de chaque commande, les dispositions, les modifications, l’éditeur
Brand.props.example             le nom, l’icône, les liens et les valeurs de chaque compilation – à copier en Brand.props et à remplir
LICENSES/                       la licence de l’icône en haut de la page (CC BY-SA 4.0)
```

Zéro avertissement : c’est la règle. Les avertissements sont corrigés, pas désactivés.

## Limites

- L’écriture n’a été essayée jusqu’ici que sur un seul clavier, un `1189:8890` à six touches et un bouton rotatif,
  sous Windows 11, sur des PC ARM64 comme x64. Elle n’a pas encore été essayée sous Windows 10. Les autres claviers sont
  écrits comme l’app de leur fabricant les écrit – les commandes sont celles de l’app macOS, octet pour octet, et des
  tests les figent –, mais aucun n’a encore été essayé sur un vrai clavier.
- Le `1189:8890` et les claviers qui n’indiquent pas leur type ne peuvent pas communiquer leurs réglages ; seuls ceux
  définis dans l’app sont donc affichés.
- Un seul clavier à la fois. Si plusieurs sont branchés, l’app prend le premier qu’elle trouve – un `1189:8890` en
  priorité.
- Le déplacement de la souris et le glissement ne sont pas proposés.

## Licence

MIT – voir [LICENSE](../LICENSE). L’icône en haut de la page : CC BY-SA 4.0 – voir
[LICENSES/CC-BY-SA-4.0.txt](../LICENSES/CC-BY-SA-4.0.txt).

L’app contient aussi des composants Microsoft – le Windows App SDK et le Windows SDK for .NET – qui ne sont pas sous
licence MIT mais sous les propres conditions de Microsoft. Ces conditions se trouvent dans l’app, sous Réglages ▸
À propos ▸ **Logiciels inclus**, et utiliser l’app revient à les accepter pour ces composants.
