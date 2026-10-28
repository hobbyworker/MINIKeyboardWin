using System.ComponentModel;
using MINIKeyboard.Presentation;
using static MINIKeyboard.Views.Strings;

namespace MINIKeyboard.Views;

/// <summary>What the app calls things, in the user's language. The view models name nothing a user reads.</summary>
internal static class Names
{
    public static string ControlTitleText(ControlTitle title) => title switch
    {
        ControlTitle.Key key => T("Key %lld", key.Number),
        ControlTitle.Knob { Action: KnobAction.RotateLeft } knob => T("Knob %lld · Rotate left", knob.Number),
        ControlTitle.Knob { Action: KnobAction.Press } knob => T("Knob %lld · Press", knob.Number),
        ControlTitle.Knob { Action: KnobAction.RotateRight } knob => T("Knob %lld · Rotate right", knob.Number),
        ControlTitle.Knob { Action: KnobAction.PressRotateLeft } knob =>
            T("Knob %lld · Press and rotate left", knob.Number),
        ControlTitle.Knob knob => T("Knob %lld · Press and rotate right", knob.Number),
        _ => throw new ArgumentOutOfRangeException(nameof(title)),
    };

    public static string MediaTitle(MediaChoice media) => media switch
    {
        MediaChoice.PlayPause => T("Play/Pause"),
        MediaChoice.Stop => T("Stop"),
        MediaChoice.PreviousTrack => T("Prev Song"),
        MediaChoice.NextTrack => T("Next Song"),
        MediaChoice.Mute => T("Mute"),
        MediaChoice.VolumeUp => T("Volume+"),
        MediaChoice.VolumeDown => T("Volume-"),
        MediaChoice.BrightnessUp => T("Brightness+"),
        MediaChoice.BrightnessDown => T("Brightness-"),
        MediaChoice.BassUp => T("Bass+"),
        MediaChoice.BassDown => T("Bass-"),
        MediaChoice.TrebleUp => T("Treble+"),
        MediaChoice.TrebleDown => T("Treble-"),
        MediaChoice.MediaPlayer => T("Media Player"),
        MediaChoice.Email => T("Email"),
        MediaChoice.Calculator => T("Calculator"),
        MediaChoice.MyComputer => T("My Computer"),
        MediaChoice.WebHome => T("Web Home"),
        MediaChoice.WebBack => T("Web Back"),
        MediaChoice.WebForward => T("Web Forward"),
        MediaChoice.WebRefresh => T("Web Refresh"),
        _ => throw new ArgumentOutOfRangeException(nameof(media)),
    };

    public static string MediaGlyph(MediaChoice media) => media switch
    {
        MediaChoice.PlayPause => Glyphs.Play,
        MediaChoice.Stop => Glyphs.Stop,
        MediaChoice.PreviousTrack => Glyphs.Previous,
        MediaChoice.NextTrack => Glyphs.Next,
        MediaChoice.Mute => Glyphs.Mute,
        MediaChoice.VolumeUp => Glyphs.VolumeHigh,
        MediaChoice.VolumeDown => Glyphs.VolumeLow,
        MediaChoice.BrightnessUp or MediaChoice.BrightnessDown => Glyphs.Brightness,
        MediaChoice.BassUp or MediaChoice.BassDown or MediaChoice.TrebleUp or MediaChoice.TrebleDown => Glyphs.Speakers,
        MediaChoice.MediaPlayer => Glyphs.Music,
        MediaChoice.Email => Glyphs.Mail,
        MediaChoice.Calculator => Glyphs.Calculator,
        MediaChoice.MyComputer => Glyphs.Computer,
        MediaChoice.WebHome => Glyphs.Home,
        MediaChoice.WebBack => Glyphs.Back,
        MediaChoice.WebForward => Glyphs.Forward,
        MediaChoice.WebRefresh => Glyphs.Refresh,
        _ => throw new ArgumentOutOfRangeException(nameof(media)),
    };

    public static string MouseTitle(MouseChoice mouse) => mouse switch
    {
        MouseChoice.LeftClick => T("Mouse Left"),
        MouseChoice.MiddleClick => T("Mouse Centre"),
        MouseChoice.RightClick => T("Mouse Right"),
        MouseChoice.WheelUp => T("Mouse wheel Up"),
        MouseChoice.WheelDown => T("Mouse wheel Down"),
        MouseChoice.ControlWheelUp => T("Ctrl + Mouse wheel Up"),
        MouseChoice.ControlWheelDown => T("Ctrl + Mouse wheel Down"),
        MouseChoice.ShiftWheelUp => T("Shift + Mouse wheel Up"),
        MouseChoice.ShiftWheelDown => T("Shift + Mouse wheel Down"),
        MouseChoice.AltWheelUp => T("Alt + Mouse wheel Up"),
        MouseChoice.AltWheelDown => T("Alt + Mouse wheel Down"),
        _ => throw new ArgumentOutOfRangeException(nameof(mouse)),
    };

    public static string MouseGlyph(MouseChoice mouse) => mouse switch
    {
        MouseChoice.LeftClick or MouseChoice.MiddleClick or MouseChoice.RightClick => Glyphs.Mouse,
        MouseChoice.WheelUp or MouseChoice.ControlWheelUp or MouseChoice.ShiftWheelUp or MouseChoice.AltWheelUp =>
            Glyphs.Up,
        _ => Glyphs.Down,
    };

    /// <summary>A modifier's full name, for its tooltip; the cap shows the presentation's label ("Win").</summary>
    public static string ModifierTitle(ModifierKey modifier) => modifier switch
    {
        ModifierKey.Control => T("Control"),
        ModifierKey.Alt => T("Alt"),
        ModifierKey.Shift => T("Shift"),
        ModifierKey.Super => T("Windows"),
        ModifierKey.RightControl => T("Right Control"),
        ModifierKey.RightAlt => T("Right Alt"),
        ModifierKey.RightShift => T("Right Shift"),
        ModifierKey.RightSuper => T("Right Windows"),
        _ => throw new ArgumentOutOfRangeException(nameof(modifier)),
    };

    public static string LedColorTitle(LedColor color) => color switch
    {
        LedColor.Red => T("Red"),
        LedColor.Orange => T("Orange"),
        LedColor.Yellow => T("Yellow"),
        LedColor.Green => T("Green"),
        LedColor.Cyan => T("Cyan"),
        LedColor.Blue => T("Blue"),
        LedColor.Purple => T("Purple"),
        _ => throw new ArgumentOutOfRangeException(nameof(color)),
    };

    /// <summary>The colour a swatch shows.</summary>
    public static Windows.UI.Color LedSwatch(LedColor color) => color switch
    {
        LedColor.Red => Rgb(0xE0, 0x1B, 0x24),
        LedColor.Orange => Rgb(0xFF, 0x78, 0x00),
        LedColor.Yellow => Rgb(0xF6, 0xD3, 0x2D),
        LedColor.Green => Rgb(0x33, 0xD1, 0x7A),
        LedColor.Cyan => Rgb(0x33, 0xC7, 0xDE),
        LedColor.Blue => Rgb(0x35, 0x84, 0xE4),
        LedColor.Purple => Rgb(0x91, 0x41, 0xAC),
        _ => throw new ArgumentOutOfRangeException(nameof(color)),
    };

    private static Windows.UI.Color Rgb(byte red, byte green, byte blue) =>
        Windows.UI.Color.FromArgb(255, red, green, blue);

    public static string InterfaceRoleTitle(InterfaceRole role) => role switch
    {
        InterfaceRole.Keyboard => T("Keyboard"),
        InterfaceRole.Mouse => T("Mouse"),
        InterfaceRole.Media => T("Multimedia"),
        InterfaceRole.Configuration => T("Configuration Channel"),
        _ => T("Other"),
    };

    /// <summary>
    /// The 1189:8890's light modes, named for what they do on one with six keys and a knob. Other keyboards' modes
    /// have numbers only — what they do hasn't been seen.
    /// </summary>
    public static string LightingModeTitle(byte mode, bool k8890) => (k8890, mode) switch
    {
        (true, 0) => T("Off"),
        (true, 1) => T("Blink Last Key"),
        (true, 2) => T("Running Light"),
        _ => T("Mode %lld", mode),
    };

    public static string? LightingModeDetail(byte mode) => mode switch
    {
        1 => T("The last key pressed keeps blinking, in a new color with each press."),
        2 => T("A light runs across the keys in order, in a new color each round."),
        _ => null,
    };

    public static string LightingModeGlyph(byte mode) => mode switch
    {
        0 => Glyphs.Moon,
        1 => Glyphs.Keyboard,
        _ => Glyphs.Repeat,
    };

    public static string KeyGroupTitle(KeyGroupKind kind) => kind switch
    {
        KeyGroupKind.Letters => T("Letters"),
        KeyGroupKind.Numbers => T("Numbers"),
        KeyGroupKind.Editing => T("Editing"),
        KeyGroupKind.Symbols => T("Symbols"),
        KeyGroupKind.FunctionKeys => T("Function Keys"),
        KeyGroupKind.Navigation => T("Navigation"),
        KeyGroupKind.Numpad => T("Numpad"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static string TabTitle(EditorTab tab) => tab switch
    {
        EditorTab.Keys => T("Keys"),
        EditorTab.Media => T("Multimedia"),
        EditorTab.Mouse => T("Mouse"),
        EditorTab.Lighting => T("LED"),
        EditorTab.Delay => T("Delay"),
        _ => throw new ArgumentOutOfRangeException(nameof(tab)),
    };

    public static string NoticeMessage(Notice notice) => notice switch
    {
        Notice.Saved { Simulated: true } => T("Saved to the simulator — nothing was written to a keyboard."),
        Notice.Saved => T("Saved to the keyboard."),
        Notice.SavedButDiffers =>
            T("Saved, but some keys or knobs read back differently. Those changes stay unsaved."),
        Notice.SavedUnchecked => T("Saved, but the keyboard couldn’t be read back to check."),
        Notice.ReadComplete => T("Read from the keyboard."),
        Notice.ReadIncomplete => T("Some keys or knobs couldn’t be read. They’re shown as before."),
        Notice.NothingToSave => T("Nothing to save — no key has changed."),
        Notice.IdentifyFailed => T("The keyboard couldn’t be asked what it is."),
        Notice.ReadFailed => T("Reading from the keyboard failed."),
        Notice.WriteFailed failed => T("Writing to the keyboard failed (%@).", ErrorCode(failed.Code)),
        Notice.KeyboardGone => T("The keyboard is no longer connected."),
        Notice.Full full => T("A key holds up to %lld presses.", full.Limit),
        Notice.SelectControlFirst => T("Select a key or knob."),
        Notice.ModifiersOnlyAlone =>
            T("Modifiers alone make the key a modifier key, so they can’t go with other presses."),
        Notice.NeedsColorKey needs => T("Only keys 1–%lld have a color. Select one of them.", needs.Keys),
        Notice.UntestedFirmware untested =>
            T("This keyboard’s firmware (type %lld) hasn’t been tested yet. If keys read or save wrong, please report it with the device log.",
                untested.Kind),
        _ => T("This keyboard can’t do that."),
    };

    /// <summary>What a key cap shows: "Win+C", "Mute".</summary>
    public static string CaptionText(ActionCaption caption) => caption switch
    {
        ActionCaption.Presses presses => presses.Text,
        ActionCaption.Media media => MediaTitle(media.Choice),
        ActionCaption.OtherMedia other => T("Media Key %@", $"0x{other.Usage:X3}"),
        ActionCaption.Mouse mouse => MouseTitle(mouse.Choice),
        _ => "",
    };

    /// <summary>
    /// What the key is called, for its tooltip. A key whose cap reads plainly — a letter, a digit, F5 — goes by its
    /// label.
    /// </summary>
    public static string KeyName(KeyUsage key) => key.Usage switch
    {
        0x28 => T("Enter"),
        0x29 => T("Escape"),
        0x2A => T("Backspace"),
        0x2B => T("Tab"),
        0x2C => T("Space"),
        0x39 => T("Caps Lock"),
        0x65 => T("Menu Key"),
        0x46 => T("Print Screen"),
        0x47 => T("Scroll Lock"),
        0x49 => T("Insert"),
        0x4A => T("Home"),
        0x4B => T("Page Up"),
        0x4C => T("Delete"),
        0x4D => T("End"),
        0x4E => T("Page Down"),
        0x4F => T("Right Arrow"),
        0x50 => T("Left Arrow"),
        0x51 => T("Down Arrow"),
        0x52 => T("Up Arrow"),
        0x53 => T("Num Lock"),
        0x58 => T("Keypad Enter"),
        // The keypad's operators as their keys are usually named, rather than as their caps show them.
        0x54 => T("Keypad %@", "/"),
        0x55 => T("Keypad %@", "*"),
        0x56 => T("Keypad %@", "-"),
        0x57 => T("Keypad %@", "+"),
        >= 0x59 and <= 0x63 => T("Keypad %@", key.Label),
        0x32 or 0x64 => T("Non-US Key %@ %@", key.Label, key.Shifted ?? ""),
        _ => key.Label,
    };

    public static string LogEventMessage(LogEvent logEvent) => logEvent switch
    {
        LogEvent.Connected connected => T("Connected to %@, interface %lld", connected.Usb, connected.Interface),
        LogEvent.NeedsPermission => T("Access to the keyboard is needed"),
        LogEvent.OpenFailed failed => T("The keyboard couldn’t be opened (%@).", ErrorCode(failed.Code)),
        LogEvent.WriteFailed failed => T("Writing to the keyboard failed (%@).", ErrorCode(failed.Code)),
        LogEvent.Simulated simulated => T("Simulated keyboard: %@", simulated.Id),
        _ => T("Looking for the keyboard"),
    };

    /// <summary>
    /// A failure's Win32 error code, with the system's words for it: "Win32 error 5: Access is denied.".
    /// </summary>
    public static string ErrorCode(int code)
    {
        var message = new Win32Exception(code).Message.Trim();
        return string.IsNullOrEmpty(message) ? $"Win32 error {code}" : $"Win32 error {code}: {message}";
    }

    public static string SimulatorKindTitle(SimulatorKind kind) => kind switch
    {
        SimulatorKind.K8890 k8890 => T("Report ID %lld", k8890.ReportId),
        SimulatorKind.Identified identified => T(
            "%@ · %@",
            T("Keys: %lld · Knobs: %lld · Type %lld", identified.Keys, identified.Knobs, identified.Kind),
            identified.Usb),
        SimulatorKind.Unidentified unidentified =>
            T("%@ · %@", CommandsText(new InfoCommands.Unidentified()), unidentified.Usb),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>"Interface 1 · HID · Report ID 3 · 65 bytes".</summary>
    public static string ChannelText(InfoChannel channel) => T(
        "Interface %lld · %@ · %@ · %@",
        channel.Interface,
        channel.Path == ChannelPath.Simulated ? T("Simulator") : "HID",
        T("Report ID %lld", channel.ReportId),
        T("%lld bytes", channel.ReportLength));

    public static string CommandsText(InfoCommands commands) => commands switch
    {
        InfoCommands.Packets packets => T("8890 Packets · %@", T("Report ID %lld", packets.ReportId)),
        InfoCommands.Standard => T("884x · Standard Firmware"),
        InfoCommands.Legacy => T("884x · Legacy Firmware"),
        InfoCommands.Unidentified => T("884x · Doesn’t Answer Identification"),
        _ => "—",
    };
}
