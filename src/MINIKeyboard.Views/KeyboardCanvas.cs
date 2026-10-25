using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using MINIKeyboard.Presentation;
using Windows.Foundation;
using Windows.UI;
using static MINIKeyboard.Views.Strings;

namespace MINIKeyboard.Views;

/// <summary>Where a drawing in layout space lands in an element: scaled to fit, in the middle.</summary>
internal readonly record struct Placement(double Scale, double OriginX, double OriginY, LayoutRect Bounds)
{
    public static Placement Fit(LayoutRect bounds, double width, double height, double margin)
    {
        var scale = Math.Max(
            Math.Min((width - 2 * margin) / bounds.Width, (height - 2 * margin) / bounds.Height), 0.01);
        return new Placement(
            scale, (width - bounds.Width * scale) / 2, (height - bounds.Height * scale) / 2, bounds);
    }

    public Rect Place(LayoutRect rect) => new(
        OriginX + (rect.X - Bounds.X) * Scale,
        OriginY + (rect.Y - Bounds.Y) * Scale,
        Math.Max(0, rect.Width * Scale),
        Math.Max(0, rect.Height * Scale));
}

/// <summary>
/// The connected keyboard as the view model draws it, turned the way it sits: a dark body, and a button for each key
/// and knob action — drawn as its cap, showing its number and what it does.
/// </summary>
internal sealed class KeyboardCanvas
{
    private const double Margin = 14;

    private static readonly Color KeyTop = Gray(0.46);
    private static readonly Color KeyBottom = Gray(0.38);
    private static readonly Color Outline = Color.FromArgb(38, 255, 255, 255);
    private static readonly Color Assigned = Color.FromArgb(191, 51, 209, 122);
    private static readonly Color NumberGreen = Color.FromArgb(255, 0x66, 0xDB, 0x66);
    private static readonly Color ChangedOrange = Color.FromArgb(255, 255, 120, 0);

    /// <summary>The simulator's colour: the bar across the window, the outline and badge here, the header's dot.</summary>
    public static readonly Color SimulatorOrange = Color.FromArgb(255, 0xC2, 0x41, 0x0C);

    private readonly Canvas canvas = new();
    private readonly Border body = new()
    {
        Background = Gradient(Gray(0.28), Gray(0.19)),
        BorderBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
        BorderThickness = new Thickness(1),
    };
    private readonly Border simulatorBadge = new()
    {
        Background = new SolidColorBrush(SimulatorOrange),
        CornerRadius = new CornerRadius(6),
        Padding = new Thickness(12, 3, 12, 5),
        IsHitTestVisible = false,
        Visibility = Visibility.Collapsed,
        Child = new TextBlock
        {
            Text = T("Simulator"),
            Foreground = new SolidColorBrush(Colors.White),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        },
    };
    private readonly List<ControlView> views = [];
    private KeyboardDrawing? drawing;
    private bool simulated;

    public KeyboardCanvas()
    {
        Root = new Grid { Background = new SolidColorBrush(Colors.Transparent) };
        Root.Children.Add(canvas);
        canvas.Children.Add(body);
        canvas.Children.Add(simulatorBadge);
        Root.SizeChanged += (_, _) => Layout();
    }

    public Grid Root { get; }

    /// <summary>A key or knob action was clicked.</summary>
    public event Action<ControlRef>? Selected;

    /// <summary>Draws <paramref name="next"/>; the buttons are made again only when the controls are others.</summary>
    public void SetDrawing(KeyboardDrawing? next)
    {
        var sameControls = (drawing, next) switch
        {
            (null, null) => true,
            ({ } current, { } other) => current.Controls.Count == other.Controls.Count
                && current.Controls.Zip(other.Controls)
                    .All(pair => pair.First.Control == pair.Second.Control && pair.First.Face == pair.Second.Face),
            _ => false,
        };
        if (!sameControls)
        {
            foreach (var view in views)
                canvas.Children.Remove(view.Button);
            views.Clear();
            foreach (var control in next?.Controls ?? [])
            {
                var view = new ControlView(control);
                var target = control.Control;
                view.Button.Click += (_, _) => Selected?.Invoke(target);
                views.Add(view);
                canvas.Children.Add(view.Button);
            }
        }
        if (next is not null)
        {
            foreach (var (view, control) in views.Zip(next.Controls))
                view.Update(control);
        }
        drawing = next;
        body.Visibility = next is null ? Visibility.Collapsed : Visibility.Visible;
        ShowSimulated();
    }

    /// <summary>Marks the keyboard as a simulated one: an orange outline, and a badge on its top edge.</summary>
    public void SetSimulated(bool value)
    {
        if (simulated == value)
            return;
        simulated = value;
        ShowSimulated();
    }

    private void ShowSimulated()
    {
        body.BorderBrush = new SolidColorBrush(simulated ? SimulatorOrange : Color.FromArgb(60, 0, 0, 0));
        body.BorderThickness = new Thickness(simulated ? 4 : 1);
        simulatorBadge.Visibility = simulated && drawing is not null ? Visibility.Visible : Visibility.Collapsed;
        Layout();
    }

    private void Layout()
    {
        if (drawing is null || Root.ActualWidth <= 0 || Root.ActualHeight <= 0)
            return;
        var placement = Placement.Fit(drawing.Bounds, Root.ActualWidth, Root.ActualHeight, Margin);
        var frame = placement.Place(drawing.Frame);
        Place(body, frame);
        body.CornerRadius = new CornerRadius(24 * placement.Scale);
        if (simulatorBadge.Visibility == Visibility.Visible)
        {
            simulatorBadge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var size = simulatorBadge.DesiredSize;
            Canvas.SetLeft(simulatorBadge, Math.Round(frame.X + (frame.Width - size.Width) / 2));
            Canvas.SetTop(simulatorBadge, Math.Round(frame.Y - size.Height / 2));
        }
        foreach (var view in views)
            view.Layout(placement);
    }

    private static void Place(FrameworkElement element, Rect rect)
    {
        Canvas.SetLeft(element, Math.Round(rect.X));
        Canvas.SetTop(element, Math.Round(rect.Y));
        element.Width = Math.Round(rect.Width);
        element.Height = Math.Round(rect.Height);
    }

    private static Color Gray(double white)
    {
        var value = (byte)Math.Round(Math.Clamp(white, 0, 1) * 255);
        return Color.FromArgb(255, value, value, value);
    }

    private static LinearGradientBrush Gradient(Color top, Color bottom) => new()
    {
        StartPoint = new Point(0, 0),
        EndPoint = new Point(0, 1),
        GradientStops =
        {
            new GradientStop { Color = top, Offset = 0 },
            new GradientStop { Color = bottom, Offset = 1 },
        },
    };

    private static Color Lighten(Color color, double amount)
    {
        byte Up(byte channel) => (byte)Math.Round(channel + (255 - channel) * amount);
        return Color.FromArgb(color.A, Up(color.R), Up(color.G), Up(color.B));
    }

    /// <summary>How light a colour looks, 0–1.</summary>
    private static double Luminance(Rgb color) =>
        (0.2126 * color.Red + 0.7152 * color.Green + 0.0722 * color.Blue) / 255;

    private static Color Accent => Application.Current.Resources.TryGetValue("SystemAccentColor", out var accent)
        && accent is Color color
            ? color
            : Color.FromArgb(255, 0, 120, 212);

    /// <summary>One key or knob action: its button, and the parts of its face.</summary>
    private sealed class ControlView
    {
        private readonly bool isKey;
        private readonly KnobAction knobAction;
        private readonly Border face = new();
        private readonly TextBlock? number;
        private readonly TextBlock? caption;
        private readonly FrameworkElement? icon;
        private Ellipse? pressDot;
        private readonly Ellipse changed = new()
        {
            Width = 7,
            Height = 7,
            Fill = new SolidColorBrush(ChangedOrange),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            IsHitTestVisible = false,
        };
        private ControlDrawing control;

        public ControlView(ControlDrawing control)
        {
            this.control = control;
            var content = new Grid();
            content.Children.Add(face);
            switch (control.Face)
            {
                case ControlFace.Key key:
                    isKey = true;
                    number = new TextBlock
                    {
                        Text = key.Number.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        IsHitTestVisible = false,
                    };
                    caption = new TextBlock
                    {
                        Foreground = new SolidColorBrush(Colors.White),
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        TextAlignment = TextAlignment.Center,
                        MaxLines = 3,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        IsHitTestVisible = false,
                    };
                    content.Children.Add(number);
                    content.Children.Add(caption);
                    break;
                case ControlFace.Knob knob:
                    knobAction = knob.Action;
                    icon = knob.Action == KnobAction.Press ? PressMark(out pressDot) : new FontIcon
                    {
                        Glyph = knob.Action is KnobAction.RotateLeft or KnobAction.PressRotateLeft
                            ? Glyphs.RotateLeft
                            : Glyphs.RotateRight,
                        Foreground = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        IsHitTestVisible = false,
                    };
                    content.Children.Add(icon);
                    break;
            }
            content.Children.Add(changed);
            Button = new Button { Content = content, Style = Styles.Get("CanvasControlButtonStyle") };
        }

        public Button Button { get; }

        /// <summary>A knob's press: a ring with a dot.</summary>
        private static Grid PressMark(out Ellipse dot)
        {
            var mark = new Grid { IsHitTestVisible = false };
            mark.Children.Add(new Ellipse
            {
                Stroke = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
                StrokeThickness = 2,
            });
            dot = new Ellipse { Fill = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)) };
            mark.Children.Add(dot);
            return mark;
        }

        public void Update(ControlDrawing next)
        {
            control = next;
            var title = Names.ControlTitleText(next.Control.Title());
            var text = Names.CaptionText(next.Caption);
            Ui.Tooltip(Button, next.Caption.IsAssigned ? $"{title}: {text}" : title);
            changed.Visibility = next.IsChanged ? Visibility.Visible : Visibility.Collapsed;
            Color outline;
            double width;
            if (next.IsSelected)
                (outline, width) = (Accent, 3);
            else if (!isKey && next.Caption.IsAssigned)
                (outline, width) = (Assigned, 1.5);
            else
                (outline, width) = (Outline, isKey ? 1 : 1.5);
            face.BorderBrush = new SolidColorBrush(outline);
            face.BorderThickness = new Thickness(width);
            if (isKey)
            {
                var (top, bottom) = next.LedColor is { } led
                    ? (Lighten(Color.FromArgb(255, led.Red, led.Green, led.Blue), 0.12),
                        Color.FromArgb(255, led.Red, led.Green, led.Blue))
                    : (KeyTop, KeyBottom);
                face.Background = Gradient(top, bottom);
                // On a lit key, black or white — whichever reads on its colour.
                var onLight = next.LedColor is { } lit && Luminance(lit) > 0.6;
                number!.Foreground = new SolidColorBrush(next.LedColor is null
                    ? NumberGreen
                    : onLight ? Color.FromArgb(210, 0, 0, 0) : Colors.White);
                caption!.Foreground = new SolidColorBrush(onLight ? Color.FromArgb(230, 0, 0, 0) : Colors.White);
                caption.Text = text;
            }
            else
            {
                var shade = knobAction == KnobAction.Press ? 0.46 : 0.36;
                face.Background = Gradient(Gray(shade + 0.04), Gray(shade - 0.04));
            }
        }

        public void Layout(Placement placement)
        {
            var rect = placement.Place(control.Rect);
            Place(Button, rect);
            var scale = placement.Scale;
            if (isKey)
            {
                var radius = 10 * scale;
                face.CornerRadius = new CornerRadius(radius);
                Button.CornerRadius = new CornerRadius(radius);
                number!.FontSize = Math.Max(9, 15 * scale);
                number.Margin = new Thickness(6 * scale + 2, 4 * scale + 1, 0, 0);
                caption!.FontSize = Math.Max(8, 13 * scale);
                caption.Margin = new Thickness(3, Math.Max(10, 20 * scale), 3, 3);
                caption.MaxWidth = Math.Max(0, rect.Width - 6);
                changed.Margin = new Thickness(0, 5, 5, 0);
            }
            else
            {
                var size = Math.Min(rect.Width, rect.Height);
                face.CornerRadius = new CornerRadius(size / 2);
                Button.CornerRadius = new CornerRadius(size / 2);
                if (icon is FontIcon glyph)
                {
                    glyph.FontSize = Math.Max(10, size * 0.38);
                }
                else if (icon is not null)
                {
                    var mark = Math.Max(10, size * 0.34);
                    icon.Width = mark;
                    icon.Height = mark;
                    if (pressDot is not null)
                        pressDot.Margin = new Thickness(mark * 0.3);
                }
                changed.Margin = new Thickness(0, 1, 1, 0);
            }
        }
    }
}

/// <summary>A keyboard's shape drawn small, for choosing one: keys, and each knob as one body.</summary>
internal sealed class ShapeThumbnail
{
    private static readonly SolidColorBrush Fill = new(Color.FromArgb(255, 140, 140, 140));

    private readonly Canvas canvas = new();
    private readonly double margin;
    private ShapeDrawing? drawing;

    public ShapeThumbnail(double margin)
    {
        this.margin = margin;
        Root = new Grid { Background = new SolidColorBrush(Colors.Transparent) };
        Root.Children.Add(canvas);
        Root.SizeChanged += (_, _) => Draw();
    }

    public Grid Root { get; }

    public void SetDrawing(ShapeDrawing? next)
    {
        drawing = next;
        Draw();
    }

    private void Draw()
    {
        canvas.Children.Clear();
        if (drawing is null || Root.ActualWidth <= 0 || Root.ActualHeight <= 0)
            return;
        var placement = Placement.Fit(drawing.Bounds, Root.ActualWidth, Root.ActualHeight, margin);
        foreach (var item in drawing.Items)
        {
            var rect = placement.Place(item.Rect);
            Shape shape;
            if (item.IsKnob)
            {
                var size = Math.Min(rect.Width, rect.Height);
                shape = new Ellipse { Width = size, Height = size };
                rect = new Rect(rect.X + (rect.Width - size) / 2, rect.Y + (rect.Height - size) / 2, size, size);
            }
            else
            {
                var radius = Math.Min(8 * placement.Scale, rect.Width / 2);
                shape = new Rectangle { Width = rect.Width, Height = rect.Height, RadiusX = radius, RadiusY = radius };
            }
            shape.Fill = Fill;
            Canvas.SetLeft(shape, rect.X);
            Canvas.SetTop(shape, rect.Y);
            canvas.Children.Add(shape);
        }
    }
}
