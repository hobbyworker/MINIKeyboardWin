using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using MINIKeyboard.Presentation;

namespace MINIKeyboard.Views;

/// <summary>Small pieces the windows are built from.</summary>
internal static class Ui
{
    public static TextBlock Text(string text, string style = "BodyTextBlockStyle", bool wrap = false) => new()
    {
        Text = text,
        Style = Styles.Get(style),
        TextWrapping = wrap ? TextWrapping.WrapWholeWords : TextWrapping.NoWrap,
    };

    /// <summary>Small, dimmed text that wraps.</summary>
    public static TextBlock Caption(string text) => Text(text, "CaptionDimTextStyle", wrap: true);

    public static FontIcon Icon(string glyph, double size = 16, string? style = null)
    {
        var icon = new FontIcon { Glyph = glyph, FontSize = size };
        if (style is not null)
            icon.Style = Styles.Get(style);
        return icon;
    }

    /// <summary>A thin line between parts.</summary>
    public static Border Divider(bool vertical = false) => new()
    {
        Style = Styles.Get("DividerStyle"),
        Width = vertical ? 1 : double.NaN,
        Height = vertical ? double.NaN : 1,
    };

    /// <summary>A titled group of controls in a page, with a note under it.</summary>
    public static StackPanel Section(string title, string? note, UIElement content)
    {
        var section = new StackPanel { Spacing = 8 };
        section.Children.Add(Text(title, "SectionTitleTextStyle"));
        section.Children.Add(content);
        if (note is not null)
            section.Children.Add(Caption(note));
        return section;
    }

    /// <summary>A key cap in the action pages.</summary>
    public static Button Keycap(string label)
    {
        var button = new Button { Content = label, Style = Styles.Get("KeycapButtonStyle") };
        return button;
    }

    /// <summary>A key cap with an icon above its title.</summary>
    public static Button TallKeycap(string glyph, string title)
    {
        var content = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
        content.Children.Add(Icon(glyph, 16));
        content.Children.Add(new TextBlock
        {
            Text = title,
            TextWrapping = TextWrapping.WrapWholeWords,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        return new Button
        {
            Content = content,
            Style = Styles.Get("KeycapButtonStyle"),
            MinHeight = 56,
            VerticalAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(6, 6, 6, 7),
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
    }

    /// <summary>A key cap's face: the key, and above it the character it makes with Shift.</summary>
    public static Button KeyButton(KeyUsage key, bool compact)
    {
        // The labels are the same in every language: in a font for English, a backslash stays one.
        var content = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Language = "en-US" };
        if (key.Shifted is { } shifted)
        {
            content.Children.Add(new TextBlock
            {
                Text = shifted,
                FontSize = compact ? 9 : 10,
                Opacity = 0.6,
                HorizontalAlignment = HorizontalAlignment.Center,
                LineHeight = compact ? 10 : 12,
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            });
        }
        content.Children.Add(new TextBlock
        {
            Text = key.Label,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        var button = new Button
        {
            Content = content,
            Style = Styles.Get(compact ? "CompactKeycapButtonStyle" : "KeycapButtonStyle"),
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        Tooltip(button, Names.KeyName(key));
        return button;
    }

    /// <summary>Marks a cap as the one in use.</summary>
    public static void SetActive(Button button, bool active)
    {
        var compact = button.Style == Styles.Get("CompactKeycapButtonStyle")
            || button.Style == Styles.Get("CompactKeycapActiveButtonStyle");
        var style = (compact, active) switch
        {
            (true, true) => "CompactKeycapActiveButtonStyle",
            (true, false) => "CompactKeycapButtonStyle",
            (false, true) => "KeycapActiveButtonStyle",
            (false, false) => "KeycapButtonStyle",
        };
        var wanted = Styles.Get(style);
        if (button.Style != wanted)
            button.Style = wanted;
    }

    /// <summary>A tooltip, which is also the element's name for assistive tools.</summary>
    public static void Tooltip(FrameworkElement element, string text)
    {
        ToolTipService.SetToolTip(element, text);
        AutomationProperties.SetName(element, text);
    }

    /// <summary>A button with an icon before its label.</summary>
    public static Button IconButton(string glyph, string label)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        content.Children.Add(Icon(glyph, 14));
        content.Children.Add(new TextBlock { Text = label });
        return new Button { Content = content };
    }

    /// <summary>Something that turns on and off with what it holds: a panel isn't a control, this is.</summary>
    public static ContentControl Enabler(UIElement content) => new()
    {
        Content = content,
        IsTabStop = false,
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        VerticalContentAlignment = VerticalAlignment.Stretch,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
    };
}
