using MINIKeyboard.Domain;

namespace MINIKeyboard.Presentation;

// What each part of the window draws, in the presentation's own types.

/// <summary>The keyboard as the main window draws it: its body and every control, turned the way it sits.</summary>
public sealed record KeyboardDrawing(LayoutRect Frame, LayoutRect Bounds, EquatableList<ControlDrawing> Controls);

public abstract record ControlFace
{
    private ControlFace() { }

    public sealed record Key(byte Number) : ControlFace;

    public sealed record Knob(KnobAction Action) : ControlFace;
}

/// <param name="LedColor">The key's colour, on the LED page of a keyboard that lights each key.</param>
public sealed record ControlDrawing(
    ControlRef Control,
    ControlFace Face,
    LayoutRect Rect,
    ActionCaption Caption,
    bool IsSelected,
    bool IsChanged,
    Rgb? LedColor);

/// <summary>A keyboard's shape drawn small, for choosing one: keys, and each knob as one body.</summary>
public sealed record ShapeDrawing(LayoutRect Frame, LayoutRect Bounds, EquatableList<ShapeItem> Items)
{
    internal static ShapeDrawing Of(LayoutGeometry geometry)
    {
        var keys = geometry.Rects
            .Where(pair => pair.Key.IsKey)
            .OrderBy(pair => pair.Key.Number)
            .Select(pair => new ShapeItem(pair.Value, false));
        var knobs = geometry.KnobOutlines().Select(knob => new ShapeItem(knob.Rect, true));
        return new ShapeDrawing(geometry.Frame, geometry.Bounds(), [.. keys, .. knobs]);
    }
}

public readonly record struct ShapeItem(LayoutRect Rect, bool IsKnob);

/// <summary>The header's account of the keyboard.</summary>
public abstract record ConnectionStatus
{
    private ConnectionStatus() { }

    public sealed record Searching : ConnectionStatus;

    public sealed record NeedsPermission : ConnectionStatus;

    public sealed record OpenFailed(int Code) : ConnectionStatus;

    /// <param name="Keys">What the keyboard said of its keys, when it said.</param>
    /// <param name="Knobs">What the keyboard said of its knobs, when it said.</param>
    public sealed record Connected(string Usb, byte? Keys, byte? Knobs, bool IsIdentified, bool IsSimulated)
        : ConnectionStatus;
}

/// <summary>The inspector's account of the selected control.</summary>
/// <param name="LayerNumber">Shown when the keyboard has layers.</param>
public sealed record SelectionState(ControlTitle Title, int? LayerNumber, BindingState Binding);

public abstract record BindingState
{
    private BindingState() { }

    public sealed record Unknown : BindingState;

    public sealed record Nothing : BindingState;

    /// <summary>
    /// Each press as text, the wait for the whole key, and how many places the presses take of the limit.
    /// </summary>
    public sealed record Presses(EquatableList<string> Strokes, uint KeyDelay, int Used, int Limit) : BindingState;

    public sealed record Media(MediaChoice Choice) : BindingState;

    public sealed record OtherMedia(ushort Usage) : BindingState;

    public sealed record Mouse(MouseChoice Choice) : BindingState;
}

/// <summary>How the Keys page lays out the keys.</summary>
public enum KeyArrangement
{
    /// <summary>As on a full-size keyboard — the cross-platform app's page.</summary>
    Qwerty,

    /// <summary>In groups, the letters in alphabetical order — the Windows tool's page.</summary>
    Alphabetical,
}

public abstract record BoardFace
{
    private BoardFace() { }

    public sealed record Key(KeyUsage Usage) : BoardFace;

    /// <summary>Held for the next key, as in the modifier row of the other arrangement.</summary>
    public sealed record Modifier(ModifierKey ModifierKey) : BoardFace;
}

/// <summary>One cap of the full-size keyboard, placed in key widths.</summary>
/// <param name="Column">From the left edge.</param>
/// <param name="IsEnabled">Off for a key the keyboard isn't given; it stays in its place.</param>
public readonly record struct BoardCap(BoardFace Face, int Row, double Column, double Width, bool IsEnabled)
{
    /// <summary>A row's width, in key widths.</summary>
    public const double RowWidth = FullKeyboard.Width;
}

public enum KeyGroupKind
{
    Letters,
    Numbers,
    Editing,
    Symbols,
    FunctionKeys,
    Navigation,
    Numpad,
}

public static class KeyGroupKinds
{
    public static IReadOnlyList<KeyGroupKind> All { get; } = [.. Enum.GetValues<KeyGroupKind>()];

    extension(KeyGroupKind kind)
    {
        internal IReadOnlyList<KeyUsage> Keys() => kind switch
        {
            KeyGroupKind.Letters => KeyUsage.Letters,
            KeyGroupKind.Numbers => KeyUsage.Digits,
            KeyGroupKind.Editing => KeyUsage.Editing,
            KeyGroupKind.Symbols => [.. KeyUsage.Symbols, .. KeyUsage.International],
            KeyGroupKind.FunctionKeys => [.. KeyUsage.FunctionKeys, .. KeyUsage.ExtraFunctionKeys],
            KeyGroupKind.Navigation => [.. KeyUsage.SystemKeys, .. KeyUsage.Navigation],
            KeyGroupKind.Numpad => KeyUsage.Keypad,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}

/// <summary>A group of keys in the alphabetical arrangement, with only the keys the keyboard is given.</summary>
public sealed record KeyGroup(KeyGroupKind Kind, EquatableList<KeyUsage> Keys);

/// <summary>The LED page, in the form the keyboard takes.</summary>
public abstract record LightingPage
{
    private LightingPage() { }

    /// <summary>The 1189:8890's modes.</summary>
    public sealed record ModesOnly(EquatableList<byte> Modes, byte? Active) : LightingPage;

    public sealed record ColorAndMode(
        EquatableList<LedColor> Colors,
        EquatableList<byte> Modes,
        LedColor? ActiveColor,
        byte? ActiveMode) : LightingPage;

    public sealed record PerKeyColor(EquatableList<byte> Modes, byte? ActiveMode, int Keys, KeyColorSelection? Selected)
        : LightingPage;
}

public readonly record struct KeyColorSelection(ControlTitle Title, Rgb Color);

/// <summary>The waits a keyboard keeps, in milliseconds, both ends included.</summary>
public readonly record struct DelayRange(uint Min, uint Max);

/// <summary>The Delay page.</summary>
public abstract record DelayPage
{
    private DelayPage() { }

    public sealed record Unavailable : DelayPage;

    /// <summary>The selected key's presses, each with its wait; empty when it has none.</summary>
    public sealed record PerPress(DelayRange Range, EquatableList<PressDelay> Presses) : DelayPage;

    /// <summary>The selected key's one wait; null when no key is selected.</summary>
    public sealed record PerKey(DelayRange Range, uint? Value) : DelayPage;
}

public sealed record PressDelay(int Index, string Text, uint Delay);

/// <summary>The layout sheet.</summary>
/// <param name="ReportedKeys">What the keyboard said of its keys, when it said.</param>
/// <param name="ReportedKnobs">What the keyboard said of its knobs, when it said.</param>
/// <param name="QuarterTurns">Quarter turns clockwise, 0–3.</param>
/// <param name="IsRequired">The first time: no Cancel.</param>
public sealed record LayoutSheetState(
    string Usb,
    byte? ReportedKeys,
    byte? ReportedKnobs,
    LayoutSheetMode Mode,
    byte QuarterTurns,
    bool IsRequired);

public abstract record LayoutSheetMode
{
    private LayoutSheetMode() { }

    /// <summary>The keyboard said its shape: it only turns.</summary>
    public sealed record Fixed : LayoutSheetMode;

    /// <summary>One of the cross-platform app's layouts.</summary>
    public sealed record Catalog(EquatableList<LayoutChoice> Choices, string SelectedId) : LayoutSheetMode;

    /// <summary>Keys, keys to a row and knobs, within the limits.</summary>
    public sealed record Arrangement(byte Keys, byte Columns, byte Knobs, byte MaxKeys, byte MaxKnobs)
        : LayoutSheetMode;
}

public sealed record LayoutChoice(string Id, int Keys, int Knobs, ShapeDrawing Drawing);

/// <summary>
/// What is known about the keyboard plugged in — from what the system says of the USB device and, on keyboards that
/// answer, from the answer. Nothing is guessed: what the keyboard doesn't say is null.
/// </summary>
/// <param name="Usb">"USB 1.1 · 12 Mb/s", or as much of it as the system tells.</param>
/// <param name="Power">mA.</param>
public sealed record KeyboardInfo(
    string UsbId,
    string? Name,
    string? Manufacturer,
    string? SerialNumber,
    Firmware Firmware,
    string? Usb,
    uint? Power,
    EquatableList<InfoInterface> Interfaces,
    InfoChannel Channel,
    InfoCommands Commands)
{
    internal static KeyboardInfo Of(ConnectedKeyboard keyboard, KeyboardIdentification? identification)
    {
        var details = keyboard.Details;
        Firmware firmware;
        if (identification?.Identity is { } identity)
            firmware = new Firmware.Reported(identity.Kind, identity.KeyCount, identity.KnobCount, identity.IsKnownKind);
        else if (details is { DeviceRelease: var release } && release != 0)
            firmware = new Firmware.Release($"{release >> 8:X}.{release & 0xFF:X2}");
        else
            firmware = new Firmware.Unknown();
        var interfaces = (details?.Interfaces ?? [])
            .Select(face => new InfoInterface(
                face.Number, Roles(face, keyboard.InterfaceNumber), face.Number == keyboard.InterfaceNumber));
        var path = keyboard.Channel == Domain.Channel.Simulated ? ChannelPath.Simulated : ChannelPath.Hid;
        InfoCommands commands = identification?.Commands switch
        {
            CommandSet.Packets packets => new InfoCommands.Packets(packets.ReportId),
            CommandSet.Records { Format: RecordFormat.Standard } => new InfoCommands.Standard(),
            CommandSet.Records { Format: RecordFormat.Legacy } => new InfoCommands.Legacy(),
            CommandSet.Records { Format: RecordFormat.Unidentified } => new InfoCommands.Unidentified(),
            _ => new InfoCommands.Unknown(),
        };
        return new KeyboardInfo(
            keyboard.Usb.ToString(),
            details?.ProductName,
            details?.Manufacturer,
            details?.SerialNumber,
            firmware,
            details is null ? null : UsbText(details),
            details?.MaxPower,
            [.. interfaces],
            new InfoChannel(keyboard.InterfaceNumber, path, keyboard.ReportId, keyboard.ReportLength),
            commands);
    }

    /// <summary>
    /// What an interface is for, by the usages of its HID device, or by its descriptor when the system made no HID
    /// device of it.
    /// </summary>
    internal static EquatableList<InterfaceRole> Roles(DeviceInterface face, byte configuration)
    {
        var roles = new List<InterfaceRole>();
        foreach (var pair in face.Usages)
        {
            var role = (pair.Page, pair.Usage) switch
            {
                (0x01, 0x06) => InterfaceRole.Keyboard,
                (0x01, 0x01 or 0x02) => InterfaceRole.Mouse,
                (0x0C, _) => InterfaceRole.Media,
                ( >= 0xFF00, _) => InterfaceRole.Configuration,
                _ => InterfaceRole.Other,
            };
            if (!roles.Contains(role))
                roles.Add(role);
        }
        if (roles.Count == 0 && face.Number == configuration)
            roles.Add(InterfaceRole.Configuration);
        if (roles.Count == 0)
        {
            roles.Add((face.Class, face.Subclass, face.Protocol) switch
            {
                (3, 1, 1) => InterfaceRole.Keyboard,
                (3, 1, 2) => InterfaceRole.Mouse,
                _ => InterfaceRole.Other,
            });
        }
        return [.. roles];
    }

    /// <summary>
    /// "USB 1.1 · 12 Mb/s" — the version and the speed, each when the system tells; null when it tells neither.
    /// </summary>
    internal static string? UsbText(DeviceDetails details)
    {
        var parts = new List<string>();
        if (details.UsbVersion is { } version)
        {
            var (major, minor, sub) = ((version >> 8) & 0xFF, (version >> 4) & 0x0F, version & 0x0F);
            parts.Add(sub != 0 ? $"USB {major:X}.{minor:X}.{sub:X}" : $"USB {major:X}.{minor:X}");
        }
        if (details.Speed is { } speed)
        {
            parts.Add(speed switch
            {
                >= 1_000_000_000 => $"{speed / 1_000_000_000} Gb/s",
                >= 1_000_000 => $"{speed / 1_000_000} Mb/s",
                _ => $"{speed / 1000} kb/s",
            });
        }
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }
}

public abstract record Firmware
{
    private Firmware() { }

    /// <summary>What a 884x keyboard answered: the type, keys and knobs.</summary>
    /// <param name="Tested">Whether the type is one the app was tried with.</param>
    public sealed record Reported(byte Kind, byte Keys, byte Knobs, bool Tested) : Firmware;

    /// <summary><c>bcdDevice</c>, "1.02".</summary>
    public sealed record Release(string Version) : Firmware;

    public sealed record Unknown : Firmware;
}

/// <param name="IsConfiguration">The interface the app writes to.</param>
public sealed record InfoInterface(byte Number, EquatableList<InterfaceRole> Roles, bool IsConfiguration);

/// <summary>The interface the app writes to, and how.</summary>
/// <param name="ReportLength">Bytes, the report ID included.</param>
public readonly record struct InfoChannel(byte Interface, ChannelPath Path, byte ReportId, int ReportLength);

public enum ChannelPath
{
    /// <summary>Through the HID device the system made of the interface.</summary>
    Hid,

    Simulated,
}

/// <summary>The commands the keyboard takes.</summary>
public abstract record InfoCommands
{
    private InfoCommands() { }

    /// <summary>The 1189:8890's packets.</summary>
    public sealed record Packets(byte ReportId) : InfoCommands;

    /// <summary>A 884x keyboard's records on the standard firmware.</summary>
    public sealed record Standard : InfoCommands;

    /// <summary>A 884x keyboard's records on the legacy firmware.</summary>
    public sealed record Legacy : InfoCommands;

    /// <summary>A 884x keyboard's records, on one that doesn't answer.</summary>
    public sealed record Unidentified : InfoCommands;

    /// <summary>Not known yet.</summary>
    public sealed record Unknown : InfoCommands;
}

public enum InterfaceRole
{
    Keyboard,
    Mouse,
    Media,
    Configuration,
    Other,
}

public enum SimulatorGroup
{
    K8890,
    Standard,
    Legacy,
    Unidentified,
}

public static class SimulatorGroups
{
    public static IReadOnlyList<SimulatorGroup> All { get; } = [.. Enum.GetValues<SimulatorGroup>()];
}

/// <summary>What the simulated keyboard is; the menu names it.</summary>
public abstract record SimulatorKind
{
    private SimulatorKind() { }

    public sealed record K8890(byte ReportId) : SimulatorKind;

    /// <summary>It answers with these, on this USB ID.</summary>
    public sealed record Identified(byte Keys, byte Knobs, byte Kind, string Usb) : SimulatorKind;

    /// <summary>It doesn't answer.</summary>
    public sealed record Unidentified(string Usb) : SimulatorKind;
}

/// <summary>One of the keyboards plugged in, to choose which the app opens.</summary>
/// <param name="Title">Its USB ID, numbered by port when several have the same one.</param>
public sealed record KeyboardChoice(string Id, string Title, bool IsSelected);

/// <summary>A keyboard the simulator can play.</summary>
public sealed record SimulatorChoice(string Id, SimulatorGroup Group, SimulatorKind Kind)
{
    /// <summary>
    /// Whether everyone's menu has it, not only a debug build's: the 8890 as it ships (report ID 3), each 884x layout on
    /// its standard records, and a 884x that doesn't answer. The 8890's other report IDs and the legacy records are for
    /// testing the app.
    /// </summary>
    public bool IsForEveryone => Group switch
    {
        SimulatorGroup.K8890 => Kind is SimulatorKind.K8890 { ReportId: 3 },
        SimulatorGroup.Legacy => false,
        _ => true,
    };

    internal static SimulatorChoice Of(SimulatedModel model)
    {
        var usb = model.Usb.ToString();
        return model.Kind switch
        {
            SimulatedKind.K8890 k8890 =>
                new SimulatorChoice(model.Id, SimulatorGroup.K8890, new SimulatorKind.K8890(k8890.ReportId)),
            SimulatedKind.Identified { Identity: var identity } => new SimulatorChoice(
                model.Id,
                identity.RecordFormat == RecordFormat.Standard ? SimulatorGroup.Standard : SimulatorGroup.Legacy,
                new SimulatorKind.Identified(identity.KeyCount, identity.KnobCount, identity.Kind, usb)),
            _ => new SimulatorChoice(model.Id, SimulatorGroup.Unidentified, new SimulatorKind.Unidentified(usb)),
        };
    }
}
