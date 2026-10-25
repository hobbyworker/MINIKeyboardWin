using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MINIKeyboard.Presentation;
using static MINIKeyboard.Views.Strings;

namespace MINIKeyboard.Views;

/// <summary>The tabs, and the page of actions for the selected key under them.</summary>
internal sealed class ActionPanel
{
    private readonly EditorModel editor;
    private readonly SelectorBar tabs = new();
    private readonly ScrollViewer scroller = new()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        Padding = new Thickness(16, 4, 16, 16),
    };
    private readonly KeysPage keys;
    private readonly ChoicesPage<MediaChoice> media;
    private readonly ChoicesPage<MouseChoice> mouse;
    private readonly LightingView lighting;
    private readonly DelayView delay;
    private IReadOnlyList<EditorTab> shownTabs = [];
    private bool updating;

    public ActionPanel(EditorModel editor, InterfaceSettings settings)
    {
        this.editor = editor;
        keys = new KeysPage(editor, settings);
        media = new ChoicesPage<MediaChoice>(
            editor,
            T("Multimedia"),
            T("A media key replaces what the key did."),
            136,
            choice => (Names.MediaGlyph(choice), Names.MediaTitle(choice)),
            (model, choice) => model.AssignMedia(choice),
            (caption, choice) => caption == new ActionCaption.Media(choice));
        mouse = new ChoicesPage<MouseChoice>(
            editor,
            T("Mouse"),
            T("A mouse action replaces what the key did."),
            168,
            choice => (Names.MouseGlyph(choice), Names.MouseTitle(choice)),
            (model, choice) => model.AssignMouse(choice),
            (caption, choice) => caption == new ActionCaption.Mouse(choice));
        lighting = new LightingView(editor);
        delay = new DelayView(editor);

        tabs.Margin = new Thickness(12, 4, 12, 0);
        tabs.SelectionChanged += (sender, _) =>
        {
            if (!updating && sender.SelectedItem is { Tag: EditorTab tab })
                editor.SetTab(tab);
        };
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(scroller, 1);
        root.Children.Add(tabs);
        root.Children.Add(scroller);
        Root = Ui.Enabler(root);
    }

    public ContentControl Root { get; }

    public void Refresh()
    {
        var available = editor.AvailableTabs;
        var tab = editor.Tab;
        updating = true;
        if (!shownTabs.SequenceEqual(available))
        {
            tabs.Items.Clear();
            foreach (var each in available)
                tabs.Items.Add(new SelectorBarItem { Text = Names.TabTitle(each), Tag = each });
            shownTabs = available;
        }
        var item = tabs.Items.FirstOrDefault(item => item.Tag is EditorTab shown && shown == tab);
        if (tabs.SelectedItem != item)
            tabs.SelectedItem = item;
        updating = false;
        Root.IsEnabled = editor.IsReady;
        UIElement page = tab switch
        {
            EditorTab.Keys => keys.Root,
            EditorTab.Media => media.Root,
            EditorTab.Mouse => mouse.Root,
            EditorTab.Lighting => lighting.Root,
            _ => delay.Root,
        };
        if (!ReferenceEquals(scroller.Content, page))
            scroller.Content = page;
        switch (tab)
        {
            case EditorTab.Keys:
                keys.Refresh();
                break;
            case EditorTab.Media:
                media.Refresh(editor.MediaChoices);
                break;
            case EditorTab.Mouse:
                mouse.Refresh(MouseChoices.All);
                break;
            case EditorTab.Lighting:
                lighting.Refresh();
                break;
            default:
                delay.Refresh();
                break;
        }
    }
}

/// <summary>
/// Modifiers to hold, then a key: each key pressed is added to the selected key's presses. The keys are laid out as on
/// a full-size keyboard or in groups, as the user picks — until then, as the keyboard's vendor tool does — and only
/// the keys the keyboard is given can be pressed.
/// </summary>
internal sealed class KeysPage
{
    private readonly EditorModel editor;
    private readonly InterfaceSettings settings;
    private readonly Segmented arrangement = new();
    private readonly Button modifiersOnly;
    private readonly TextBlock note = Ui.Caption("");
    private readonly Border shown = new();
    private readonly StackPanel qwerty = new() { Spacing = 16 };
    private readonly BoardPanel board = new() { RowWidth = BoardCap.RowWidth };
    private readonly StackPanel more = new();
    private readonly StackPanel grouped = new() { Spacing = 16 };
    private readonly List<(ModifierKey Modifier, Button Button)> modifiers = [];
    private IReadOnlyList<BoardCap>? shownBoard;
    private IReadOnlyList<KeyUsage>? shownMore;
    private (IReadOnlyList<KeyGroup> Groups, IReadOnlyList<KeyUsage> Symbols)? shownGroups;

    public KeysPage(EditorModel editor, InterfaceSettings settings)
    {
        this.editor = editor;
        this.settings = settings;
        arrangement.SetItems(["QWERTY", "ABC"]);
        Ui.Tooltip(arrangement.Root, T("Key Layout"));
        arrangement.Selected += index =>
        {
            settings.Update(choices => choices with { KeyArrangement = index == 0 ? "qwerty" : "abc" });
            // The choice is the interface's, not the model's; showing the page again draws it.
            editor.SetTab(EditorTab.Keys);
        };
        modifiersOnly = Ui.Keycap(T("Modifiers Only"));
        modifiersOnly.HorizontalAlignment = HorizontalAlignment.Right;
        Ui.Tooltip(modifiersOnly, T("Makes the key hold these modifiers, as a modifier key does."));
        modifiersOnly.Click += (_, _) => editor.AssignHeldModifiers();

        var row = new Grid { ColumnSpacing = 12 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(modifiersOnly, 1);
        row.Children.Add(arrangement.Control);
        row.Children.Add(modifiersOnly);
        var top = new StackPanel { Spacing = 8 };
        top.Children.Add(row);
        top.Children.Add(note);

        qwerty.Children.Add(board);
        qwerty.Children.Add(more);
        Root = new StackPanel { Spacing = 16 };
        Root.Children.Add(top);
        Root.Children.Add(shown);
    }

    public StackPanel Root { get; }

    private KeyArrangement Arrangement => settings.Choices.KeyArrangement switch
    {
        "qwerty" => KeyArrangement.Qwerty,
        "abc" => KeyArrangement.Alphabetical,
        _ => editor.VendorKeyArrangement,
    };

    public void Refresh()
    {
        var current = Arrangement;
        arrangement.SelectedIndex = current == KeyArrangement.Qwerty ? 0 : 1;
        var limit = editor.PressLimit;
        note.Text = limit == 0
            ? T("Modifiers apply to the next key.")
            : editor.ModifiersCountAsPresses
                ? T("Modifiers apply to the next key. A key holds up to %lld presses, each modifier counting as one.",
                    limit)
                : T("Modifiers apply to the next key. A key holds up to %lld presses.", limit);
        var held = editor.HeldModifiers;
        modifiersOnly.IsEnabled = held.Count > 0;

        if (current == KeyArrangement.Qwerty)
        {
            var caps = editor.KeyBoard;
            var extra = editor.MoreKeys;
            if (shownBoard is null || !shownBoard.SequenceEqual(caps) || !shownMore!.SequenceEqual(extra))
            {
                BuildBoard(caps, extra);
                (shownBoard, shownMore, shownGroups) = (caps, extra, null);
            }
            if (shown.Child != qwerty)
                shown.Child = qwerty;
        }
        else
        {
            var groups = editor.KeyGroups;
            var symbols = editor.ShiftSymbols;
            if (shownGroups is not { } previous || !previous.Groups.SequenceEqual(groups)
                || !previous.Symbols.SequenceEqual(symbols))
            {
                BuildGroups(groups, symbols);
                (shownGroups, shownBoard, shownMore) = ((groups, symbols), null, null);
            }
            if (shown.Child != grouped)
                shown.Child = grouped;
        }
        foreach (var (modifier, button) in modifiers)
            Ui.SetActive(button, held.Contains(modifier));
    }

    private Button ModifierButton(ModifierKey modifier, bool compact)
    {
        var button = Ui.Keycap(modifier.Label());
        if (compact)
            button.Style = Styles.Get("CompactKeycapButtonStyle");
        Ui.Tooltip(button, Names.ModifierTitle(modifier));
        button.Click += (_, _) => editor.Toggle(modifier);
        return button;
    }

    private Button PressButton(KeyUsage key, bool compact)
    {
        var button = Ui.KeyButton(key, compact);
        button.Click += (_, _) => editor.Press(key);
        return button;
    }

    /// <summary>
    /// The keys as on a full-size keyboard, its modifier keys held for the next key. A key the keyboard isn't given
    /// stays in its place, off.
    /// </summary>
    private void BuildBoard(IReadOnlyList<BoardCap> caps, IReadOnlyList<KeyUsage> extra)
    {
        board.Clear();
        modifiers.Clear();
        foreach (var cap in caps)
        {
            UIElement element;
            switch (cap.Face)
            {
                case BoardFace.Key { Usage: var key }:
                    var button = PressButton(key, compact: true);
                    if (cap.IsEnabled)
                    {
                        element = button;
                    }
                    else
                    {
                        // A cap that's off shows no tooltip of its own; what holds it says why.
                        button.IsEnabled = false;
                        var holder = new Grid
                        {
                            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                        };
                        holder.Children.Add(button);
                        ToolTipService.SetToolTip(holder, T("This keyboard doesn’t take this key."));
                        element = holder;
                    }
                    break;
                case BoardFace.Modifier { ModifierKey: var modifier }:
                    var held = ModifierButton(modifier, compact: true);
                    modifiers.Add((modifier, held));
                    element = held;
                    break;
                default:
                    continue;
            }
            board.Add(element, cap.Row, cap.Column, cap.Width);
        }
        more.Children.Clear();
        if (extra.Count > 0)
        {
            var wrap = new WrapPanel();
            foreach (var key in extra)
                wrap.Children.Add(PressButton(key, compact: false));
            more.Children.Add(Ui.Section(T("More Keys"), null, wrap));
        }
    }

    /// <summary>The keys in groups, the letters in alphabetical order, under a row of modifiers to hold.</summary>
    private void BuildGroups(IReadOnlyList<KeyGroup> groups, IReadOnlyList<KeyUsage> shiftSymbols)
    {
        grouped.Children.Clear();
        modifiers.Clear();
        var row = new WrapPanel { HorizontalSpacing = 6, VerticalSpacing = 6 };
        foreach (var modifier in ModifierKeys.All)
        {
            var button = ModifierButton(modifier, compact: false);
            button.MinWidth = 62;
            row.Children.Add(button);
            modifiers.Add((modifier, button));
        }
        grouped.Children.Add(Ui.Section(T("Modifiers"), null, row));
        foreach (var group in groups)
        {
            var wrap = new WrapPanel();
            foreach (var key in group.Keys)
                wrap.Children.Add(PressButton(key, compact: false));
            grouped.Children.Add(Ui.Section(Names.KeyGroupTitle(group.Kind), null, wrap));
        }
        var symbols = new WrapPanel();
        foreach (var key in shiftSymbols)
        {
            if (key.Shifted is not { } shifted)
                continue;
            var button = Ui.Keycap(shifted);
            ToolTipService.SetToolTip(button, $"Shift+{key.Label}");
            button.Click += (_, _) => editor.PressShifted(key);
            symbols.Children.Add(button);
        }
        grouped.Children.Add(Ui.Section(T("Shift Symbols"), null, symbols));
    }
}

/// <summary>Media keys or mouse actions: a cap for each, the one the key does marked.</summary>
internal sealed class ChoicesPage<TChoice>
    where TChoice : struct, Enum
{
    private readonly EditorModel editor;
    private readonly WrapPanel grid = new() { HorizontalSpacing = 8, VerticalSpacing = 8, UniformHeight = true };
    private readonly double width;
    private readonly Func<TChoice, (string Glyph, string Title)> face;
    private readonly Action<EditorModel, TChoice> assign;
    private readonly Func<ActionCaption, TChoice, bool> isActive;
    private IReadOnlyList<TChoice>? shownChoices;
    private ActionCaption? shownCaption;

    public ChoicesPage(
        EditorModel editor,
        string title,
        string note,
        double width,
        Func<TChoice, (string Glyph, string Title)> face,
        Action<EditorModel, TChoice> assign,
        Func<ActionCaption, TChoice, bool> isActive)
    {
        this.editor = editor;
        this.width = width;
        this.face = face;
        this.assign = assign;
        this.isActive = isActive;
        Root = Ui.Section(title, note, grid);
    }

    public StackPanel Root { get; }

    public void Refresh(IReadOnlyList<TChoice> choices)
    {
        var caption = editor.SelectedCaption;
        if (shownChoices is not null && shownChoices.SequenceEqual(choices) && Equals(shownCaption, caption))
            return;
        grid.Children.Clear();
        foreach (var choice in choices)
        {
            var (glyph, title) = face(choice);
            var button = Ui.TallKeycap(glyph, title);
            button.Width = width;
            Ui.SetActive(button, caption is not null && isActive(caption, choice));
            button.Click += (_, _) => assign(editor, choice);
            grid.Children.Add(button);
        }
        (shownChoices, shownCaption) = (choices, caption);
    }
}
