using MINIKeyboard.Domain;

namespace MINIKeyboard.Presentation;

// What the screens offer and show, in their own words. The domain's types stay behind the view model: views hand
// these back, and the view model turns them into the domain's.

/// <summary>A modifier key, as the Keys page offers it.</summary>
public enum ModifierKey
{
    Control,
    Alt,
    Shift,
    Super,
    RightControl,
    RightAlt,
    RightShift,
    RightSuper,
}

public static class ModifierKeys
{
    public static IReadOnlyList<ModifierKey> All { get; } =
    [
        ModifierKey.Control, ModifierKey.Alt, ModifierKey.Shift, ModifierKey.Super, ModifierKey.RightControl,
        ModifierKey.RightAlt, ModifierKey.RightShift, ModifierKey.RightSuper,
    ];

    extension(ModifierKey modifier)
    {
        /// <summary>"Ctrl", or "RCtrl" for a right-hand one. Super is "Win", as the vendor tools call it.</summary>
        public string Label() => modifier switch
        {
            ModifierKey.Control => "Ctrl",
            ModifierKey.Alt => "Alt",
            ModifierKey.Shift => "Shift",
            ModifierKey.Super => "Win",
            ModifierKey.RightControl => "RCtrl",
            ModifierKey.RightAlt => "RAlt",
            ModifierKey.RightShift => "RShift",
            ModifierKey.RightSuper => "RWin",
            _ => throw new ArgumentOutOfRangeException(nameof(modifier)),
        };

        internal Modifiers ToModifiers() => modifier switch
        {
            ModifierKey.Control => Modifiers.Control,
            ModifierKey.Alt => Modifiers.Alt,
            ModifierKey.Shift => Modifiers.Shift,
            ModifierKey.Super => Modifiers.Super,
            ModifierKey.RightControl => Modifiers.RightControl,
            ModifierKey.RightAlt => Modifiers.RightAlt,
            ModifierKey.RightShift => Modifiers.RightShift,
            ModifierKey.RightSuper => Modifiers.RightSuper,
            _ => throw new ArgumentOutOfRangeException(nameof(modifier)),
        };
    }
}

/// <summary>A media key the app names.</summary>
public enum MediaChoice
{
    PlayPause,
    Stop,
    PreviousTrack,
    NextTrack,
    Mute,
    VolumeUp,
    VolumeDown,
    BrightnessUp,
    BrightnessDown,
    BassUp,
    BassDown,
    TrebleUp,
    TrebleDown,
    MediaPlayer,
    Email,
    Calculator,
    MyComputer,
    WebHome,
    WebBack,
    WebForward,
    WebRefresh,
}

public static class MediaChoices
{
    public static IReadOnlyList<MediaChoice> All { get; } = [.. Enum.GetValues<MediaChoice>()];

    extension(MediaChoice choice)
    {
        internal MediaKey Key() => choice switch
        {
            MediaChoice.PlayPause => MediaKey.PlayPause,
            MediaChoice.Stop => MediaKey.Stop,
            MediaChoice.PreviousTrack => MediaKey.PreviousTrack,
            MediaChoice.NextTrack => MediaKey.NextTrack,
            MediaChoice.Mute => MediaKey.Mute,
            MediaChoice.VolumeUp => MediaKey.VolumeUp,
            MediaChoice.VolumeDown => MediaKey.VolumeDown,
            MediaChoice.BrightnessUp => MediaKey.BrightnessUp,
            MediaChoice.BrightnessDown => MediaKey.BrightnessDown,
            MediaChoice.BassUp => MediaKey.BassUp,
            MediaChoice.BassDown => MediaKey.BassDown,
            MediaChoice.TrebleUp => MediaKey.TrebleUp,
            MediaChoice.TrebleDown => MediaKey.TrebleDown,
            MediaChoice.MediaPlayer => MediaKey.MediaPlayer,
            MediaChoice.Email => MediaKey.Email,
            MediaChoice.Calculator => MediaKey.Calculator,
            MediaChoice.MyComputer => MediaKey.MyComputer,
            MediaChoice.WebHome => MediaKey.WebHome,
            MediaChoice.WebBack => MediaKey.WebBack,
            MediaChoice.WebForward => MediaKey.WebForward,
            MediaChoice.WebRefresh => MediaKey.WebRefresh,
            _ => throw new ArgumentOutOfRangeException(nameof(choice)),
        };
    }

    extension(MediaKey key)
    {
        /// <summary>The app's name for the key; null for a consumer usage it has none for.</summary>
        internal MediaChoice? Choice() => All.Cast<MediaChoice?>().FirstOrDefault(choice => choice!.Value.Key() == key);
    }
}

/// <summary>A mouse action.</summary>
public enum MouseChoice
{
    LeftClick,
    MiddleClick,
    RightClick,
    WheelUp,
    WheelDown,
    ControlWheelUp,
    ControlWheelDown,
    ShiftWheelUp,
    ShiftWheelDown,
    AltWheelUp,
    AltWheelDown,
}

public static class MouseChoices
{
    public static IReadOnlyList<MouseChoice> All { get; } = [.. Enum.GetValues<MouseChoice>()];

    extension(MouseChoice choice)
    {
        internal MouseAction ToAction() => choice switch
        {
            MouseChoice.LeftClick => MouseAction.LeftClick,
            MouseChoice.MiddleClick => MouseAction.MiddleClick,
            MouseChoice.RightClick => MouseAction.RightClick,
            MouseChoice.WheelUp => MouseAction.WheelUp,
            MouseChoice.WheelDown => MouseAction.WheelDown,
            MouseChoice.ControlWheelUp => MouseAction.ControlWheelUp,
            MouseChoice.ControlWheelDown => MouseAction.ControlWheelDown,
            MouseChoice.ShiftWheelUp => MouseAction.ShiftWheelUp,
            MouseChoice.ShiftWheelDown => MouseAction.ShiftWheelDown,
            MouseChoice.AltWheelUp => MouseAction.AltWheelUp,
            MouseChoice.AltWheelDown => MouseAction.AltWheelDown,
            _ => throw new ArgumentOutOfRangeException(nameof(choice)),
        };
    }

    extension(MouseAction action)
    {
        /// <summary>Every mouse action has a choice.</summary>
        internal MouseChoice Choice() =>
            All.Cast<MouseChoice?>().FirstOrDefault(choice => choice!.Value.ToAction() == action)
            ?? throw new ArgumentOutOfRangeException(nameof(action));
    }
}

/// <summary>A colour a layer of a legacy-format or unidentified keyboard lights in.</summary>
public enum LedColor
{
    Red,
    Orange,
    Yellow,
    Green,
    Cyan,
    Blue,
    Purple,
}

public static class LedColors
{
    public static IReadOnlyList<LedColor> All { get; } = [.. Enum.GetValues<LedColor>()];

    extension(LedColor color)
    {
        internal LightColor ToLightColor() => LightColors.All[(int)color];
    }

    extension(LightColor color)
    {
        internal LedColor ToLedColor() => All[(int)color - 1];
    }
}

/// <summary>A key's colour on a keyboard that lights each key.</summary>
public readonly partial record struct Rgb(byte Red, byte Green, byte Blue)
{
    internal static Rgb Of(KeyColor color) => new(color.Red, color.Green, color.Blue);

    internal KeyColor ToKeyColor() => new(Red, Green, Blue);

    /// <summary>"#00FF00".</summary>
    public string Hex() => $"#{Red:X2}{Green:X2}{Blue:X2}";
}

/// <summary>One action of a knob.</summary>
public enum KnobAction
{
    RotateLeft,
    Press,
    RotateRight,
    PressRotateLeft,
    PressRotateRight,
}

public static class KnobActions
{
    extension(KnobPart part)
    {
        internal KnobAction ToKnobAction() => part switch
        {
            KnobPart.RotateLeft => KnobAction.RotateLeft,
            KnobPart.Press => KnobAction.Press,
            KnobPart.RotateRight => KnobAction.RotateRight,
            KnobPart.PressRotateLeft => KnobAction.PressRotateLeft,
            KnobPart.PressRotateRight => KnobAction.PressRotateRight,
            _ => throw new ArgumentOutOfRangeException(nameof(part)),
        };
    }

    extension(KnobAction action)
    {
        internal KnobPart ToPart() => action switch
        {
            KnobAction.RotateLeft => KnobPart.RotateLeft,
            KnobAction.Press => KnobPart.Press,
            KnobAction.RotateRight => KnobPart.RotateRight,
            KnobAction.PressRotateLeft => KnobPart.PressRotateLeft,
            KnobAction.PressRotateRight => KnobPart.PressRotateRight,
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };
    }
}

/// <summary>What a control is called: "Key 3", "Knob 1 · Press".</summary>
public abstract record ControlTitle
{
    private ControlTitle() { }

    public sealed record Key(byte Number) : ControlTitle;

    public sealed record Knob(byte Number, KnobAction Action) : ControlTitle;
}

/// <summary>A key or knob action on the keyboard, as views hand it back to the view model.</summary>
public readonly record struct ControlRef
{
    internal ControlRef(ControlId id) => Id = id;

    internal ControlId Id { get; }

    public static ControlRef Key(byte number) => new(ControlId.Key(number));

    public static ControlRef Knob(byte number, KnobAction action) => new(ControlId.Knob(number, action.ToPart()));

    public ControlTitle Title() =>
        Id.IsKey ? new ControlTitle.Key(Id.Number) : new ControlTitle.Knob(Id.Number, Id.Part.ToKnobAction());

    public override string ToString() => Id.ToString();
}

/// <summary>What a key does, as a key cap and the inspector show it.</summary>
public abstract record ActionCaption
{
    private ActionCaption() { }

    /// <summary>The app doesn't know: the keyboard can't be read, or nobody has set it.</summary>
    public sealed record Unknown : ActionCaption;

    public sealed record Nothing : ActionCaption;

    /// <summary>"Win+C ↵".</summary>
    public sealed record Presses(string Text) : ActionCaption;

    public sealed record Media(MediaChoice Choice) : ActionCaption;

    /// <summary>A media key the app has no name for, by its consumer usage.</summary>
    public sealed record OtherMedia(ushort Usage) : ActionCaption;

    public sealed record Mouse(MouseChoice Choice) : ActionCaption;

    public bool IsAssigned => this is not (Unknown or Nothing);

    internal static ActionCaption Of(KeyAction? action) => action switch
    {
        null => new Unknown(),
        KeyAction.UnassignedAction => new Nothing(),
        KeyAction.Keys { Macro.Strokes.IsEmpty: true } => new Nothing(),
        KeyAction.Keys keys => new Presses(ActionText.Keys(keys.Macro)),
        KeyAction.Media media => media.Key.Choice() is { } choice ? new Media(choice) : new OtherMedia(media.Key.Usage),
        KeyAction.Mouse mouse => new Mouse(mouse.Action.Choice()),
        _ => new Unknown(),
    };
}
