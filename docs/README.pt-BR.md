<p align="center"><img src="../.github/icon.png" width="128" height="128" alt="Ícone do app"></p>

<h1 align="center">Configurador de teclados de macro para Windows</h1>

<p align="center">
  <img alt="Windows 10 (2004)+ · 11" src="https://img.shields.io/badge/Windows-10%20(2004)%2B%20·%2011-0078D4?logo=windows&logoColor=white">
  <img alt="x64 · ARM64" src="https://img.shields.io/badge/x64%20·%20ARM64-5E5CE6">
  <img alt=".NET 10 · WinUI 3" src="https://img.shields.io/badge/.NET%2010-WinUI%203-512BD4?logo=dotnet&logoColor=white">
  <a href="../LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-yellow"></a>
</p>

<p align="center"><a href="../README.md">English</a> · <a href="README.ko.md">한국어</a> · <a href="README.de.md">Deutsch</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a> · <a href="README.id.md">Bahasa Indonesia</a> · <a href="README.it.md">Italiano</a> · <a href="README.ja.md">日本語</a> · <a href="README.pl.md">Polski</a> · <b>Português (Brasil)</b> · <a href="README.vi.md">Tiếng Việt</a> · <a href="README.zh-Hans.md">简体中文</a> · <a href="README.zh-Hant.md">繁體中文</a></p>

Um app para Windows que grava as configurações das teclas em pequenos teclados de macro USB. Eles são vendidos em
vários tamanhos: desde os que têm só um botão giratório até os de 21 teclas, com até quatro botões giratórios. Para
cada tecla e cada ação de botão giratório (girar à esquerda, pressionar, girar à direita), o app define nas
camadas 1–3 toques de teclas, uma tecla multimídia ou uma ação do mouse, além da iluminação do teclado. É a versão
para Windows de um app para macOS feito com as mesmas regras, e usa os mesmos comandos.

| Teclado | Disposição | Leitura | Atraso | Iluminação | Testado |
|---|---|---|---|---|---|
| `1189:8890` | definida por você — teclas (até 12), teclas por fileira, botões giratórios (até 2) | não | não | três modos | sim — 6 teclas e um botão giratório |
| fabricante `1189` ou `514C`, produto `8840` · `8842` · `8830`–`8833` · `8850` (e `514C:8851`), firmware novo | conforme o que o teclado informa — 26 disposições | ao conectar | antes de cada toque | um modo e uma cor para cada uma das teclas 1–16 | não |
| os mesmos, firmware antigo | conforme o que o teclado informa | quando você pede | um por tecla | uma cor e um modo | não |
| os mesmos, sem informar o tipo | escolhida por você — 26 disposições | não | não | uma cor e um modo | não |

O `1189:8890` é o teclado com que trabalha a ferramenta de configuração para Windows do fabricante, “MINI KeyBoard”;
os demais são os que o app multiplataforma do fabricante, “MINI_KEYBOARD”, procura. O app usa os comandos das duas
ferramentas. A maioria dos teclados do segundo tipo informa quantas teclas e botões giratórios tem e qual firmware
usa, então o app desenha a mesma disposição que o app do fabricante desenharia; para um teclado que não informa
isso, ele usa a disposição que você escolher entre essas. O app do fabricante não conhece o `1189:8890`, e a
própria ferramenta dele não desenha nenhuma forma, então não se sabe qual é a forma real. Por isso, você informa
quantas teclas e botões giratórios ele tem e quantas teclas há em cada fileira, e o app desenha tudo isso de
forma organizada.

O app está disponível em 13 idiomas — inglês, coreano, japonês, chinês (simplificado e tradicional), alemão,
espanhol, francês, italiano, português (Brasil), polonês, indonésio e vietnamita — e segue o idioma do Windows (ou o
que você escolher em Ajustes). Em qualquer outro idioma, ele aparece em inglês.

O código-fonte deste repositório está sob a licença MIT. Algumas partes do app distribuído — o nome, o ícone e os
links — não fazem parte deste código. O ícone no topo é mostrado apenas como referência: ele não está coberto pela
licença MIT, mas pela CC BY-SA 4.0 ([`LICENSES/CC-BY-SA-4.0.txt`](../LICENSES/CC-BY-SA-4.0.txt)), e as compilações
deste repositório não o incluem.

## Download

Baixe o instalador para o seu PC na página [Releases](https://github.com/hobbyworker/MINIKeyboardWin/releases) e
execute-o: `MINIKeyboard-<versão>-x64-Setup.exe` para Intel e AMD, e `-arm64-Setup.exe` para ARM. Ele é instalado na
sua pasta de usuário sem direitos de administrador, com atalhos na área de trabalho e no menu Iniciar. O instalador
não é assinado, então, na primeira vez, o Windows mostra “O Windows protegeu o computador”: escolha
**Mais informações** e depois **Executar assim mesmo**.

O app se atualiza sozinho. Ele procura uma versão nova ao iniciar e uma vez por dia (**Ajustes ▸ App ▸ Buscar
atualizações automaticamente**), e também quando você escolhe ⋯ ▸ **Buscar Atualizações…**. Quando houver uma,
escolha **Atualizar e Reabrir** na barra no topo da janela. Suas configurações são mantidas. Os arquivos `.nupkg` e
`releases.*.json` da página Releases são o que o app baixa para se atualizar; você não precisa deles. Para remover o
app, vá em Configurações ▸ Aplicativos ▸ Aplicativos instalados, no Windows.

## Requisitos

- Windows 10 versão 2004 ou posterior, ou Windows 11, em x64 ou ARM64
- Nenhum driver e nenhuma permissão: os dois tipos são abertos como dispositivos HID.
  - O `1189:8890` é gravado pela interface de configuração dele (interface USB 1), como faz a ferramenta do
    fabricante. O app nunca lê o que você digita.
  - Os demais teclados são gravados pela primeira interface USB deles, como faz o app do fabricante. Enquanto isso,
    você continua digitando normalmente com o teclado.

## Compilação

É preciso ter o SDK do .NET 10 (10.0.402 ou posterior) — o Visual Studio não é necessário. O app só é compilado no
Windows; as três bibliotecas e os testes são compilados em qualquer sistema.

```powershell
copy Brand.props.example Brand.props                                 # uma vez — depois, preencha BrandName
dotnet build src/MINIKeyboard/MINIKeyboard.csproj -p:Platform=x64    # num PC ARM, -p:Platform=ARM64
dotnet test --project tests/MINIKeyboard.Domain.Tests                # e Data.Tests, Presentation.Tests — nenhum teclado é tocado
```

O app fica em `src/MINIKeyboard/bin/<x64 ou ARM64>/Debug/net10.0-windows10.0.26100.0/MINIKeyboard.exe`.

O nome, o ícone e os links do app não estão no repositório — coloque os seus em `Brand.props`. Sem um nome
(`BrandName`), só o app para e pede um; as bibliotecas e os testes são compilados normalmente. O ícone, a linha de
copyright e os links são opcionais — sem ícone, o app usa o ícone genérico do Windows.

Cada compilação é de um de três tipos (`-p:Distribution=…`). `Source`, o padrão, não tem atualizações próprias.
`Direct` é instalado e atualizado pelo [Velopack](https://velopack.io): `BrandUpdateUrl` diz onde estão as versões
novas — um repositório do GitHub (os Releases dele) ou qualquer outro endereço ou pasta que tenha o feed do Velopack.
`Store` é o lugar de uma compilação para a Microsoft Store, que ainda não está pronta.

⋯ ▸ **Simulador** permite experimentar o app sem o teclado em questão: escolha um `1189:8890`, uma disposição do
firmware novo ou um teclado que não informe o tipo, e o app age como se ele estivesse conectado — com os mesmos
comandos, sem enviar nada por USB. Enquanto ele estiver em uso, uma barra laranja no topo da janela avisa, e
**Voltar ao teclado via USB** volta ao teclado real. As compilações Debug oferecem todos os tipos (os outros IDs do
relatório do `1189:8890`, o firmware antigo), e com `MINIKEYBOARD_SIMULATED_KEYBOARD` (por exemplo, `8890-3`), o app
já inicia com esse teclado. ⋯ ▸ **Registro do Dispositivo** (Ctrl+Alt+L) mostra todos os relatórios gravados e todas
as respostas.

## Como usar

1. Conecte o teclado por USB. O app o encontra e, se ele for do segundo tipo, pergunta qual é o teclado; se tiver
   firmware novo, já o lê na hora.
2. Na primeira vez, para um teclado que não informa a forma, você define a disposição — uma das disposições do app
   do fabricante ou, no `1189:8890`, o número de teclas, de teclas por fileira e de botões giratórios — e a posição
   em que ele fica na sua frente. O app guarda as duas coisas para aquele ID USB. Depois, você pode mudá-las com
   **Forma…** e **Girar** (Ctrl+T). Um teclado que informou a forma só pode ser girado.
3. Escolha uma camada e, no desenho do teclado, uma tecla ou uma ação de botão giratório.
4. Atribua uma ação:
   - **Teclas** — segure as teclas modificadoras (Ctrl, Alt, Shift, Win e as do lado direito) e clique nas teclas;
     cada clique adiciona um toque. **Só modificadoras** transforma a tecla em uma tecla modificadora. **QWERTY**
     dispõe as teclas como num teclado completo, e **ABC**, em grupos. Só é possível clicar nas teclas que o teclado
     aceita.
   - **Multimídia**, **Mouse** — substituem o que a tecla fazia.
   - **LED** — a iluminação da camada. No firmware novo, escolha uma das teclas 1–16 para dar uma cor a ela.
   - **Atraso** — no firmware que guarda os atrasos.
5. **Salvar** (Ctrl+S) grava o que mudou. **Ler** (Ctrl+R) traz a configuração atual dos teclados que permitem a
   leitura.

Em ⋯ ▸ **Ajustes** (Ctrl+,), você escolhe o idioma da interface (um dos 13 ou o do Windows) e o modo claro ou
escuro. O app da página Releases também tem ali **App ▸ Buscar atualizações automaticamente**. Em **Sobre**, no
final, você vê a versão e os links.

Em **Ajustes ▸ Contornos para falhas do firmware**, **Evitar a repetição de teclas após mudar a iluminação** vem
ativado por padrão. O firmware do `1189:8890` repete sem parar o primeiro caractere de uma tecla depois que a
iluminação é salva (o software do fabricante faz o mesmo), então o app envia mais um comando de salvar, sem nenhuma
tecla, depois da iluminação. Desativado, o app envia apenas o mesmo que o software do fabricante. Esse teclado também
esquece a iluminação quando é desconectado.

## Estrutura

As regras ficam em três bibliotecas cujas dependências apontam só para dentro: uma camada que tente alcançar algo de
fora não compila.

```
src/
├── MINIKeyboard.Domain/        teclados, ações, os controles de cada disposição e o que cada teclado comporta; as portas; casos de uso (só a biblioteca de classes base)
├── MINIKeyboard.Data/          os comandos de cada teclado e a numeração dos controles; encontrar o teclado e gravar por HID (SetupAPI, hid.dll); teclados simulados
├── MINIKeyboard.Presentation/  view models e os valores que as telas desenham — a geometria das disposições, tabelas de teclas e cores (sem framework de UI)
├── MINIKeyboard.Views/         WinUI 3 — só vê Presentation; uma tabela de tradução por idioma (Translations/)
└── MINIKeyboard/               a composition root: o único projeto que conhece todos os outros. Direct/ entra só no download direto (Velopack)
tests/                          xUnit — os bytes de cada comando, as disposições, as edições, o editor
Brand.props.example             o nome, o ícone, os links e os valores de cada tipo de compilação — copie para Brand.props e preencha
LICENSES/                       a licença do ícone no topo (CC BY-SA 4.0)
```

Zero avisos é o padrão. Avisos são corrigidos, não desativados.

## Limitações

- Até agora, a gravação só foi testada em um teclado, um `1189:8890` com seis teclas e um botão giratório, no
  Windows 11 (ARM64). Ainda não foi testada em um PC x64 nem no Windows 10. Os demais teclados são gravados como o
  app do fabricante os grava — os comandos são os do app para macOS, byte a byte, e os testes os fixam —, mas nenhum
  deles foi testado em um teclado real ainda.
- O `1189:8890` e os teclados que não informam o tipo não conseguem informar a configuração atual, então o app
  mostra só o que foi definido nele.
- Um teclado por vez. Com vários conectados, o app usa o primeiro que encontrar — um `1189:8890` primeiro.
- Não há ações para mover o mouse nem para arrastar.

## Licença

MIT — veja [LICENSE](../LICENSE). O ícone no topo: CC BY-SA 4.0 — veja
[LICENSES/CC-BY-SA-4.0.txt](../LICENSES/CC-BY-SA-4.0.txt).
