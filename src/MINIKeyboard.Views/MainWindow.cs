using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using MINIKeyboard.Presentation;
using Windows.System;
using static MINIKeyboard.Views.Strings;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace MINIKeyboard.Views;

/// <summary>The main window: the keyboard on top, what to give the selected key below.</summary>
public sealed partial class MainWindow : Window
{
    /// <summary>The window's rows: the title bar, the simulator's bar, the update bar, then the keyboard or Settings.</summary>
    private const int SimulatorRow = 1, UpdateRow = 2, ContentRow = 3;

    private readonly AppInfo info;
    private readonly DeviceLogModel log;
    private readonly Grid root = new();
    private readonly KeyboardCanvas canvas = new();
    private readonly ContentControl canvasHost;
    private readonly StackPanel emptyState = new()
    {
        Spacing = 10,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };
    private readonly StackPanel layoutButtons = new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 6,
        HorizontalAlignment = HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Bottom,
        Margin = new Thickness(10),
    };
    private readonly Grid content = new();
    private SettingsPage? settingsPage;
    private readonly InfoBar notice = new() { IsClosable = true, IsOpen = false };
    private readonly Border noticeHolder;
    private readonly Border simulatorBar;
    private readonly UpdateBar? updateBar;
    private readonly ActionPanel panel;
    private readonly Inspector inspector;
    private readonly DialogHost dialogs;
    private readonly Dictionary<string, Command> commands = [];
    private readonly List<(string Command, MenuFlyoutItem Item)> menuItems = [];
    private readonly List<(string? Id, RadioMenuFlyoutItem Item)> simulatorItems = [];
    private LayoutDialog? layoutDialog;
    private ulong shownNotice;
    private DispatcherQueueTimer? noticeTimer;

    /// <param name="updates">The app's own updates, in the build that has them.</param>
    public MainWindow(
        AppInfo info, InterfaceSettings settings, EditorModel editor, DeviceLogModel log, IAppUpdates? updates = null)
    {
        this.info = info;
        this.log = log;
        Editor = editor;
        Settings = settings;
        Title = info.Name;
        SystemBackdrop = new MicaBackdrop();
        dialogs = new DialogHost(root, () => Theme);

        Header = new Header(editor, info);
        ExtendsContentIntoTitleBar = true;
        Header.Attach(this);

        // The keyboard.
        canvas.Selected += control => editor.Select(control);
        canvasHost = Ui.Enabler(canvas.Root);
        canvasHost.Margin = new Thickness(24, 10, 24, 10);
        emptyState.Children.Add(Ui.Icon(Glyphs.Keyboard, 30, "FaintIconStyle"));
        emptyState.Children.Add(Ui.Text(T("Keys appear here once the keyboard is connected."), "DimTextStyle"));
        var rotate = new Button { Content = Ui.Icon(Glyphs.Rotate, 14) };
        Ui.Tooltip(rotate, T("Rotate"));
        rotate.Click += (_, _) => Run("rotate");
        var shape = new Button { Content = T("Shape…") };
        shape.Click += (_, _) => Run("shape");
        layoutButtons.Children.Add(rotate);
        layoutButtons.Children.Add(shape);
        noticeHolder = new Border
        {
            Style = Styles.Get("NoticeBackgroundStyle"),
            Child = notice,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            MaxWidth = 640,
            Margin = new Thickness(16, 10, 16, 0),
            Visibility = Visibility.Collapsed,
        };
        notice.CloseButtonClick += (_, _) =>
        {
            if (editor.NoticeSerial == shownNotice)
                editor.DismissNotice();
        };
        var keyboardArea = new Grid { MinHeight = 230 };
        keyboardArea.Children.Add(emptyState);
        keyboardArea.Children.Add(canvasHost);
        keyboardArea.Children.Add(layoutButtons);
        keyboardArea.Children.Add(noticeHolder);

        // What to give the selected key.
        panel = new ActionPanel(editor, settings);
        inspector = new Inspector(editor);
        var bottom = new Grid();
        bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        var divider = Ui.Divider(vertical: true);
        Grid.SetColumn(divider, 1);
        Grid.SetColumn(inspector.Root, 2);
        bottom.Children.Add(panel.Root);
        bottom.Children.Add(divider);
        bottom.Children.Add(inspector.Root);
        var layer = new Border
        {
            Style = Styles.Get("LayerStyle"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child = bottom,
        };

        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(
            new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 230 });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(420) });
        Grid.SetRow(keyboardArea, 1);
        Grid.SetRow(layer, 2);
        content.Children.Add(Header.Commands);
        content.Children.Add(keyboardArea);
        content.Children.Add(layer);

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        simulatorBar = SimulatorBar();
        Grid.SetRow(simulatorBar, SimulatorRow);
        Grid.SetRow(content, ContentRow);
        root.Children.Add(Header.Bar);
        root.Children.Add(simulatorBar);
        root.Children.Add(content);
        if (updates is not null)
        {
            updateBar = new UpdateBar(updates, settings, info.Name);
            Grid.SetRow((FrameworkElement)updateBar.Root, UpdateRow);
            root.Children.Add(updateBar.Root);
        }
        root.RequestedTheme = Theme;
        root.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;
        Content = root;

        InstallCommands();
        WindowSizing.Apply(this, 1100, 850, 900, 770, tallTitleBar: true);
        WindowSizing.SetCaptionTheme(this, Theme);
        editor.Changed += (_, _) => Refresh();
        Closed += (_, _) =>
        {
            noticeTimer?.Stop();
            updateBar?.Stop();
            LogWindow?.CloseForGood();
        };
        Refresh();
    }

    internal EditorModel Editor { get; }

    internal InterfaceSettings Settings { get; }

    internal Header Header { get; }

    /// <summary>The device log, once it has been opened.</summary>
    internal DeviceLogWindow? LogWindow { get; private set; }

    private ElementTheme Theme => ThemeOf(Settings.Choices.Appearance);

    private static ElementTheme ThemeOf(Appearance appearance) => appearance switch
    {
        Appearance.Light => ElementTheme.Light,
        Appearance.Dark => ElementTheme.Dark,
        _ => ElementTheme.Default,
    };

    /// <summary>What the window can do, by name — from its buttons, its menu, its shortcuts.</summary>
    private sealed record Command(Action Execute, Func<bool> CanExecute);

    /// <summary>Does what <paramref name="name"/> names when it can be done now; false when it can't.</summary>
    internal bool Run(string name)
    {
        if (!commands.TryGetValue(name, out var command) || !command.CanExecute())
            return false;
        command.Execute();
        return true;
    }

    private void InstallCommands()
    {
        var editor = Editor;
        bool Ready() => editor.IsReady;
        commands["read"] = new(() => _ = editor.ReadAsync(), () => editor.CanRead);
        commands["save"] = new(() => _ = editor.SaveAsync(), () => Ready() && editor.HasUnsavedChanges);
        commands["clear-key"] = new(editor.ClearSelected, () => Ready() && editor.SelectedControl is not null);
        commands["clear-layer"] = new(editor.ClearLayer, Ready);
        commands["shape"] = new(() => editor.SetChoosingLayout(true), () => editor.HasLayout);
        commands["rotate"] = new(editor.Rotate, () => editor.HasLayout);
        commands["device-log"] = new(ShowLog, () => true);
        commands["settings"] = new(() => ShowSettings(toAbout: false), () => true);
        commands["preferences"] = commands["settings"];
        commands["about"] = new(() => ShowSettings(toAbout: true), () => true);
        commands["licenses"] = new(ShowLicenses, () => info.Licenses is { Count: > 0 });
        if (updateBar is { } updates)
            commands["check-updates"] = new(() => _ = updates.CheckAsync(asked: true), () => true);
        commands["back"] = new(HideSettings, () => settingsPage?.Root.Visibility == Visibility.Visible);
        commands["close"] = new(Close, () => true);
        commands["quit"] = new(Application.Current.Exit, () => true);

        Header.Read.Click += (_, _) => Run("read");
        Header.Save.Click += (_, _) => Run("save");
        Header.Back.Click += (_, _) => Run("back");

        var menu = Header.Menu.Items;
        menu.Add(MenuItem(T("Read from Keyboard"), "", "read", "Ctrl+R"));
        menu.Add(MenuItem(T("Save to Keyboard"), "", "save", "Ctrl+S"));
        menu.Add(new MenuFlyoutSeparator());
        menu.Add(MenuItem(T("Clear Key"), "", "clear-key", "Ctrl+Backspace"));
        menu.Add(MenuItem(T("Clear Layer"), "", "clear-layer", "Ctrl+Shift+Backspace"));
        menu.Add(new MenuFlyoutSeparator());
        menu.Add(MenuItem(T("Shape…"), "", "shape"));
        menu.Add(MenuItem(T("Rotate"), Glyphs.Rotate, "rotate", "Ctrl+T"));
        menu.Add(new MenuFlyoutSeparator());
        menu.Add(MenuItem(T("Device Log"), Glyphs.Log, "device-log", "Ctrl+Alt+L"));
        if (editor.CanSelectSource)
        {
            menu.Add(new MenuFlyoutSeparator());
            menu.Add(SimulatorMenu());
        }
        menu.Add(new MenuFlyoutSeparator());
        menu.Add(MenuItem(T("Settings"), "", "settings", "Ctrl+,"));
        if (updateBar is not null)
            menu.Add(MenuItem(T("Check for Updates…"), Glyphs.Sync, "check-updates"));
        menu.Add(MenuItem(T("About %@", info.Name), Glyphs.Info, "about"));

        Accelerator(VirtualKey.R, VirtualKeyModifiers.Control, "read");
        Accelerator(VirtualKey.S, VirtualKeyModifiers.Control, "save");
        Accelerator(VirtualKey.Back, VirtualKeyModifiers.Control, "clear-key", leavesText: true);
        Accelerator(VirtualKey.Back, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, "clear-layer",
            leavesText: true);
        Accelerator(VirtualKey.T, VirtualKeyModifiers.Control, "rotate");
        Accelerator(VirtualKey.L, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu, "device-log");
        Accelerator((VirtualKey)0xBC, VirtualKeyModifiers.Control, "settings");
        Accelerator(VirtualKey.Escape, VirtualKeyModifiers.None, "back");
        Accelerator(VirtualKey.Left, VirtualKeyModifiers.Menu, "back");
        Accelerator(VirtualKey.W, VirtualKeyModifiers.Control, "close");
        Accelerator(VirtualKey.Q, VirtualKeyModifiers.Control, "quit");
    }

    private MenuFlyoutItem MenuItem(string text, string glyph, string command, string? shortcut = null)
    {
        var item = new MenuFlyoutItem { Text = text, Icon = new FontIcon { Glyph = glyph } };
        if (shortcut is not null)
            item.KeyboardAcceleratorTextOverride = shortcut;
        item.Click += (_, _) => Run(command);
        menuItems.Add((command, item));
        return item;
    }

    /// <param name="leavesText">Not while typing, where the keys edit the text.</param>
    private void Accelerator(VirtualKey key, VirtualKeyModifiers modifiers, string command, bool leavesText = false)
    {
        var accelerator = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
        accelerator.Invoked += (_, args) =>
        {
            if (leavesText && FocusManager.GetFocusedElement(root.XamlRoot) is TextBox)
                return;
            args.Handled = Run(command);
        };
        root.KeyboardAccelerators.Add(accelerator);
    }

    /// <summary>
    /// While a simulated keyboard is in use, a bar across the window says so, loudly — nothing saved then reaches a
    /// keyboard — with the way back to the keyboard on USB.
    /// </summary>
    private Border SimulatorBar()
    {
        var white = new SolidColorBrush(Microsoft.UI.Colors.White);
        var icon = Ui.Icon(Glyphs.Warning, 22);
        icon.Foreground = white;
        icon.VerticalAlignment = VerticalAlignment.Center;
        var texts = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        texts.Children.Add(new TextBlock
        {
            Text = T("Simulator in use"),
            Foreground = white,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
        });
        texts.Children.Add(new TextBlock
        {
            Text = T("This isn't a real keyboard. Nothing you save here is written to any keyboard."),
            Foreground = white,
            TextWrapping = TextWrapping.Wrap,
        });
        var back = new Button { Content = T("Back to the Keyboard on USB"), VerticalAlignment = VerticalAlignment.Center };
        back.Click += (_, _) => Editor.SelectSimulator(null);
        var row = new Grid { ColumnSpacing = 14 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(texts, 1);
        Grid.SetColumn(back, 2);
        row.Children.Add(icon);
        row.Children.Add(texts);
        row.Children.Add(back);
        return new Border
        {
            Background = new SolidColorBrush(KeyboardCanvas.SimulatorOrange),
            Padding = new Thickness(20, 10, 16, 10),
            Child = row,
            Visibility = Visibility.Collapsed,
        };
    }

    /// <summary>
    /// The keyboard on USB, or a simulated one: the kinds users have, and in a debug build every kind (the 8890's other
    /// report IDs, the legacy records).
    /// </summary>
    private MenuFlyoutSubItem SimulatorMenu()
    {
#if DEBUG
        var choices = Editor.SimulatorChoices;
#else
        var choices = Editor.SimulatorChoices.Where(choice => choice.IsForEveryone).ToList();
#endif
        RadioMenuFlyoutItem Radio(string text, string? id)
        {
            var item = new RadioMenuFlyoutItem { Text = text, GroupName = "simulator" };
            item.Click += (_, _) => Editor.SelectSimulator(id);
            simulatorItems.Add((id, item));
            return item;
        }
        var menu = new MenuFlyoutSubItem { Text = T("Simulator"), Icon = new FontIcon { Glyph = Glyphs.Keyboard } };
        // A group of one is an item of its own, under the group's name; an empty one isn't there.
        void Group(string title, SimulatorGroup group)
        {
            var members = choices.Where(choice => choice.Group == group).ToList();
            if (members.Count == 1)
            {
                menu.Items.Add(Radio(title, members[0].Id));
                return;
            }
            if (members.Count == 0)
                return;
            var sub = new MenuFlyoutSubItem { Text = title };
            foreach (var choice in members)
                sub.Items.Add(Radio(Names.SimulatorKindTitle(choice.Kind), choice.Id));
            menu.Items.Add(sub);
        }
        menu.Items.Add(Radio(T("Keyboard on USB"), null));
        menu.Items.Add(new MenuFlyoutSeparator());
        Group("1189:8890", SimulatorGroup.K8890);
        Group(Names.CommandsText(new InfoCommands.Standard()), SimulatorGroup.Standard);
        Group(Names.CommandsText(new InfoCommands.Legacy()), SimulatorGroup.Legacy);
        foreach (var choice in choices.Where(choice => choice.Group == SimulatorGroup.Unidentified))
            menu.Items.Add(Radio(Names.SimulatorKindTitle(choice.Kind), choice.Id));
        return menu;
    }

    private void ShowLog()
    {
        LogWindow ??= new DeviceLogWindow(log, Theme);
        LogWindow.Present();
    }

    /// <summary>
    /// Settings in place of the keyboard, with a back button in the title bar (as Notepad does); the title bar's
    /// editing controls step aside meanwhile.
    /// </summary>
    private void ShowSettings(bool toAbout)
    {
        if (settingsPage is null)
        {
            settingsPage = new SettingsPage(
                Settings, info, ApplyAppearance, hasUpdates: updateBar is not null, showLicenses: ShowLicenses);
            Grid.SetRow((FrameworkElement)settingsPage.Root, ContentRow);
            root.Children.Add(settingsPage.Root);
        }
        settingsPage.Root.Visibility = Visibility.Visible;
        content.Visibility = Visibility.Collapsed;
        Header.ShowEditing(false);
        settingsPage.Reveal(toAbout);
        UpdateCommands();
    }

    private void ShowLicenses()
    {
        if (info.Licenses is { Count: > 0 } licenses)
            _ = dialogs.ShowAsync(LicensesDialog.Create(info, licenses));
    }

    private void HideSettings()
    {
        if (settingsPage is not null)
            settingsPage.Root.Visibility = Visibility.Collapsed;
        content.Visibility = Visibility.Visible;
        Header.ShowEditing(true);
        UpdateCommands();
    }

    /// <summary>Light, dark or the system's, at once, in every window.</summary>
    internal void ApplyAppearance(Appearance appearance)
    {
        var theme = ThemeOf(appearance);
        root.RequestedTheme = theme;
        WindowSizing.SetCaptionTheme(this, theme);
        LogWindow?.SetTheme(theme);
        if (dialogs.Current is { } dialog)
            dialog.RequestedTheme = theme;
    }

    /// <summary>Opens what is known about the keyboard, as the header's button does.</summary>
    internal void ShowKeyboardInfo()
    {
        if (Header.Info.Flyout is { } flyout && Header.Info.Visibility == Visibility.Visible)
            flyout.ShowAt(Header.Info);
    }

    private void Refresh()
    {
        var editor = Editor;
        Header.Refresh();
        var drawing = editor.Drawing;
        emptyState.Visibility = drawing is null ? Visibility.Visible : Visibility.Collapsed;
        canvasHost.Visibility = drawing is null ? Visibility.Collapsed : Visibility.Visible;
        canvas.SetDrawing(drawing);
        var simulated = editor.ActiveSimulatorId is not null;
        simulatorBar.Visibility = simulated ? Visibility.Visible : Visibility.Collapsed;
        canvas.SetSimulated(simulated);
        var ready = editor.IsReady;
        canvasHost.IsEnabled = ready;
        canvasHost.Opacity = ready ? 1 : 0.45;
        layoutButtons.Visibility = editor.HasLayout ? Visibility.Visible : Visibility.Collapsed;
        panel.Refresh();
        inspector.Refresh();
        UpdateCommands();
        UpdateNotice();
        UpdateLayoutDialog();
    }

    private void UpdateCommands()
    {
        bool Can(string name) => commands[name].CanExecute();
        Header.Read.IsEnabled = Can("read");
        Header.Save.IsEnabled = Can("save");
        foreach (var (command, item) in menuItems)
            item.IsEnabled = Can(command);
        var active = Editor.ActiveSimulatorId;
        foreach (var (id, item) in simulatorItems)
            item.IsChecked = id == active;
    }

    /// <summary>A message that comes and goes, over the top of the keyboard.</summary>
    private void UpdateNotice()
    {
        var editor = Editor;
        var serial = editor.NoticeSerial;
        if (editor.Notice is { } current)
        {
            if (serial == shownNotice)
                return;
            shownNotice = serial;
            notice.Message = Names.NoticeMessage(current);
            notice.Severity = current switch
            {
                Notice.Saved or Notice.ReadComplete => InfoBarSeverity.Success,
                Notice.NothingToSave => InfoBarSeverity.Informational,
                Notice.WriteFailed or Notice.ReadFailed or Notice.IdentifyFailed or Notice.KeyboardGone =>
                    InfoBarSeverity.Error,
                _ => InfoBarSeverity.Warning,
            };
            notice.IsOpen = true;
            noticeHolder.Visibility = Visibility.Visible;
            noticeTimer?.Stop();
            noticeTimer = DispatcherQueue.CreateTimer();
            noticeTimer.Interval = current.Lifetime;
            noticeTimer.IsRepeating = false;
            noticeTimer.Tick += (_, _) => editor.ExpireNotice(serial);
            noticeTimer.Start();
        }
        else
        {
            noticeTimer?.Stop();
            notice.IsOpen = false;
            noticeHolder.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>The layout dialog is up while the editor says it is.</summary>
    private void UpdateLayoutDialog()
    {
        var editor = Editor;
        var sheet = editor.LayoutSheet;
        if (editor.IsChoosingLayout && sheet is not null && layoutDialog is null)
        {
            var created = new LayoutDialog(editor, sheet);
            layoutDialog = created;
            _ = ShowLayoutDialogAsync(created);
        }
        else if ((!editor.IsChoosingLayout || sheet is null) && layoutDialog is { } open)
        {
            layoutDialog = null;
            open.Close();
        }
    }

    private async Task ShowLayoutDialogAsync(LayoutDialog dialog)
    {
        await dialogs.ShowAsync(dialog.Dialog, () => layoutDialog == dialog);
        if (layoutDialog != dialog)
            return;
        // Closed from the dialog: the choosing ends — even when the keyboard went away meanwhile.
        layoutDialog = null;
        if (Editor.IsChoosingLayout)
            Editor.SetChoosingLayout(false);
    }
}
