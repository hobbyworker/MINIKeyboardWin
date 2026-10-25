using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MINIKeyboard.Presentation;
using static MINIKeyboard.Views.Strings;

namespace MINIKeyboard.Views;

/// <summary>The selected key or knob action: what it does, and clearing it.</summary>
internal sealed class Inspector
{
    private readonly EditorModel editor;
    private readonly StackPanel selected = new() { Spacing = 10 };
    private readonly TextBlock placeholder = Ui.Text(T("Select a key or knob."), "DimTextStyle", wrap: true);
    private readonly TextBlock held = Ui.Text("", "AccentCaptionTextStyle", wrap: true);
    private readonly Button clearKey = new() { Content = T("Clear") };
    private readonly Button clearLayer = new() { Content = T("Clear Layer") };
    private readonly TextBlock unsaved = Ui.Text("", "CaptionDimTextStyle");
    private SelectionState? shown;
    private bool built;

    public Inspector(EditorModel editor)
    {
        this.editor = editor;
        var top = new StackPanel { Spacing = 14 };
        top.Children.Add(placeholder);
        top.Children.Add(selected);
        top.Children.Add(held);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        buttons.Children.Add(clearKey);
        buttons.Children.Add(clearLayer);
        var bottom = new StackPanel { Spacing = 12 };
        bottom.Children.Add(buttons);
        bottom.Children.Add(unsaved);
        clearKey.Click += (_, _) => editor.ClearSelected();
        clearLayer.Click += (_, _) => editor.ClearLayer();

        var root = new Grid { Padding = new Thickness(16), RowSpacing = 14 };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var scroller = new ScrollViewer
        {
            Content = top,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        Grid.SetRow(bottom, 1);
        root.Children.Add(scroller);
        root.Children.Add(bottom);
        Root = root;
    }

    public Grid Root { get; }

    public void Refresh()
    {
        var state = editor.SelectionState;
        if (!built || !Equals(shown, state))
        {
            selected.Children.Clear();
            placeholder.Visibility = state is null ? Visibility.Visible : Visibility.Collapsed;
            selected.Visibility = state is null ? Visibility.Collapsed : Visibility.Visible;
            if (state is not null)
                Build(state);
            (shown, built) = (state, true);
        }
        if (editor.HeldModifierText is { } text)
        {
            held.Text = T("Next key with %@", text);
            held.Visibility = Visibility.Visible;
        }
        else
        {
            held.Visibility = Visibility.Collapsed;
        }
        var ready = editor.IsReady;
        clearKey.IsEnabled = ready && editor.SelectedControl is not null;
        clearLayer.IsEnabled = ready;
        if (editor.HasUnsavedChanges)
        {
            unsaved.Text = $"● {T("Unsaved changes")}";
            unsaved.Style = Styles.Get("UnsavedTextStyle");
            unsaved.Visibility = Visibility.Visible;
        }
        else if (ready)
        {
            unsaved.Text = $"✓ {T("No unsaved changes")}";
            unsaved.Style = Styles.Get("CaptionDimTextStyle");
            unsaved.Visibility = Visibility.Visible;
        }
        else
        {
            unsaved.Visibility = Visibility.Collapsed;
        }
    }

    private void Build(SelectionState state)
    {
        var heading = new StackPanel { Spacing = 2 };
        heading.Children.Add(Ui.Text(Names.ControlTitleText(state.Title), "SubtitleTextBlockStyle", wrap: true));
        if (state.LayerNumber is { } layer)
            heading.Children.Add(Ui.Caption(T("Layer %lld", layer)));
        selected.Children.Add(heading);
        selected.Children.Add(Ui.Divider());
        switch (state.Binding)
        {
            case BindingState.Nothing:
                selected.Children.Add(Ui.Text(T("Nothing"), "DimTextStyle"));
                break;
            case BindingState.Presses presses:
                Presses(presses);
                break;
            case BindingState.Media media:
                selected.Children.Add(Large(Names.MediaGlyph(media.Choice), Names.MediaTitle(media.Choice)));
                break;
            case BindingState.OtherMedia other:
                selected.Children.Add(Ui.Text(
                    Names.CaptionText(new ActionCaption.OtherMedia(other.Usage)), "BodyStrongTextBlockStyle"));
                break;
            case BindingState.Mouse mouse:
                selected.Children.Add(Large(Names.MouseGlyph(mouse.Choice), Names.MouseTitle(mouse.Choice)));
                break;
            default:
                selected.Children.Add(Ui.Text(
                    T("Not known — this keyboard doesn’t tell what its keys do. Whatever you set here is written when you save."),
                    "DimTextStyle",
                    wrap: true));
                break;
        }
    }

    private void Presses(BindingState.Presses presses)
    {
        var chips = new WrapPanel { HorizontalSpacing = 6, VerticalSpacing = 6 };
        foreach (var stroke in presses.Strokes)
        {
            var text = Ui.Text(stroke, "BodyStrongTextBlockStyle");
            text.IsTextSelectionEnabled = true;
            chips.Children.Add(new Border { Style = Styles.Get("ChipStyle"), Child = text });
        }
        if (chips.Children.Count > 0)
            selected.Children.Add(chips);
        if (presses.KeyDelay > 0)
            selected.Children.Add(Ui.Caption(T("Waits %lld ms", presses.KeyDelay)));
        selected.Children.Add(Ui.Caption(T("%lld of %lld presses", presses.Used, presses.Limit)));
        var remove = Ui.IconButton(Glyphs.Undo, T("Remove Last Press"));
        remove.IsEnabled = !presses.Strokes.IsEmpty;
        remove.Click += (_, _) => editor.RemoveLastStroke();
        selected.Children.Add(remove);
    }

    private static StackPanel Large(string glyph, string title)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        row.Children.Add(Ui.Icon(glyph, 20));
        var text = Ui.Text(title, "BodyStrongTextBlockStyle");
        text.FontSize = 16;
        text.VerticalAlignment = VerticalAlignment.Center;
        row.Children.Add(text);
        return row;
    }
}
