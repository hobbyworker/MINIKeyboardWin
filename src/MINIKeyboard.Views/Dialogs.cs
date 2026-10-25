using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MINIKeyboard.Presentation;
using static MINIKeyboard.Views.Strings;

namespace MINIKeyboard.Views;

/// <summary>
/// Shows the window's dialogs one at a time — a window holds only one — each after the one before closed, and none
/// before the window has loaded.
/// </summary>
internal sealed class DialogHost
{
    private readonly FrameworkElement root;
    private readonly Func<ElementTheme> theme;
    private Task showing;

    public DialogHost(FrameworkElement root, Func<ElementTheme> theme)
    {
        this.root = root;
        this.theme = theme;
        showing = Loaded(root);
    }

    public ContentDialog? Current { get; private set; }

    /// <summary>A dialog with the app's look.</summary>
    public static ContentDialog Create() => new() { Style = Styles.Get("DefaultContentDialogStyle") };

    /// <summary>
    /// Shows <paramref name="dialog"/> once the one before has closed, unless by then <paramref name="stillWanted"/>
    /// says no; null when it wasn't shown.
    /// </summary>
    public async Task<ContentDialogResult?> ShowAsync(ContentDialog dialog, Func<bool>? stillWanted = null)
    {
        var previous = showing;
        var done = new TaskCompletionSource();
        showing = done.Task;
        try
        {
            await previous;
            if (stillWanted?.Invoke() == false || root.XamlRoot is not { } xamlRoot)
                return null;
            dialog.XamlRoot = xamlRoot;
            dialog.RequestedTheme = theme();
            Current = dialog;
            return await dialog.ShowAsync();
        }
        finally
        {
            if (Current == dialog)
                Current = null;
            done.SetResult();
        }
    }

    private static Task Loaded(FrameworkElement element)
    {
        if (element.IsLoaded)
            return Task.CompletedTask;
        var loaded = new TaskCompletionSource();
        element.Loaded += (_, _) => loaded.TrySetResult();
        return loaded.Task;
    }
}

/// <summary>
/// Sets the shape of a keyboard that can't say it, and how the keyboard is turned. It comes up on its own the first
/// time such a keyboard is plugged in — then without Cancel, there being nothing to go back to.
/// </summary>
/// <remarks>
/// A 884x keyboard picks one of the cross-platform app's layouts. The 1189:8890, which that app doesn't know and whose
/// own tool draws no shape, is arranged: how many keys, how many to a row, how many knobs. A keyboard that said its
/// shape only turns.
/// </remarks>
internal sealed class LayoutDialog
{
    private readonly EditorModel editor;
    private readonly LayoutSheetState sheet;
    private readonly ShapeThumbnail preview = new(16);
    private string? layoutId;
    private int quarterTurns;
    private byte keys;
    private byte columns = 1;
    private byte knobs;
    private bool forced;
    private bool accepted;

    public LayoutDialog(EditorModel editor, LayoutSheetState sheet)
    {
        this.editor = editor;
        this.sheet = sheet;
        quarterTurns = sheet.QuarterTurns;
        if (sheet.Mode is LayoutSheetMode.Arrangement arrangement)
            (keys, columns, knobs) = (arrangement.Keys, arrangement.Columns, arrangement.Knobs);

        var content = new StackPanel { Spacing = 16, Width = 640 };
        var explanation = new StackPanel { Spacing = 6 };
        explanation.Children.Add(Ui.Text(sheet.Usb, "MonoDimTextStyle"));
        var reported = sheet is { ReportedKeys: { } reportedKeys, ReportedKnobs: { } reportedKnobs }
            ? T("The keyboard reports its keys and knobs: %@",
                T("Keys: %lld · Knobs: %lld", reportedKeys, reportedKnobs))
            : T("The keyboard doesn’t say how many keys and knobs it has.");
        var how = sheet.Mode is LayoutSheetMode.Arrangement
            ? T("Its real shape isn’t known either. Set how many keys and knobs it has and how many keys are in a row, then rotate it to match how it sits in front of you.")
            : T("Pick the shape that matches the keyboard, then rotate it to match how it sits in front of you.");
        explanation.Children.Add(Ui.Text(reported, "DimTextStyle", wrap: true));
        explanation.Children.Add(Ui.Text(how, "DimTextStyle", wrap: true));
        content.Children.Add(explanation);

        var rotate = Ui.IconButton(Glyphs.Rotate, T("Rotate"));
        rotate.HorizontalAlignment = HorizontalAlignment.Right;
        rotate.VerticalAlignment = VerticalAlignment.Bottom;
        rotate.Margin = new Thickness(10);
        rotate.Click += (_, _) =>
        {
            quarterTurns = (quarterTurns + 1) % 4;
            UpdatePreview();
        };
        var previewBox = new Grid { Height = 220 };
        preview.Root.Margin = new Thickness(0, 0, 0, 44);
        previewBox.Children.Add(new Border { Style = Styles.Get("PreviewBoxStyle"), Child = preview.Root });
        previewBox.Children.Add(rotate);

        switch (sheet.Mode)
        {
            case LayoutSheetMode.Arrangement limits:
                var row = new Grid { ColumnSpacing = 20 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
                var counts = Arrangement(limits.MaxKeys, limits.MaxKnobs);
                Grid.SetColumn(counts, 1);
                row.Children.Add(previewBox);
                row.Children.Add(counts);
                content.Children.Add(row);
                break;
            case LayoutSheetMode.Catalog catalog:
                content.Children.Add(new ScrollViewer
                {
                    Height = 200,
                    Content = Catalog(catalog),
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                });
                content.Children.Add(previewBox);
                break;
            default:
                content.Children.Add(previewBox);
                break;
        }

        Dialog = DialogHost.Create();
        Dialog.Title = T("Choose the Keyboard’s Shape");
        Dialog.Content = content;
        Dialog.PrimaryButtonText = T("Done");
        Dialog.DefaultButton = ContentDialogButton.Primary;
        if (!sheet.IsRequired)
            Dialog.CloseButtonText = T("Cancel");
        Dialog.Resources["ContentDialogMaxWidth"] = 720.0;
        Dialog.PrimaryButtonClick += (_, _) => Accept();
        Dialog.Closing += (_, args) =>
        {
            // The first time there is nothing to go back to: Escape doesn't close it.
            if (sheet.IsRequired && !forced && !accepted && args.Result == ContentDialogResult.None)
                args.Cancel = true;
            else
                IsClosing = true;
        };
        preview.Root.SizeChanged += (_, _) => UpdatePreview();
        UpdatePreview();
    }

    public ContentDialog Dialog { get; }

    public bool IsClosing { get; private set; }

    /// <summary>
    /// Takes it away, whatever it is showing — the keyboard went away, or a choice was made elsewhere.
    /// </summary>
    public void Close()
    {
        forced = true;
        if (!IsClosing)
            Dialog.Hide();
    }

    private byte Columns => Math.Min(columns, Math.Max(keys, (byte)1));

    private void UpdatePreview() => preview.SetDrawing(sheet.Mode switch
    {
        LayoutSheetMode.Arrangement => editor.PreviewArrangement(keys, Columns, knobs, quarterTurns),
        LayoutSheetMode.Catalog catalog => editor.PreviewDrawing(layoutId ?? catalog.SelectedId, quarterTurns),
        _ => editor.PreviewDrawing(null, quarterTurns),
    });

    private void Accept()
    {
        accepted = true;
        IsClosing = true;
        switch (sheet.Mode)
        {
            case LayoutSheetMode.Arrangement:
                editor.ChooseArrangement(keys, Columns, knobs, quarterTurns);
                break;
            case LayoutSheetMode.Catalog catalog:
                editor.ChooseLayout(layoutId ?? catalog.SelectedId, quarterTurns);
                break;
            default:
                editor.ChooseLayout(null, quarterTurns);
                break;
        }
    }

    /// <summary>One of the cross-platform app's layouts, as a grid of small drawings.</summary>
    private WrapPanel Catalog(LayoutSheetMode.Catalog catalog)
    {
        var grid = new WrapPanel { HorizontalSpacing = 8, VerticalSpacing = 8 };
        var buttons = new List<(string Id, Button Button)>();
        foreach (var layout in catalog.Choices)
        {
            var face = new StackPanel { Spacing = 6, Margin = new Thickness(0, 6, 0, 4) };
            var thumbnail = new ShapeThumbnail(4);
            thumbnail.Root.Width = 80;
            thumbnail.Root.Height = 52;
            thumbnail.SetDrawing(layout.Drawing);
            face.Children.Add(thumbnail.Root);
            var counts = Ui.Text($"{layout.Keys} + {layout.Knobs}", "CaptionTextBlockStyle");
            counts.HorizontalAlignment = HorizontalAlignment.Center;
            face.Children.Add(counts);
            var button = new Button { Content = face, Style = Styles.Get("KeycapButtonStyle") };
            Ui.Tooltip(button, T("Keys: %lld · Knobs: %lld", layout.Keys, layout.Knobs));
            Ui.SetActive(button, layout.Id == catalog.SelectedId);
            if (layout.Id == catalog.SelectedId)
            {
                button.Loaded += (_, _) =>
                    button.StartBringIntoView(new BringIntoViewOptions { VerticalAlignmentRatio = 0.5 });
            }
            var id = layout.Id;
            button.Click += (_, _) =>
            {
                layoutId = id;
                foreach (var (other, each) in buttons)
                    Ui.SetActive(each, other == id);
                UpdatePreview();
            };
            buttons.Add((id, button));
            grid.Children.Add(button);
        }
        return grid;
    }

    /// <summary>
    /// How many keys, how many to a row, how many knobs. A keyboard has at least one of either: taking the last one
    /// away brings one of the other.
    /// </summary>
    private StackPanel Arrangement(byte maxKeys, byte maxKnobs)
    {
        var column = new StackPanel { Spacing = 18, VerticalAlignment = VerticalAlignment.Center };
        var (keysBox, keysSlider) = CountSlider(T("Keys"), 0, maxKeys, keys);
        var (columnsBox, columnsSlider) = CountSlider(T("Keys per row"), 1, Math.Max(keys, (byte)2), columns);
        columnsSlider.IsEnabled = keys >= 2;
        var knobsBox = new StackPanel { Spacing = 6 };
        knobsBox.Children.Add(Ui.Text(T("Knobs"), "DimTextStyle"));
        var knobsChoice = new Segmented();
        knobsChoice.SetItems([.. Enumerable.Range(0, maxKnobs + 1).Select(count => count.ToString(
            System.Globalization.CultureInfo.InvariantCulture))]);
        knobsChoice.SelectedIndex = knobs;
        knobsBox.Children.Add(knobsChoice.Control);
        column.Children.Add(keysBox);
        column.Children.Add(columnsBox);
        column.Children.Add(knobsBox);

        var settling = false;
        keysSlider.ValueChanged += (_, args) =>
        {
            if (settling)
                return;
            settling = true;
            keys = (byte)Math.Round(args.NewValue);
            columnsSlider.Maximum = Math.Max(keys, (byte)2);
            columns = Math.Min(columns, Math.Max(keys, (byte)1));
            columnsSlider.Value = columns;
            columnsSlider.IsEnabled = keys >= 2;
            if (keys == 0 && knobs == 0)
            {
                knobs = 1;
                knobsChoice.SelectedIndex = 1;
            }
            settling = false;
            UpdatePreview();
        };
        columnsSlider.ValueChanged += (_, args) =>
        {
            if (settling)
                return;
            columns = (byte)Math.Round(args.NewValue);
            UpdatePreview();
        };
        knobsChoice.Selected += value =>
        {
            knobs = (byte)value;
            if (knobs == 0 && keys == 0)
                keysSlider.Value = 1;
            UpdatePreview();
        };
        return column;
    }

    /// <summary>A whole number on a slider that stops at each one, with the number beside its name.</summary>
    private static (StackPanel Box, Slider Slider) CountSlider(string title, byte lower, byte upper, byte value)
    {
        var box = new StackPanel { Spacing = 2 };
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(Ui.Text(title, "DimTextStyle"));
        var number = Ui.Text(
            value.ToString(System.Globalization.CultureInfo.InvariantCulture), "BodyStrongTextBlockStyle");
        Grid.SetColumn(number, 1);
        row.Children.Add(number);
        var slider = new Slider
        {
            Minimum = lower,
            Maximum = upper,
            StepFrequency = 1,
            SmallChange = 1,
            SnapsTo = Microsoft.UI.Xaml.Controls.Primitives.SliderSnapsTo.StepValues,
            Value = value,
        };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(slider, title);
        slider.ValueChanged += (_, args) =>
            number.Text = ((int)Math.Round(args.NewValue)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        box.Children.Add(row);
        box.Children.Add(slider);
        return (box, slider);
    }
}

/// <summary>The licenses of the software the app comes with, each folded under its name, below a line on what they are.</summary>
internal static class LicensesDialog
{
    public static ContentDialog Create(AppInfo info, IReadOnlyList<LicenseNotice> licenses)
    {
        var list = new StackPanel { Spacing = 4 };
        list.Children.Add(new TextBlock
        {
            Text = T("%@ comes with the following software. Below is the license text each one ships with.", info.Name),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
        });
        foreach (var license in licenses)
        {
            var body = new StackPanel { Spacing = 8 };
            if (Uri.TryCreate(license.Url, UriKind.Absolute, out var uri))
                body.Children.Add(new HyperlinkButton
                {
                    Content = uri.Host + uri.AbsolutePath.TrimEnd('/'),
                    NavigateUri = uri,
                    Padding = new Thickness(0),
                });
            body.Children.Add(new TextBlock
            {
                Text = license.Text,
                Style = Styles.Get("MonoTextStyle"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
            });
            list.Children.Add(new Expander
            {
                Header = license.Name,
                Content = body,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
            });
        }
        var dialog = DialogHost.Create();
        dialog.Title = T("Included Software");
        dialog.Content = list;
        dialog.CloseButtonText = T("Done");
        dialog.DefaultButton = ContentDialogButton.Close;
        dialog.Resources["ContentDialogMaxWidth"] = 820.0;
        return dialog;
    }
}
