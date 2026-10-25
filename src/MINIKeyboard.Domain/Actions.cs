using System.Numerics;

namespace MINIKeyboard.Domain;

/// <summary>
/// The modifier keys of a key press, as the bits of a HID keyboard report (usages 0xE0–0xE7). The vendor tools
/// call Super "Win".
/// </summary>
[Flags]
public enum Modifiers : byte
{
    None = 0,
    Control = 0x01,
    Shift = 0x02,
    Alt = 0x04,
    Super = 0x08,
    RightControl = 0x10,
    RightShift = 0x20,
    RightAlt = 0x40,
    RightSuper = 0x80,
}

public static class ModifiersExtensions
{
    /// <summary>Each modifier on its own, lowest bit first.</summary>
    public static IEnumerable<Modifiers> Each() => Enumerable.Range(0, 8).Select(bit => (Modifiers)(1 << bit));

    extension(Modifiers modifiers)
    {
        /// <summary>The modifiers set here, lowest bit first.</summary>
        public IEnumerable<Modifiers> Members() => Each().Where(modifier => modifiers.HasFlag(modifier));

        public int Count() => BitOperations.PopCount((uint)modifiers);
    }
}

/// <summary>
/// One press: modifiers held while a key goes down. A key of 0 holds the modifiers alone, which turns the keyboard
/// key into a modifier key — so such a press is only ever a key's whole action.
/// </summary>
/// <param name="Key">The key's usage on the HID keyboard page, or 0 for none.</param>
/// <param name="Delay">Milliseconds to wait before this press. Only standard-format keyboards keep it.</param>
public readonly record struct KeyStroke(Modifiers Modifiers, byte Key, uint Delay = 0)
{
    public static KeyStroke OfKey(byte key) => new(Modifiers.None, key);

    public KeyStroke WithDelay(uint delay) => this with { Delay = delay };

    public bool IsModifiersOnly => Key == 0;
}

/// <summary>Key presses one keyboard key sends, in order.</summary>
/// <param name="Delay">Milliseconds to wait, for the whole key. Only legacy-format keyboards keep it.</param>
public sealed record KeyMacro(EquatableArray<KeyStroke> Strokes, uint Delay = 0)
{
    public static KeyMacro Empty { get; } = new([]);

    /// <summary>One modifiers-only press, or nothing at all, but never one among others.</summary>
    public bool IsWellFormed => Strokes.Count <= 1 || !Strokes.Any(stroke => stroke.IsModifiersOnly);
}

/// <summary>What a keyboard key or knob action does.</summary>
public abstract record KeyAction
{
    private KeyAction() { }

    /// <summary>Nothing: the key is cleared.</summary>
    public static KeyAction Unassigned { get; } = new UnassignedAction();

    public sealed record UnassignedAction : KeyAction;

    public sealed record Keys(KeyMacro Macro) : KeyAction;

    public sealed record Media(MediaKey Key) : KeyAction;

    public sealed record Mouse(MouseAction Action) : KeyAction;
}

/// <summary>A key on the HID consumer page — media and application keys.</summary>
/// <param name="Usage">The consumer-page usage.</param>
public readonly record struct MediaKey(ushort Usage)
{
    public static readonly MediaKey PlayPause = new(0xCD);
    public static readonly MediaKey Stop = new(0xB7);
    public static readonly MediaKey PreviousTrack = new(0xB6);
    public static readonly MediaKey NextTrack = new(0xB5);
    public static readonly MediaKey Mute = new(0xE2);
    public static readonly MediaKey VolumeUp = new(0xE9);
    public static readonly MediaKey VolumeDown = new(0xEA);
    public static readonly MediaKey BrightnessUp = new(0x6F);
    public static readonly MediaKey BrightnessDown = new(0x70);
    public static readonly MediaKey BassUp = new(0x152);
    public static readonly MediaKey BassDown = new(0x153);
    public static readonly MediaKey TrebleUp = new(0x154);
    public static readonly MediaKey TrebleDown = new(0x155);
    public static readonly MediaKey MediaPlayer = new(0x183);
    public static readonly MediaKey Email = new(0x18A);
    public static readonly MediaKey Calculator = new(0x192);
    public static readonly MediaKey MyComputer = new(0x194);
    public static readonly MediaKey WebHome = new(0x223);
    public static readonly MediaKey WebBack = new(0x224);
    public static readonly MediaKey WebForward = new(0x225);
    public static readonly MediaKey WebRefresh = new(0x227);

    /// <summary>The cross-platform app's media buttons, in its order.</summary>
    public static readonly EquatableArray<MediaKey> All =
    [
        PlayPause, Stop, PreviousTrack, NextTrack, MyComputer, BrightnessUp, BrightnessDown, MediaPlayer, Mute,
        VolumeUp, VolumeDown, Calculator, WebHome, Email, BassUp, BassDown, TrebleUp, TrebleDown, WebRefresh,
        WebForward, WebBack,
    ];

    /// <summary>The Windows tool's six.</summary>
    public static readonly EquatableArray<MediaKey> Basic = [PlayPause, PreviousTrack, NextTrack, Mute, VolumeUp, VolumeDown];
}

/// <summary>The mouse buttons and wheel moves both vendor tools offer.</summary>
public enum MouseAction
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

public static class MouseActions
{
    public static readonly EquatableArray<MouseAction> All =
    [
        MouseAction.LeftClick, MouseAction.MiddleClick, MouseAction.RightClick, MouseAction.WheelUp,
        MouseAction.WheelDown, MouseAction.ControlWheelUp, MouseAction.ControlWheelDown, MouseAction.ShiftWheelUp,
        MouseAction.ShiftWheelDown, MouseAction.AltWheelUp, MouseAction.AltWheelDown,
    ];

    extension(MouseAction action)
    {
        /// <summary>HID mouse button bits.</summary>
        public byte Buttons() => action switch
        {
            MouseAction.LeftClick => 0x01,
            MouseAction.RightClick => 0x02,
            MouseAction.MiddleClick => 0x04,
            _ => 0,
        };

        /// <summary>One notch up (0x01) or down (0xFF), or 0.</summary>
        public byte Wheel() => action switch
        {
            MouseAction.WheelUp or MouseAction.ControlWheelUp or MouseAction.ShiftWheelUp or MouseAction.AltWheelUp
                => 0x01,
            MouseAction.WheelDown or MouseAction.ControlWheelDown or MouseAction.ShiftWheelDown
                or MouseAction.AltWheelDown => 0xFF,
            _ => 0,
        };

        /// <summary>The modifier held while the wheel turns.</summary>
        public Modifiers Modifiers() => action switch
        {
            MouseAction.ControlWheelUp or MouseAction.ControlWheelDown => Domain.Modifiers.Control,
            MouseAction.ShiftWheelUp or MouseAction.ShiftWheelDown => Domain.Modifiers.Shift,
            MouseAction.AltWheelUp or MouseAction.AltWheelDown => Domain.Modifiers.Alt,
            _ => Domain.Modifiers.None,
        };
    }

    /// <summary>The action with these buttons, or this wheel move and modifier; null when there is none.</summary>
    public static MouseAction? FromParts(byte buttons, byte wheel, Modifiers modifiers)
    {
        if (buttons != 0)
            return All.Cast<MouseAction?>().FirstOrDefault(action => action!.Value.Buttons() == buttons);
        if (wheel != 0)
        {
            return All.Cast<MouseAction?>().FirstOrDefault(action =>
                action!.Value.Buttons() == 0 && action.Value.Wheel() == wheel && action.Value.Modifiers() == modifiers);
        }
        return null;
    }
}
