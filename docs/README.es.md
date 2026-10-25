<p align="center"><img src="../.github/icon.png" width="128" height="128" alt="Icono de la app"></p>

<h1 align="center">Configurador de teclados de macros para Windows</h1>

<p align="center">
  <img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white">
  <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6">
  <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white">
  <a href="../LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"></a>
</p>

<p align="center"><a href="../README.md">English</a> · <a href="README.ko.md">한국어</a> · <a href="README.de.md">Deutsch</a> · <b>Español</b> · <a href="README.fr.md">Français</a> · <a href="README.id.md">Bahasa Indonesia</a> · <a href="README.it.md">Italiano</a> · <a href="README.ja.md">日本語</a> · <a href="README.pl.md">Polski</a> · <a href="README.pt-BR.md">Português (Brasil)</a> · <a href="README.vi.md">Tiếng Việt</a> · <a href="README.zh-Hans.md">简体中文</a> · <a href="README.zh-Hant.md">繁體中文</a></p>

Una app para Windows que escribe la configuración de las teclas en pequeños teclados de macros USB. Se venden en
muchos tamaños: desde los que solo tienen un botón giratorio hasta los de 21 teclas, con hasta cuatro botones
giratorios. Para cada tecla y cada acción de un botón giratorio (girar a la izquierda, pulsar, girar a la derecha),
define en las capas 1–3 pulsaciones de teclas, una tecla multimedia o una acción del ratón, y también la
iluminación del teclado. Es la versión para Windows de una app para macOS creada con las mismas reglas, y usa los
mismos comandos.

| Teclado | Disposición | Lectura | Demora | Iluminación | Probado |
|---|---|---|---|---|---|
| `1189:8890` | la defines tú — teclas (hasta 12), teclas por fila, botones giratorios (hasta 2) | no | no | tres modos | sí — 6 teclas y un botón giratorio |
| fabricante `1189` o `514C`, producto `8840` · `8842` · `8830`–`8833` · `8850` (y `514C:8851`), firmware nuevo | según lo que indica el teclado — 26 disposiciones | al conectarlo | antes de cada pulsación | un modo y un color para cada una de las teclas 1–16 | no |
| los mismos, firmware antiguo | según lo que indica el teclado | cuando lo pides | una por tecla | un color y un modo | no |
| los mismos, sin indicar su tipo | la eliges tú — 26 disposiciones | no | no | un color y un modo | no |

El `1189:8890` es el teclado con el que trabaja la herramienta de configuración para Windows del fabricante,
“MINI KeyBoard”; los demás son los que busca la app multiplataforma del fabricante, “MINI_KEYBOARD”. La app usa los
comandos de ambas herramientas. La mayoría de los teclados del segundo tipo indican cuántas teclas y botones
giratorios tienen y qué firmware usan, así que la app dibuja la misma disposición que dibujaría la app del
fabricante; si un teclado no lo indica, usa la que elijas entre esas disposiciones. La app del fabricante no conoce
el `1189:8890` y su propia herramienta no dibuja ninguna forma, así que no se sabe cuál es su forma real. Por eso,
tú indicas cuántas teclas y botones giratorios tiene y cuántas teclas hay en cada fila, y la app los dibuja tal
cual, bien ordenados.

La app está disponible en 13 idiomas: inglés, coreano, japonés, chino (simplificado y tradicional), alemán, español,
francés, italiano, portugués (Brasil), polaco, indonesio y vietnamita. Sigue el idioma de Windows (o el que elijas en
Ajustes); en cualquier otro idioma, aparece en inglés.

El código fuente de este repositorio tiene licencia MIT. Algunas partes de la app que se distribuye —su nombre, su
icono y sus enlaces— no están incluidas en este código. El icono de arriba se muestra solo como referencia: no está
cubierto por la licencia MIT, sino por CC BY-SA 4.0 ([`LICENSES/CC-BY-SA-4.0.txt`](../LICENSES/CC-BY-SA-4.0.txt)), y
las compilaciones de este repositorio no lo incluyen.

## Descarga

Descarga el instalador para tu PC desde la página [Releases](https://github.com/hobbyworker/MINIKeyboardWin/releases) y
ejecútalo: `MINIKeyboard-<versión>-x64-Setup.exe` para Intel y AMD, y `-arm64-Setup.exe` para ARM. Se instala en tu
carpeta de usuario sin permisos de administrador y crea accesos directos en el escritorio y en el menú Inicio. El
instalador no está firmado, así que la primera vez Windows muestra “Windows protegió su PC”: elige **Más información**
y después **Ejecutar de todas formas**.

La app se actualiza sola. Busca una versión nueva al iniciarse y una vez al día (**Ajustes ▸ App ▸ Buscar
actualizaciones automáticamente**), y también cuando eliges ⋯ ▸ **Buscar actualizaciones…**. Si hay una, elige
**Actualizar y reiniciar** en la barra de la parte superior de la ventana. Tu configuración se conserva. Los archivos
`.nupkg` y `releases.*.json` de la página Releases son los que descarga la app para actualizarse; no los necesitas.
Para desinstalar la app, ve a Configuración ▸ Aplicaciones ▸ Aplicaciones instaladas de Windows.

## Requisitos

- Windows 10 versión 2004 o posterior, o Windows 11, en x64 o ARM64
- No hace falta ningún controlador ni ningún permiso: los dos tipos se abren como dispositivos HID.
  - El `1189:8890` se escribe a través de su interfaz de configuración (interfaz USB 1), como hace la herramienta del
    fabricante. La app nunca lee lo que tecleas.
  - Los demás teclados se escriben a través de su primera interfaz USB, como hace la app del fabricante. Mientras
    tanto, puedes seguir escribiendo con el teclado como siempre.

## Compilación

Necesita el SDK de .NET 10 (10.0.402 o posterior); no hace falta Visual Studio. La app solo se compila en Windows;
las tres bibliotecas y las pruebas se compilan en cualquier sistema.

```powershell
copy Brand.props.example Brand.props                                 # una vez — después, rellena BrandName
dotnet build src/MINIKeyboard/MINIKeyboard.csproj -p:Platform=x64    # en un PC ARM, -p:Platform=ARM64
dotnet test --project tests/MINIKeyboard.Domain.Tests                # y Data.Tests, Presentation.Tests — no se toca ningún teclado
```

La app queda en `src/MINIKeyboard/bin/<x64 o ARM64>/Debug/net10.0-windows10.0.26100.0/MINIKeyboard.exe`.

El nombre, el icono y los enlaces de la app no están en el repositorio: pon los tuyos en `Brand.props`. Sin un nombre
(`BrandName`), solo la app se detiene y pide uno; las bibliotecas y las pruebas se compilan tal cual. El icono, la
línea de copyright y los enlaces son opcionales: sin icono, la app usa el icono genérico de Windows.

Cada compilación es de uno de tres tipos (`-p:Distribution=…`). `Source`, el predeterminado, no tiene actualizaciones
propias. `Direct` se instala y se actualiza con [Velopack](https://velopack.io): `BrandUpdateUrl` indica dónde están
las versiones nuevas —un repositorio de GitHub (sus Releases) o cualquier otra dirección o carpeta que contenga el feed
de Velopack—. `Store` es el lugar reservado para una compilación para Microsoft Store, que todavía no está lista.

⋯ ▸ **Simulador** permite probar la app sin el teclado en cuestión: elige un `1189:8890`, una disposición del
firmware nuevo o un teclado que no indique su tipo, y la app actúa como si estuviera conectado, con los mismos
comandos y sin enviar nada por USB. Mientras se usa, una barra naranja en la parte superior de la ventana lo indica, y
**Volver al teclado por USB** vuelve atrás. Las compilaciones Debug ofrecen todos los tipos (los otros ID de informe del
`1189:8890` y el firmware antiguo), y con `MINIKEYBOARD_SIMULATED_KEYBOARD` (por ejemplo, `8890-3`), la app arranca con
ese teclado. ⋯ ▸ **Registro del dispositivo** (Ctrl+Alt+L) muestra todos los informes escritos y todas las respuestas.

## Uso

1. Conecta el teclado por USB. La app lo encuentra y, si es del segundo tipo, le pregunta qué teclado es; si tiene
   firmware nuevo, lo lee enseguida.
2. Si un teclado no indica su forma, la primera vez eliges su disposición —una de las de la app del fabricante o,
   para el `1189:8890`, el número de teclas, de teclas por fila y de botones giratorios— y cómo está colocado delante
   de ti. La app recuerda ambas cosas para ese ID USB. Más adelante puedes cambiarlas con **Forma…** y **Girar**
   (Ctrl+T). Un teclado que indica su forma solo se puede girar.
3. Elige una capa y, en el dibujo del teclado, una tecla o una acción de un botón giratorio.
4. Asígnale una acción:
   - **Teclas** — mantén pulsadas las teclas de modificación (Ctrl, Alt, Shift, Win y las de la derecha) y haz
     clic en las teclas; cada clic añade una pulsación. **Solo modificadores** convierte la tecla en una tecla de
     modificación. **QWERTY** coloca las teclas como en un teclado completo y **ABC**, por grupos. Solo puedes hacer
     clic en las teclas que admite el teclado.
   - **Multimedia**, **Ratón** — sustituyen lo que hacía la tecla.
   - **LED** — la iluminación de la capa. En el firmware nuevo, elige una de las teclas 1–16 para darle un color.
   - **Demora** — en el firmware que guarda las demoras.
5. **Guardar** (Ctrl+S) escribe lo que ha cambiado. **Leer** (Ctrl+R) trae la configuración actual de los teclados
   que permiten leerla.

En ⋯ ▸ **Ajustes** (Ctrl+,) eliges el idioma de la interfaz (uno de los 13 o el de Windows) y el modo claro u
oscuro. La app de la página Releases también tiene ahí **App ▸ Buscar actualizaciones automáticamente**. En
**Acerca de**, al final, ves la versión, los enlaces y **Software incluido**: las licencias del software que trae la app.

En **Ajustes ▸ Soluciones para fallos del firmware**, **Evitar la repetición de teclas tras cambiar la iluminación**
está activado de forma predeterminada. El firmware del `1189:8890` repite sin parar el primer carácter de una tecla
después de guardar la iluminación (el software del fabricante hace lo mismo), así que la app envía una orden de
guardado más, sin ninguna tecla, después de la iluminación. Si lo desactivas, la app envía solo lo mismo que el
software del fabricante. Además, este teclado olvida su iluminación al desconectarlo.

## Estructura

Las reglas están en tres bibliotecas cuyas dependencias apuntan solo hacia dentro: una capa que intente alcanzar algo
de fuera no compila.

```
src/
├── MINIKeyboard.Domain/        teclados, acciones, los controles de cada disposición y lo que admite cada teclado; puertos; casos de uso (solo la biblioteca de clases base)
├── MINIKeyboard.Data/          los comandos de cada teclado y la numeración de sus controles; búsqueda del teclado y escritura por HID (SetupAPI, hid.dll); teclados simulados
├── MINIKeyboard.Presentation/  modelos de vista y los valores que dibujan las pantallas — la geometría de las disposiciones, tablas de teclas y colores (sin framework de UI)
├── MINIKeyboard.Views/         WinUI 3 — solo ve Presentation; una tabla de traducción por idioma (Translations/)
└── MINIKeyboard/               la raíz de composición (composition root), el único proyecto que los ve todos. Direct/ solo entra en la descarga directa (Velopack)
tests/                          xUnit — los bytes de cada comando, las disposiciones, las ediciones, el editor
Brand.props.example             el nombre, el icono, los enlaces y los valores de cada tipo de compilación — cópialo como Brand.props y rellénalo
LICENSES/                       la licencia del icono de arriba (CC BY-SA 4.0)
```

El objetivo es cero advertencias. Las advertencias se corrigen, no se desactivan.

## Limitaciones

- Hasta ahora, la escritura solo se ha probado en un teclado: un `1189:8890` con seis teclas y un botón giratorio,
  en Windows 11 (ARM64). Todavía no se ha probado en un PC x64 ni en Windows 10. Los demás teclados se escriben tal
  como lo hace la app de su fabricante —los comandos son los de la app para macOS, byte a byte, y las pruebas los
  fijan—, pero todavía no se ha comprobado ninguno con un teclado real.
- El `1189:8890` y los teclados que no indican su tipo no pueden informar de su configuración, así que la app solo
  muestra lo que se ha definido en ella.
- Un teclado cada vez. Si hay varios conectados, la app usa el primero que encuentra, empezando por un `1189:8890`.
- No hay acciones para mover el ratón ni para arrastrar.

## Licencia

MIT — consulta [LICENSE](../LICENSE). El icono de arriba: CC BY-SA 4.0 — consulta
[LICENSES/CC-BY-SA-4.0.txt](../LICENSES/CC-BY-SA-4.0.txt).
