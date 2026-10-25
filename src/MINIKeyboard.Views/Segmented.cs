using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace MINIKeyboard.Views;

/// <summary>
/// A row of choices, one of them chosen — layers, the Keys page's layout, a number of knobs. Setting the choice from
/// code doesn't raise <see cref="Selected"/>.
/// </summary>
internal sealed class Segmented
{
    private readonly StackPanel row = new() { Orientation = Orientation.Horizontal, Spacing = 2 };
    private readonly List<ToggleButton> items = [];
    private int selected = -1;

    public Segmented()
    {
        Root = new Border { Style = Styles.Get("SegmentedBorderStyle"), Child = row };
        Control = Ui.Enabler(Root);
        Control.HorizontalAlignment = HorizontalAlignment.Left;
        Control.VerticalAlignment = VerticalAlignment.Center;
    }

    public Border Root { get; }

    /// <summary>The element to place: it turns on and off as a whole.</summary>
    public ContentControl Control { get; }

    /// <summary>The user chose another.</summary>
    public event Action<int>? Selected;

    public int Count => items.Count;

    public int SelectedIndex
    {
        get => selected;
        set
        {
            selected = value;
            for (var index = 0; index < items.Count; index++)
                items[index].IsChecked = index == selected;
        }
    }

    public void SetItems(IReadOnlyList<string> labels, double? itemWidth = null)
    {
        row.Children.Clear();
        items.Clear();
        for (var index = 0; index < labels.Count; index++)
        {
            var item = new ToggleButton { Content = labels[index], Style = Styles.Get("SegmentItemStyle") };
            if (itemWidth is { } width)
                item.MinWidth = width;
            AutomationProperties.SetName(item, labels[index]);
            var chosen = index;
            item.Click += (_, _) =>
            {
                var changed = chosen != selected;
                SelectedIndex = chosen;
                if (changed)
                    Selected?.Invoke(chosen);
            };
            items.Add(item);
            row.Children.Add(item);
        }
        SelectedIndex = selected < items.Count ? selected : -1;
    }
}
