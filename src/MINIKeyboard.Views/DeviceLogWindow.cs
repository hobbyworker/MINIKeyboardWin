using System.Globalization;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MINIKeyboard.Presentation;
using Windows.ApplicationModel.DataTransfer;
using static MINIKeyboard.Views.Strings;

namespace MINIKeyboard.Views;

/// <summary>
/// The reports written to the keyboard and its answers, for checking what a keyboard does. A report shows on one line
/// up to the zero padding at its end; opened, it shows every byte. Closing it only hides it.
/// </summary>
public sealed partial class DeviceLogWindow : Window
{
    private readonly DeviceLogModel model;
    private readonly Grid root = new();
    private readonly TextBlock count = Ui.Text("", "DimTextStyle");
    private readonly Button expandAll = new() { Content = T("Expand All") };
    private readonly Button copy = new() { Content = T("Copy") };
    private readonly Button clear = new() { Content = T("Clear") };
    private readonly StackPanel list = new() { Padding = new Thickness(8, 6, 8, 10) };
    private readonly ScrollViewer scroller = new()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        HorizontalScrollMode = ScrollMode.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
    };
    private readonly StackPanel empty = new()
    {
        Spacing = 10,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };
    private readonly List<Row> rows = [];
    private readonly HashSet<ulong> expanded = [];
    private bool closable;

    internal DeviceLogWindow(DeviceLogModel model, ElementTheme theme)
    {
        this.model = model;
        Title = T("Device Log");
        SystemBackdrop = new MicaBackdrop();
        var titleBar = new TitleBar
        {
            Title = T("Device Log"),
            IsBackButtonVisible = false,
            IsPaneToggleButtonVisible = false,
        };
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(titleBar);

        var bar = new Grid { ColumnSpacing = 8, Padding = new Thickness(14, 6, 14, 10) };
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var column = 0; column < 3; column++)
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        count.VerticalAlignment = VerticalAlignment.Center;
        bar.Children.Add(count);
        foreach (var (button, column) in new[] { (expandAll, 1), (copy, 2), (clear, 3) })
        {
            Grid.SetColumn(button, column);
            bar.Children.Add(button);
        }

        empty.Children.Add(Ui.Icon(Glyphs.Log, 32, "FaintIconStyle"));
        empty.Children.Add(Ui.Text(T("No packets yet."), "DimTextStyle"));
        scroller.Content = list;
        var body = new Grid();
        body.Children.Add(scroller);
        body.Children.Add(empty);
        var layer = new Border
        {
            Style = Styles.Get("LayerStyle"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child = body,
        };

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(bar, 1);
        Grid.SetRow(layer, 2);
        root.Children.Add(titleBar);
        root.Children.Add(bar);
        root.Children.Add(layer);
        root.RequestedTheme = theme;
        Content = root;

        expandAll.Click += (_, _) => ToggleAll();
        copy.Click += (_, _) =>
        {
            var package = new DataPackage();
            package.SetText(CopyText(model.Lines));
            Clipboard.SetContent(package);
        };
        clear.Click += (_, _) =>
        {
            expanded.Clear();
            model.Clear();
        };
        model.Changed += (_, _) => Refresh();
        AppWindow.Closing += (window, args) =>
        {
            if (closable)
                return;
            args.Cancel = true;
            window.Hide();
        };
        WindowSizing.Apply(this, 760, 460, 480, 260);
        Refresh();
    }

    /// <summary>Brings it up, in front.</summary>
    internal void Present()
    {
        AppWindow.Show();
        Activate();
    }

    internal void SetTheme(ElementTheme theme)
    {
        root.RequestedTheme = theme;
        WindowSizing.SetCaptionTheme(this, theme);
    }

    /// <summary>Closes it for good, as the app quits.</summary>
    internal void CloseForGood()
    {
        closable = true;
        Close();
    }

    /// <summary>
    /// Adds the lines that came and drops the ones the log let go, rather than drawing every line again.
    /// </summary>
    private void Refresh()
    {
        var lines = model.Lines;
        var first = lines.Count > 0 ? lines[0].Id : (ulong?)null;
        while (rows.Count > 0 && (first is null || rows[0].Id < first))
        {
            list.Children.Remove(rows[0].Element);
            rows.RemoveAt(0);
        }
        var last = rows.Count > 0 ? rows[^1].Id : (ulong?)null;
        var added = false;
        foreach (var line in lines.Where(line => last is null || line.Id > last))
        {
            var row = MakeRow(line);
            list.Children.Add(row.Element);
            rows.Add(row);
            added = true;
        }
        count.Text = T("Packets: %lld", lines.Count);
        empty.Visibility = lines.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        scroller.Visibility = lines.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        copy.IsEnabled = lines.Count > 0;
        clear.IsEnabled = lines.Count > 0;
        UpdateExpandAll();
        if (added)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                scroller.UpdateLayout();
                scroller.ChangeView(null, scroller.ScrollableHeight, null, disableAnimation: true);
            });
        }
    }

    private Row MakeRow(LogLine line)
    {
        var canExpand = line.Event is null && (line.IsShortened || line.Bytes.Count > 16);
        var chevron = new Button
        {
            Content = Ui.Icon(Glyphs.ChevronRight, 10),
            Style = Styles.Get("SubtleButtonStyle"),
            Padding = new Thickness(6),
            Opacity = canExpand ? 1 : 0,
            IsEnabled = canExpand,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        header.Children.Add(chevron);
        var time = Ui.Text(TimeText(line), "LogDimTextStyle");
        time.VerticalAlignment = VerticalAlignment.Center;
        header.Children.Add(time);
        var (glyph, iconStyle) = line.Direction switch
        {
            LogDirection.Output => (Glyphs.Up, "OutputIconStyle"),
            LogDirection.Input => (Glyphs.Down, "InputIconStyle"),
            _ => (Glyphs.Info, "DimIconStyle"),
        };
        var icon = Ui.Icon(glyph, 12, iconStyle);
        icon.VerticalAlignment = VerticalAlignment.Center;
        header.Children.Add(icon);
        var text = line.Event is { } logEvent ? Names.LogEventMessage(logEvent) : line.Summary();
        var body = Ui.Text(text, "LogTextStyle");
        body.IsTextSelectionEnabled = true;
        body.VerticalAlignment = VerticalAlignment.Center;
        header.Children.Add(body);
        if (line.IsShortened)
        {
            var more = Ui.Text($"… {T("%lld bytes", line.Bytes.Count)}", "LogDimTextStyle");
            more.VerticalAlignment = VerticalAlignment.Center;
            header.Children.Add(more);
        }
        var detail = Ui.Text(string.Join("\n", line.Rows()), "LogDimTextStyle");
        detail.IsTextSelectionEnabled = true;
        detail.Margin = new Thickness(40, 0, 0, 6);
        detail.Visibility = Visibility.Collapsed;
        var column = new StackPanel { Spacing = 2 };
        column.Children.Add(header);
        column.Children.Add(detail);
        var row = new Row(line.Id, column, detail, chevron, canExpand);
        chevron.Click += (_, _) =>
        {
            SetExpanded(row.Id, !expanded.Contains(row.Id));
            UpdateExpandAll();
        };
        ShowExpanded(row, expanded.Contains(line.Id));
        return row;
    }

    private void SetExpanded(ulong id, bool open)
    {
        if (open)
            expanded.Add(id);
        else
            expanded.Remove(id);
        if (rows.FirstOrDefault(row => row.Id == id) is { } row)
            ShowExpanded(row, open);
    }

    private bool AllExpanded()
    {
        var expandable = rows.Where(row => row.CanExpand).ToList();
        return expandable.Count > 0 && expandable.All(row => expanded.Contains(row.Id));
    }

    private void ToggleAll()
    {
        var open = !AllExpanded();
        foreach (var row in rows.Where(row => row.CanExpand))
            SetExpanded(row.Id, open);
        UpdateExpandAll();
    }

    private void UpdateExpandAll()
    {
        expandAll.Content = AllExpanded() ? T("Collapse All") : T("Expand All");
        expandAll.IsEnabled = rows.Any(row => row.CanExpand);
    }

    private static void ShowExpanded(Row row, bool open)
    {
        open = open && row.CanExpand;
        row.Detail.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        row.Chevron.Content = Ui.Icon(open ? Glyphs.ChevronDown : Glyphs.ChevronRight, 10);
        Ui.Tooltip(row.Chevron, open ? T("Collapse") : T("Expand"));
    }

    /// <summary>"14:03:27.412", in local time.</summary>
    private static string TimeText(LogLine line) =>
        line.Time.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

    /// <summary>Every line, every byte of each report.</summary>
    private static string CopyText(IReadOnlyList<LogLine> lines) => string.Join("\r\n", lines.Select(line =>
    {
        var mark = line.Direction switch
        {
            LogDirection.Output => "OUT",
            LogDirection.Input => "IN ",
            _ => "-- ",
        };
        var body = line.Event is { } logEvent ? Names.LogEventMessage(logEvent) : line.Hex();
        return $"{TimeText(line)} {mark} {body}";
    }));

    private sealed record Row(ulong Id, StackPanel Element, TextBlock Detail, Button Chevron, bool CanExpand);
}

/// <summary>
/// A window's size in view pixels, made physical for its monitor; and the colours of its caption buttons.
/// </summary>
internal static partial class WindowSizing
{
    /// <summary>Sizes <paramref name="window"/> and centres it on its monitor's work area.</summary>
    /// <param name="tallTitleBar">Caption buttons as tall as a title bar that holds controls.</param>
    public static void Apply(
        Window window, int width, int height, int minimumWidth, int minimumHeight, bool tallTitleBar = false)
    {
        var scale = Scale(window);
        var appWindow = window.AppWindow;
        var size = new Windows.Graphics.SizeInt32((int)(width * scale), (int)(height * scale));
        if (DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest) is { } display)
        {
            var area = display.WorkArea;
            size.Width = Math.Min(size.Width, area.Width);
            size.Height = Math.Min(size.Height, area.Height);
            appWindow.MoveAndResize(new Windows.Graphics.RectInt32(
                area.X + (area.Width - size.Width) / 2,
                area.Y + (area.Height - size.Height) / 2,
                size.Width,
                size.Height));
        }
        else
        {
            appWindow.Resize(size);
        }
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = (int)(minimumWidth * scale);
            presenter.PreferredMinimumHeight = (int)(minimumHeight * scale);
        }
        appWindow.TitleBar.PreferredHeightOption =
            tallTitleBar ? TitleBarHeightOption.Tall : TitleBarHeightOption.Standard;
    }

    public static void SetCaptionTheme(Window window, ElementTheme theme) =>
        window.AppWindow.TitleBar.PreferredTheme = theme switch
        {
            ElementTheme.Light => TitleBarTheme.Light,
            ElementTheme.Dark => TitleBarTheme.Dark,
            _ => TitleBarTheme.UseDefaultAppMode,
        };

    private static double Scale(Window window)
    {
        var dpi = GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(window));
        return dpi == 0 ? 1 : dpi / 96.0;
    }

    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint window);
}
