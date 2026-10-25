using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MINIKeyboard.Presentation;
using static MINIKeyboard.Views.Strings;

namespace MINIKeyboard.Views;

/// <summary>The shown layer's lights, in what the keyboard takes.</summary>
internal sealed class LightingView(EditorModel editor)
{
    private LightingPage? shown;
    private bool built;

    public StackPanel Root { get; } = new() { Spacing = 18 };

    public void Refresh()
    {
        var page = editor.LightingPage;
        if (built && Equals(shown, page))
            return;
        Root.Children.Clear();
        switch (page)
        {
            case LightingPage.ModesOnly modes:
                Modes(modes.Modes, modes.Active);
                break;
            case LightingPage.ColorAndMode colorAndMode:
                var row = new WrapPanel { HorizontalSpacing = 8, VerticalSpacing = 8 };
                foreach (var color in colorAndMode.Colors)
                {
                    var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                    content.Children.Add(new Microsoft.UI.Xaml.Shapes.Ellipse
                    {
                        Width = 12,
                        Height = 12,
                        Fill = new SolidColorBrush(Names.LedSwatch(color)),
                        VerticalAlignment = VerticalAlignment.Center,
                    });
                    content.Children.Add(new TextBlock { Text = Names.LedColorTitle(color) });
                    var button = new Button { Content = content, Style = Styles.Get("KeycapButtonStyle") };
                    Ui.SetActive(button, colorAndMode.ActiveColor == color);
                    button.Click += (_, _) => editor.SetLightColor(color);
                    row.Children.Add(button);
                }
                Root.Children.Add(Ui.Section(T("Color"), null, row));
                Root.Children.Add(Effects(
                    colorAndMode.Modes,
                    colorAndMode.ActiveMode,
                    T("Lighting is set per layer; no key selection is needed.")));
                break;
            case LightingPage.PerKeyColor perKey:
                Root.Children.Add(Effects(perKey.Modes, perKey.ActiveMode, null));
                var colors = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 18 };
                var palette = new Grid { RowSpacing = 5, ColumnSpacing = 5 };
                for (var column = 0; column < 14; column++)
                    palette.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                for (var index = 0; index < Rgb.Palette.Count; index++)
                {
                    if (index % 14 == 0)
                        palette.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    var color = Rgb.Palette[index];
                    var swatch = Swatch(color, 26);
                    ToolTipService.SetToolTip(swatch, color.Hex());
                    swatch.Click += (_, _) => editor.SetKeyColor(color);
                    Grid.SetColumn(swatch, index % 14);
                    Grid.SetRow(swatch, index / 14);
                    palette.Children.Add(swatch);
                }
                colors.Children.Add(palette);
                if (perKey.Selected is { } selected)
                {
                    var preview = new StackPanel { Spacing = 6 };
                    preview.Children.Add(new Border
                    {
                        Width = 60,
                        Height = 60,
                        CornerRadius = new CornerRadius(10),
                        Background = new SolidColorBrush(ToColor(selected.Color)),
                        Style = Styles.Get("SwatchBorderStyle"),
                        HorizontalAlignment = HorizontalAlignment.Left,
                    });
                    preview.Children.Add(Ui.Text(Names.ControlTitleText(selected.Title), "CaptionTextBlockStyle"));
                    var hex = Ui.Text(selected.Color.Hex(), "MonoDimTextStyle");
                    hex.FontSize = 12;
                    preview.Children.Add(hex);
                    colors.Children.Add(preview);
                }
                Root.Children.Add(Ui.Section(
                    T("Color"),
                    T("Select one of keys 1–%lld on the keyboard, then pick its color.", perKey.Keys),
                    colors));
                break;
        }
        (shown, built) = (page, true);
    }

    /// <summary>
    /// The 1189:8890's modes. What a mode is set to isn't known until the user sets it — the keyboard can't be asked.
    /// </summary>
    private void Modes(IReadOnlyList<byte> modes, byte? active)
    {
        var row = new Grid { ColumnSpacing = 10 };
        for (var index = 0; index < modes.Count; index++)
        {
            var mode = modes[index];
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var content = new StackPanel { Spacing = 6, Margin = new Thickness(0, 8, 0, 8) };
            var icon = Ui.Icon(Names.LightingModeGlyph(mode), 20);
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            content.Children.Add(icon);
            var title = Ui.Text(Names.LightingModeTitle(mode, k8890: true), "BodyStrongTextBlockStyle");
            title.HorizontalAlignment = HorizontalAlignment.Center;
            content.Children.Add(title);
            if (Names.LightingModeDetail(mode) is { } detail)
            {
                var caption = Ui.Text(detail, "CaptionTextBlockStyle", wrap: true);
                caption.Opacity = 0.8;
                caption.TextAlignment = TextAlignment.Center;
                caption.HorizontalAlignment = HorizontalAlignment.Center;
                content.Children.Add(caption);
            }
            var button = new Button
            {
                Content = content,
                Style = Styles.Get("KeycapButtonStyle"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Top,
            };
            Ui.SetActive(button, active == mode);
            button.Click += (_, _) => editor.SetLightingMode(mode);
            Grid.SetColumn(button, index);
            row.Children.Add(button);
        }
        Root.Children.Add(Ui.Section(T("LED"), T("Lighting is set per layer; no key selection is needed."), row));
    }

    private StackPanel Effects(IReadOnlyList<byte> modes, byte? active, string? note)
    {
        var row = new WrapPanel { HorizontalSpacing = 8, VerticalSpacing = 8 };
        foreach (var mode in modes)
        {
            var button = Ui.Keycap(Names.LightingModeTitle(mode, k8890: false));
            button.MinWidth = 90;
            Ui.SetActive(button, active == mode);
            button.Click += (_, _) => editor.SetLightingMode(mode);
            row.Children.Add(button);
        }
        return Ui.Section(T("Effect"), note, row);
    }

    private static Windows.UI.Color ToColor(Rgb color) =>
        Windows.UI.Color.FromArgb(255, color.Red, color.Green, color.Blue);

    /// <summary>A square of one colour, as a button.</summary>
    private static Button Swatch(Rgb color, double size) => new()
    {
        Width = size,
        Height = size,
        MinWidth = 0,
        MinHeight = 0,
        Padding = new Thickness(0),
        CornerRadius = new CornerRadius(5),
        Background = new SolidColorBrush(ToColor(color)),
        Resources =
        {
            ["ButtonBackground"] = new SolidColorBrush(ToColor(color)),
            ["ButtonBackgroundPointerOver"] = new SolidColorBrush(ToColor(color)) { Opacity = 0.8 },
            ["ButtonBackgroundPressed"] = new SolidColorBrush(ToColor(color)) { Opacity = 0.6 },
        },
    };
}

/// <summary>Waits: before each press on the standard format, for the whole key on the legacy one.</summary>
internal sealed class DelayView(EditorModel editor)
{
    /// <summary>The rows as last built — the presses' text — and their fields, so a changed number doesn't rebuild
    /// them under the user's typing.</summary>
    private string? shownShape;
    private List<NumberBox> fields = [];
    private bool updating;

    public StackPanel Root { get; } = new() { Spacing = 8 };

    public void Refresh()
    {
        var page = editor.DelayPage;
        var shape = page switch
        {
            DelayPage.PerPress perPress =>
                "press:" + string.Join("\u0001", perPress.Presses.Select(press => press.Text)),
            DelayPage.PerKey perKey => perKey.Value is null ? "key:none" : "key:one",
            _ => "none",
        };
        List<uint> values = page switch
        {
            DelayPage.PerPress perPress => [.. perPress.Presses.Select(press => press.Delay)],
            DelayPage.PerKey { Value: { } value } => [value],
            _ => [],
        };
        if (shownShape != shape)
        {
            fields = Build(page);
            shownShape = shape;
        }
        updating = true;
        foreach (var (field, value) in fields.Zip(values))
        {
            if (double.IsNaN(field.Value) || (uint)field.Value != value)
                field.Value = value;
        }
        updating = false;
    }

    private List<NumberBox> Build(DelayPage page)
    {
        Root.Children.Clear();
        var built = new List<NumberBox>();
        switch (page)
        {
            case DelayPage.PerPress perPress:
                UIElement content;
                if (perPress.Presses.IsEmpty)
                {
                    content = Ui.Text(T("The selected key has no presses."), "DimTextStyle");
                }
                else
                {
                    var grid = new Grid { ColumnSpacing = 14, RowSpacing = 6 };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    foreach (var press in perPress.Presses)
                    {
                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        var name = Ui.Text(T("Press %lld", press.Index + 1), "DimTextStyle");
                        var text = Ui.Text(press.Text, "BodyStrongTextBlockStyle");
                        var index = press.Index;
                        var (field, controls) = Milliseconds(perPress.Range, value => editor.SetDelay(index, value));
                        built.Add(field);
                        FrameworkElement[] cells = [name, text, controls];
                        for (var column = 0; column < cells.Length; column++)
                        {
                            cells[column].VerticalAlignment = VerticalAlignment.Center;
                            Grid.SetRow(cells[column], press.Index);
                            Grid.SetColumn(cells[column], column);
                            grid.Children.Add(cells[column]);
                        }
                    }
                    content = grid;
                }
                Root.Children.Add(Ui.Section(
                    T("Delay"),
                    T("Each press can wait before it is sent. Give the key its presses on the Keys page first."),
                    content));
                break;
            case DelayPage.PerKey perKey:
                UIElement keyContent;
                if (perKey.Value is not null)
                {
                    var (field, controls) = Milliseconds(perKey.Range, editor.SetKeyDelay);
                    built.Add(field);
                    keyContent = controls;
                }
                else
                {
                    keyContent = Ui.Text(T("Select a key or knob."), "DimTextStyle");
                }
                Root.Children.Add(
                    Ui.Section(T("Delay"), T("One wait for the whole key. Its presses stay."), keyContent));
                break;
        }
        return built;
    }

    /// <summary>A number of milliseconds, kept in the range.</summary>
    private (NumberBox Field, StackPanel Controls) Milliseconds(DelayRange range, Action<uint> commit)
    {
        var field = new NumberBox
        {
            Minimum = range.Min,
            Maximum = range.Max,
            SmallChange = 10,
            LargeChange = 100,
            Value = range.Min,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
            ValidationMode = NumberBoxValidationMode.InvalidInputOverwritten,
            MinWidth = 140,
        };
        field.ValueChanged += (_, args) =>
        {
            if (!updating && !double.IsNaN(args.NewValue))
                commit((uint)Math.Round(Math.Clamp(args.NewValue, range.Min, range.Max)));
        };
        var controls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        controls.Children.Add(field);
        var unit = Ui.Text("ms", "DimTextStyle");
        unit.VerticalAlignment = VerticalAlignment.Center;
        controls.Children.Add(unit);
        return (field, controls);
    }
}
