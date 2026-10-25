namespace MINIKeyboard.Domain;

/// <summary>How a keyboard keeps waits.</summary>
public abstract record DelaySupport
{
    private DelaySupport() { }

    /// <summary>No waits.</summary>
    public static DelaySupport Unavailable { get; } = new None();

    public sealed record None : DelaySupport;

    /// <summary>A wait before each press, in milliseconds.</summary>
    public sealed record PerPress(InclusiveRange<uint> Range) : DelaySupport;

    /// <summary>One wait for the whole key, in milliseconds.</summary>
    public sealed record PerKey(InclusiveRange<uint> Range) : DelaySupport;
}

/// <summary>Whether the keyboard tells what it holds.</summary>
public enum ReadSupport
{
    Never,

    /// <summary>Asked as soon as it is connected, as the cross-platform app does.</summary>
    OnConnect,

    /// <summary>Asked only when the user asks.</summary>
    OnRequest,
}

/// <summary>What a connected keyboard can hold and do, from the commands it takes.</summary>
public sealed record KeyboardCapabilities(CommandSet Commands)
{
    private static readonly CommandSet StandardRecords = new CommandSet.Records(RecordFormat.Standard);

    /// <summary>The 8890 with report ID 0 takes no layer command; every other keyboard has three layers.</summary>
    public int LayerCount => Commands is CommandSet.Packets { ReportId: 0 } ? 1 : 3;

    /// <summary>How many presses one key holds — counted as <see cref="Presses"/> counts them.</summary>
    public int PressLimit => Commands switch
    {
        CommandSet.Packets => 5,
        CommandSet.Records { Format: RecordFormat.Standard } => 18,
        CommandSet.Records { Format: RecordFormat.Legacy } => 20,
        _ => 18,
    };

    /// <summary>Whether each modifier takes a place of its own, as in the standard format.</summary>
    public bool ModifiersTakePlaces => Commands == StandardRecords;

    /// <summary>The places <paramref name="macro"/> takes in a key.</summary>
    public int Presses(KeyMacro macro)
    {
        if (!ModifiersTakePlaces)
            return macro.Strokes.Count;
        return macro.Strokes.Sum(stroke => stroke.Modifiers.Count() + (stroke.IsModifiersOnly ? 0 : 1));
    }

    public bool Fits(KeyMacro macro) => macro.IsWellFormed && Presses(macro) <= PressLimit;

    /// <summary>
    /// Whether the keyboard is given <paramref name="key"/>, a usage on the HID keyboard page — the keys its vendor tool
    /// offers. The Windows tool for the 8890 has 95: A (0x04) to Keypad . (0x63) and Menu (0x65), without the two
    /// non-US keys (0x32, 0x64) or Keypad Enter (0x58). The cross-platform app has every usage from 0x04 to 0x65, and
    /// F13–F24 (0x68–0x73).
    /// </summary>
    public bool Takes(byte key) => Commands switch
    {
        CommandSet.Packets => key is >= 0x04 and <= 0x63 and not 0x32 and not 0x58 || key == 0x65,
        _ => key is >= 0x04 and <= 0x65 or >= 0x68 and <= 0x73,
    };

    /// <summary>Whether the keyboard is given every key <paramref name="macro"/> presses.</summary>
    public bool TakesKeysOf(KeyMacro macro) => macro.Strokes.All(stroke => stroke.IsModifiersOnly || Takes(stroke.Key));

    public DelaySupport Delays => Commands switch
    {
        CommandSet.Records { Format: RecordFormat.Standard } => new DelaySupport.PerPress(new(0, 60000)),
        CommandSet.Records { Format: RecordFormat.Legacy } => new DelaySupport.PerKey(new(0, 6000)),
        _ => DelaySupport.Unavailable,
    };

    /// <summary>Report ID 3 takes any consumer usage, but the Windows tool offers its six on every report ID.</summary>
    public EquatableArray<MediaKey> MediaKeys => Commands is CommandSet.Packets ? MediaKey.Basic : MediaKey.All;

    public LightingSupport Lighting => Commands switch
    {
        CommandSet.Packets => new LightingSupport.Modes(LightingSupport.K8890Modes),
        CommandSet.Records { Format: RecordFormat.Standard } =>
            new LightingSupport.PerKeyColor(new(0, 5), LightingSupport.ColorKeyCount),
        _ => new LightingSupport.ColorAndMode(new(0, 5)),
    };

    public ReadSupport Reading => Commands switch
    {
        CommandSet.Records { Format: RecordFormat.Standard } => ReadSupport.OnConnect,
        CommandSet.Records { Format: RecordFormat.Legacy } => ReadSupport.OnRequest,
        _ => ReadSupport.Never,
    };

    /// <summary>
    /// Keyboards no vendor tool draws are arranged by the user within these (<see cref="GridShape"/>); null for
    /// keyboards that take the cross-platform app's layouts.
    /// </summary>
    public GridLimits? GridLimits => Commands is CommandSet.Packets ? Domain.GridLimits.K8890 : null;

    /// <summary>
    /// Whether the keyboard can take <paramref name="control"/>. The 1189:8890 has twelve keys and two knobs that turn
    /// and press; a 884x keyboard stores every control of its layout.
    /// </summary>
    public bool CanAddress(ControlId control)
    {
        if (Commands is not CommandSet.Packets)
            return true;
        return control.IsKey
            ? control.Number is >= 1 and <= 12
            : control.Number is >= 1 and <= 2 && control.Part <= KnobPart.RotateRight;
    }

    public bool Addresses(KeyboardLayout layout) => layout.Controls.All(CanAddress);
}
