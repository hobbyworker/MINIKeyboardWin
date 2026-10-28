using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using MINIKeyboard.Presentation;
using Windows.Graphics;
using static MINIKeyboard.Views.Strings;

namespace MINIKeyboard.Views;

/// <summary>
/// The window's top, as Windows 11 apps lay it out: the title bar has the keyboard's state — with several plugged in, a
/// menu to choose which one is open — beside the app's icon and the menu at its end (Settings names the app there instead); the command row below it has the layers in the middle and
/// reading and saving on the right.
/// </summary>
/// <remarks>
/// The title bar is our own rather than WinUI's <see cref="TitleBar"/>, which centres what it holds and, above 100 %
/// scale, leaves twice the caption buttons' width free before them.
/// </remarks>
internal sealed class Header
{
    private readonly EditorModel editor;
    private readonly Ellipse dot = new() { Width = 10, Height = 10, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock title = Ui.Text("", "BodyStrongTextBlockStyle");
    private readonly TextBlock detail = Ui.Text("", "CaptionDimTextStyle");
    private readonly Button identify = new() { Content = T("Retry"), VerticalAlignment = VerticalAlignment.Center };
    private readonly Button retry = new() { Content = T("Retry"), VerticalAlignment = VerticalAlignment.Center };
    private readonly ProgressRing spinner = new() { Width = 16, Height = 16, IsActive = false };
    private readonly Segmented layers = new();
    private readonly StackPanel status = new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 10,
        Margin = new Thickness(14, 0, 8, 0),
        VerticalAlignment = VerticalAlignment.Center,
    };
    private readonly TextBlock name;
    private readonly ColumnDefinition leftInset = new() { Width = new GridLength(0) };
    private readonly ColumnDefinition rightInset = new() { Width = new GridLength(0) };
    private readonly FrameworkElement[] dims;
    private readonly FrameworkElement[] clickable;
    private int layerCount = -1;

    public Header(EditorModel editor, AppInfo info)
    {
        this.editor = editor;
        title.TextTrimming = TextTrimming.CharacterEllipsis;
        detail.TextTrimming = TextTrimming.CharacterEllipsis;
        var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        texts.Children.Add(title);
        texts.Children.Add(detail);

        Keyboards = new Button
        {
            Content = Ui.Icon(Glyphs.ChevronDown, 12),
            Style = Styles.Get("SubtleButtonStyle"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Ui.Tooltip(Keyboards, T("Choose a Keyboard"));
        var choices = new MenuFlyout { Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft };
        choices.Opening += (_, _) =>
        {
            choices.Items.Clear();
            foreach (var choice in editor.KeyboardChoices)
            {
                var item = new RadioMenuFlyoutItem
                {
                    Text = choice.Title, GroupName = "keyboards", IsChecked = choice.IsSelected,
                };
                item.Click += (_, _) => editor.SelectKeyboard(choice.Id);
                choices.Items.Add(item);
            }
        };
        Keyboards.Flyout = choices;

        Info = new Button
        {
            Content = Ui.Icon(Glyphs.Info, 16),
            Style = Styles.Get("SubtleButtonStyle"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Ui.Tooltip(Info, T("Keyboard Information"));
        var popup = new Flyout
        {
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft,
            // Wider than a flyout's default; on WinUI's own style, which an explicit one would otherwise replace.
            FlyoutPresenterStyle = new Style(typeof(FlyoutPresenter))
            {
                BasedOn = Styles.Get("DefaultFlyoutPresenterStyle"),
                Setters = { new Setter(FrameworkElement.MaxWidthProperty, 640.0) },
            },
        };
        popup.Opening += (_, _) =>
            popup.Content = editor.KeyboardInfo is { } info ? KeyboardInfoView.Create(info) : null;
        Info.Flyout = popup;

        identify.Click += async (_, _) => await editor.IdentifyAsync();
        retry.Click += (_, _) => editor.RetryConnection();

        status.Children.Add(dot);
        status.Children.Add(texts);
        status.Children.Add(Keyboards);
        status.Children.Add(Info);
        status.Children.Add(identify);
        status.Children.Add(retry);
        status.Children.Add(spinner);

        layers.Selected += layer => editor.SetLayer(layer);

        Read = new Button { Content = T("Read"), VerticalAlignment = VerticalAlignment.Center };
        Ui.Tooltip(Read, T("Read from Keyboard"));
        Save = new Button
        {
            Content = T("Save"),
            Style = Styles.Get("AccentButtonStyle"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Ui.Tooltip(Save, T("Save to Keyboard"));
        Menu = new MenuFlyout { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight };
        More = new Button
        {
            Content = Ui.Icon(Glyphs.More, 16),
            Style = Styles.Get("SubtleButtonStyle"),
            VerticalAlignment = VerticalAlignment.Center,
            Flyout = Menu,
        };
        Ui.Tooltip(More, T("Main Menu"));

        // The title bar: [icon] ● state ⓘ … ⋯ [caption buttons], or in Settings [←] [icon] name. The back button is in
        // WinUI's own style, glyph and all. The icon is 18 rather than the usual 16: the drawing is wider than tall, and at 18 it matches the width
        // of the Photos app's.
        Back = new Button { Style = Styles.Get("TitleBarBackButtonStyle"), Visibility = Visibility.Collapsed };
        Ui.Tooltip(Back, T("Back"));
        var icon = new Image
        {
            Width = 18,
            Height = 18,
            Margin = new Thickness(16, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (info.IconPath is { } path)
            icon.Source = new BitmapImage(new Uri(path));
        name = Ui.Text(info.Name, "CaptionTextBlockStyle");
        name.Margin = new Thickness(14, 0, 0, 0);
        name.VerticalAlignment = VerticalAlignment.Center;
        name.Visibility = Visibility.Collapsed;
        More.Margin = new Thickness(8, 0, 6, 0);

        Bar = new Grid { Height = 48 };
        Bar.ColumnDefinitions.Add(leftInset);
        for (var column = 0; column < 4; column++)
            Bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Bar.ColumnDefinitions.Add(rightInset);
        FrameworkElement[] parts = [Back, icon, name, status];
        for (var column = 0; column < parts.Length; column++)
        {
            Grid.SetColumn(parts[column], column + 1);
            Bar.Children.Add(parts[column]);
        }
        Grid.SetColumn(More, 6);
        Bar.Children.Add(More);
        dims = [icon, status, More];
        clickable = [Back, Keyboards, Info, identify, retry, More];

        // The command row: the layers in the middle, reading and saving on the right.
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        actions.Children.Add(Read);
        actions.Children.Add(Save);
        var commands = new Grid { Margin = new Thickness(16, 2, 16, 6), MinHeight = 40 };
        commands.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        commands.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        commands.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layers.Control.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(layers.Control, 1);
        Grid.SetColumn(actions, 2);
        commands.Children.Add(layers.Control);
        commands.Children.Add(actions);
        Commands = commands;
    }

    /// <summary>The title bar; the window drags by it, but for its buttons (<see cref="Attach"/>).</summary>
    public Grid Bar { get; }

    /// <summary>While Settings is open, in place of the menu.</summary>
    public Button Back { get; }

    /// <summary>The row under the title bar: the layers, and reading and saving.</summary>
    public FrameworkElement Commands { get; }

    /// <summary>With several keyboards plugged in, chooses which one is open.</summary>
    public Button Keyboards { get; }

    /// <summary>Opens what is known about the keyboard.</summary>
    public Button Info { get; }

    public Button Read { get; }

    public Button Save { get; }

    public Button More { get; }

    public MenuFlyout Menu { get; }

    /// <summary>
    /// The keyboard's state and the menu, or — while Settings is open — a back button and the app's name instead. The
    /// command row goes with the page it belongs to.
    /// </summary>
    public void ShowEditing(bool editing)
    {
        Show(status, editing);
        Show(More, editing);
        Show(Back, !editing);
        Show(name, !editing);
    }

    /// <summary>
    /// Makes <see cref="Bar"/> <paramref name="window"/>'s title bar: its buttons take clicks and the rest drags the
    /// window, its ends stay clear of the caption buttons, and it fades while the window is in the background.
    /// </summary>
    public void Attach(Window window)
    {
        window.SetTitleBar(Bar);
        var source = InputNonClientPointerSource.GetForWindowId(window.AppWindow.Id);
        RectInt32[] passthrough = [];
        // After every layout: a change in the state's text moves the buttons after it, a resize moves the menu.
        Bar.LayoutUpdated += (_, _) =>
        {
            if (Bar.XamlRoot is not { } root)
                return;
            var scale = root.RasterizationScale;
            var titleBar = window.AppWindow.TitleBar;
            Fit(leftInset, titleBar.LeftInset / scale);
            Fit(rightInset, titleBar.RightInset / scale);
            RectInt32[] rects = [.. clickable
                .Where(element => Shown(element) && element.ActualWidth > 0)
                .Select(element => Physical(element, scale))];
            if (!rects.SequenceEqual(passthrough))
            {
                source.SetRegionRects(NonClientRegionKind.Passthrough, rects);
                passthrough = rects;
            }
        };
        window.Activated += (_, args) =>
        {
            var active = args.WindowActivationState != WindowActivationState.Deactivated;
            name.Style = Styles.Get(active ? "CaptionTextBlockStyle" : "CaptionFaintTextStyle");
            foreach (var element in dims)
                element.Opacity = active ? 1 : 0.5;
        };
    }

    /// <summary>Whether <paramref name="element"/> and everything it sits in on the title bar is visible.</summary>
    private bool Shown(FrameworkElement element)
    {
        for (DependencyObject? at = element; at is not null && at != Bar; at = VisualTreeHelper.GetParent(at))
        {
            if (at is UIElement { Visibility: Visibility.Collapsed })
                return false;
        }
        return true;
    }

    private static void Fit(ColumnDefinition column, double width)
    {
        if (column.Width.Value != width)
            column.Width = new GridLength(width);
    }

    private static RectInt32 Physical(FrameworkElement element, double scale)
    {
        var bounds = element.TransformToVisual(null)
            .TransformBounds(new Windows.Foundation.Rect(0, 0, element.ActualWidth, element.ActualHeight));
        return new RectInt32(
            (int)Math.Round(bounds.X * scale),
            (int)Math.Round(bounds.Y * scale),
            (int)Math.Round(bounds.Width * scale),
            (int)Math.Round(bounds.Height * scale));
    }

    public void Refresh()
    {
        var status = editor.Status;
        var activity = editor.Activity;
        dot.Style = Styles.Get(status switch
        {
            ConnectionStatus.Connected { IsSimulated: true } => "DotSimulatedStyle",
            ConnectionStatus.Connected { IsIdentified: true } => "DotConnectedStyle",
            ConnectionStatus.Connected or ConnectionStatus.NeedsPermission => "DotWaitingStyle",
            _ => "DotOffStyle",
        });
        (title.Text, detail.Text) = status switch
        {
            ConnectionStatus.NeedsPermission => (T("Access to the keyboard is needed"), ""),
            ConnectionStatus.OpenFailed failed =>
                (T("The keyboard couldn’t be opened"), Names.ErrorCode(failed.Code)),
            ConnectionStatus.Connected connected => (
                activity switch
                {
                    Activity.Identifying => T("Asking the keyboard what it is…"),
                    Activity.Reading => T("Reading…"),
                    Activity.Saving => T("Saving…"),
                    _ when connected.IsSimulated => T("Simulated Keyboard"),
                    _ => T("Connected"),
                },
                connected is { Keys: { } keys, Knobs: { } knobs }
                    ? T("%@ · %@", connected.Usb, T("Keys: %lld · Knobs: %lld", keys, knobs))
                    : connected.Usb),
            _ => (T("Not Connected"), T("Connect the keyboard via USB.")),
        };
        var connectedNow = status is ConnectionStatus.Connected;
        Show(Keyboards, editor.KeyboardChoices.Count > 0);
        Keyboards.IsEnabled = editor.CanSwitchKeyboard;
        Show(Info, connectedNow && editor.KeyboardInfo is not null);
        Show(identify, status is ConnectionStatus.Connected { IsIdentified: false } && activity is null);
        Show(retry, status is ConnectionStatus.NeedsPermission or ConnectionStatus.OpenFailed);
        Show(spinner, activity is not null);
        spinner.IsActive = activity is not null;

        var count = editor.LayerCount;
        if (count != layerCount)
        {
            layers.SetItems([.. Enumerable.Range(1, count).Select(layer => T("Layer %lld", layer))]);
            layerCount = count;
        }
        Show(layers.Control, count > 1);
        layers.SelectedIndex = editor.Layer;
        layers.Control.IsEnabled = editor.IsReady;
        Show(Read, editor.ReadsBack);
    }

    private static void Show(UIElement element, bool visible) =>
        element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
}

/// <summary>
/// What is known about the keyboard plugged in. Nothing is guessed: what the keyboard doesn't say shows as not
/// provided.
/// </summary>
internal static class KeyboardInfoView
{
    public static UIElement Create(KeyboardInfo info)
    {
        var root = new StackPanel { Spacing = 12, MinWidth = 380, MaxWidth = 560 };
        root.Children.Add(Ui.Text(T("Keyboard Information"), "BodyStrongTextBlockStyle"));
        var grid = new Grid { ColumnSpacing = 14, RowSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        void Add(string title, FrameworkElement value)
        {
            var row = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var name = Ui.Text(title, "DimTextStyle");
            name.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetRow(name, row);
            Grid.SetRow(value, row);
            Grid.SetColumn(value, 1);
            grid.Children.Add(name);
            grid.Children.Add(value);
        }
        static TextBlock Value(string text, string style = "BodyTextBlockStyle")
        {
            var value = Ui.Text(text, style, wrap: true);
            value.IsTextSelectionEnabled = true;
            return value;
        }
        static FrameworkElement Provided(string? value) =>
            value is null ? Ui.Text(T("Not provided by the keyboard"), "DimTextStyle") : Value(value);

        Add("USB ID", Value(info.UsbId, "MonoTextStyle"));
        Add(T("Name"), Provided(info.Name));
        Add(T("Manufacturer"), Provided(info.Manufacturer));
        Add(T("Serial Number"), Provided(info.SerialNumber));
        // No USB descriptor names the chip.
        Add(T("Chip"), Provided(null));
        switch (info.Firmware)
        {
            case Firmware.Reported reported:
                var column = new StackPanel { Spacing = 2 };
                column.Children.Add(Value(reported.Kind.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                column.Children.Add(Ui.Caption(T("Keys: %lld · Knobs: %lld", reported.Keys, reported.Knobs)));
                if (!reported.Tested)
                    column.Children.Add(Ui.Caption(T("Not tested yet")));
                Add(T("Firmware"), column);
                break;
            case Firmware.Release release:
                Add(T("Firmware"), Value(release.Version));
                break;
            default:
                Add(T("Firmware"), Provided(null));
                break;
        }
        if (info.Usb is { } usb)
            Add("USB", Value(usb));
        if (info.Power is { } power)
            Add(T("Power"), Value($"{power} mA"));
        if (!info.Interfaces.IsEmpty)
        {
            var column = new StackPanel { Spacing = 3 };
            foreach (var face in info.Interfaces)
            {
                var roles = string.Join(", ", face.Roles.Select(Names.InterfaceRoleTitle));
                column.Children.Add(Value($"{face.Number}  {roles}{(face.IsConfiguration ? "  ←" : "")}"));
            }
            Add(T("Interfaces"), column);
        }
        Add(T("Configuration Channel"), Value(Names.ChannelText(info.Channel)));
        Add(T("Commands"), Value(Names.CommandsText(info.Commands)));
        root.Children.Add(grid);
        return root;
    }
}
