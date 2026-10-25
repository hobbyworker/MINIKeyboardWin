using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace MINIKeyboard.Views;

/// <summary>Children side by side, onto as many lines as they need.</summary>
internal sealed partial class WrapPanel : Panel
{
    public double HorizontalSpacing { get; set; } = 4;

    public double VerticalSpacing { get; set; } = 4;

    /// <summary>Every child as tall as the tallest, so a grid of caps stays a grid.</summary>
    public bool UniformHeight { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        return Lay(availableSize.Width, arrange: false);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Lay(finalSize.Width, arrange: true);
        return finalSize;
    }

    private Size Lay(double width, bool arrange)
    {
        double x = 0, y = 0, line = 0, widest = 0;
        var tallest = UniformHeight
            ? Children.Where(child => child.Visibility == Visibility.Visible).Select(child => child.DesiredSize.Height)
                .DefaultIfEmpty(0).Max()
            : 0;
        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
                continue;
            var size = UniformHeight ? new Size(child.DesiredSize.Width, tallest) : child.DesiredSize;
            if (x > 0 && x + size.Width > width + 0.5)
            {
                y += line + VerticalSpacing;
                x = 0;
                line = 0;
            }
            if (arrange)
                child.Arrange(new Rect(x, y, size.Width, size.Height));
            widest = Math.Max(widest, x + size.Width);
            x += size.Width + HorizontalSpacing;
            line = Math.Max(line, size.Height);
        }
        return new Size(widest, y + line);
    }
}

/// <summary>
/// The full-size keyboard of the Keys page: each cap placed by its row and its column and width in key widths, a key
/// width being the panel's width over a row's width.
/// </summary>
internal sealed partial class BoardPanel : Panel
{
    public const double CapHeight = 30;
    private const double RowSpacing = 3;
    private const double CapSpacing = 2;

    private readonly Dictionary<UIElement, (int Row, double Column, double Width)> places = [];

    public double RowWidth { get; set; } = 22.5;

    public void Add(UIElement cap, int row, double column, double width)
    {
        places[cap] = (row, column, width);
        Children.Add(cap);
    }

    public void Clear()
    {
        places.Clear();
        Children.Clear();
    }

    private int Rows => places.Count == 0 ? 0 : places.Values.Max(place => place.Row) + 1;

    private double Unit(double width) => double.IsInfinity(width) ? 34 : width / RowWidth;

    protected override Size MeasureOverride(Size availableSize)
    {
        var unit = Unit(availableSize.Width);
        foreach (var child in Children)
        {
            var (_, _, width) = places[child];
            child.Measure(new Size(Math.Max(0, width * unit - CapSpacing), CapHeight));
        }
        var rows = Rows;
        return new Size(unit * RowWidth, rows == 0 ? 0 : rows * CapHeight + (rows - 1) * RowSpacing);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var unit = Unit(finalSize.Width);
        foreach (var child in Children)
        {
            var (row, column, width) = places[child];
            child.Arrange(new Rect(
                column * unit + CapSpacing / 2,
                row * (CapHeight + RowSpacing),
                Math.Max(0, width * unit - CapSpacing),
                CapHeight));
        }
        return finalSize;
    }
}
