namespace MINIKeyboard.Domain;

/// <summary>The layout a user chose for a keyboard that doesn't say its shape, and how the keyboard is turned.</summary>
public sealed record LayoutProfile(string LayoutId, Rotation Rotation);

/// <summary>Where the layout drawn for the connected keyboard came from.</summary>
public enum Source
{
    /// <summary>The keyboard said how many keys and knobs it has.</summary>
    Identified,

    /// <summary>The user chose it.</summary>
    Chosen,

    /// <summary>Nobody has chosen one yet: what the keyboard's vendor tool draws for a keyboard it knows nothing of.</summary>
    Assumed,
}

/// <summary>The layout the app draws for the connected keyboard, and where it came from.</summary>
/// <param name="Choices">
/// The cross-platform app's layouts the user may choose from; empty when the keyboard said its shape, or is arranged
/// instead.
/// </param>
/// <param name="Grid">
/// For a keyboard no vendor tool draws: how large an arrangement (<see cref="GridShape"/>) the user may give it.
/// </param>
public sealed record ResolvedLayout(
    KeyboardLayout Layout,
    Rotation Rotation,
    Source Source,
    EquatableArray<KeyboardLayout> Choices,
    GridLimits? Grid)
{
    public bool NeedsChoice => Source == Source.Assumed;
}

/// <summary>What one line of the device log holds.</summary>
public abstract record LogContent
{
    private LogContent() { }

    /// <summary>A report written to the keyboard, its report ID first unless it is 0.</summary>
    public sealed record Output(EquatableArray<byte> Bytes) : LogContent;

    /// <summary>A report the keyboard sent.</summary>
    public sealed record Input(EquatableArray<byte> Bytes) : LogContent;

    public sealed record Event(DeviceLogEvent What) : LogContent;
}

/// <summary>One line of the device log: a report written or read — every byte of it — or something that happened.</summary>
/// <param name="Id">Counts up from 1 as lines are added.</param>
public sealed record DeviceLogEntry(ulong Id, DateTimeOffset Time, LogContent Content);

/// <summary>What happened to the connection, for the device log.</summary>
public abstract record DeviceLogEvent
{
    private DeviceLogEvent() { }

    public sealed record Searching : DeviceLogEvent;

    public sealed record Connected(UsbIdentity Usb, byte Interface) : DeviceLogEvent;

    public sealed record NeedsPermission : DeviceLogEvent;

    public sealed record OpenFailed(int Code) : DeviceLogEvent;

    public sealed record WriteFailed(int Code) : DeviceLogEvent;

    /// <summary>A simulated keyboard took the place of the one on USB.</summary>
    public sealed record Simulated(string ModelId) : DeviceLogEvent;
}

/// <summary>Which keyboard the app talks to: the one on USB, or one that exists only in the app.</summary>
public abstract record KeyboardSource
{
    private KeyboardSource() { }

    public static KeyboardSource Hardware { get; } = new HardwareSource();

    public sealed record HardwareSource : KeyboardSource;

    public sealed record Simulated(SimulatedModel Model) : KeyboardSource;

    public bool IsSimulated => this is Simulated;
}

/// <summary>What a simulated keyboard is.</summary>
public abstract record SimulatedKind
{
    private SimulatedKind() { }

    /// <summary>A 1189:8890 whose descriptor declares this report ID.</summary>
    public sealed record K8890(byte ReportId) : SimulatedKind;

    /// <summary>A 884x keyboard that answers identification.</summary>
    public sealed record Identified(KeyboardIdentity Identity) : SimulatedKind;

    /// <summary>A 884x keyboard that doesn't.</summary>
    public sealed record Unidentified : SimulatedKind;
}

/// <summary>
/// A keyboard the simulator plays: its USB ID, the commands it takes and, for a 884x keyboard that answers, what it
/// says about itself.
/// </summary>
public sealed record SimulatedModel(UsbIdentity Usb, SimulatedKind Kind)
{
    public string Id => Kind switch
    {
        SimulatedKind.K8890 k8890 => $"8890-{k8890.ReportId}",
        SimulatedKind.Identified { Identity: var identity } =>
            $"{Usb}-{identity.KeyCount}-{identity.KnobCount}-{identity.Kind}",
        _ => $"{Usb}-unidentified",
    };

    public KeyboardFamily Family => KeyboardFamilies.Of(Usb) ?? KeyboardFamily.K884x;

    /// <summary>
    /// The 8890 on each report ID, then every layout a 884x keyboard can be identified as on each format, then a 884x
    /// keyboard that doesn't answer.
    /// </summary>
    public static IReadOnlyList<SimulatedModel> All()
    {
        var models = new List<SimulatedModel>();
        foreach (byte reportId in new byte[] { 3, 0, 2 })
            models.Add(new SimulatedModel(KeyboardFamilies.K8890Usb, new SimulatedKind.K8890(reportId)));
        foreach (var format in new[] { RecordFormat.Standard, RecordFormat.Legacy })
        {
            models.AddRange(KeyboardLayout.All.Select(layout => IdentifiedAs(layout, format)).OfType<SimulatedModel>());
        }
        models.Add(new SimulatedModel(new UsbIdentity(0x1189, 0x8840), new SimulatedKind.Unidentified()));
        return models;
    }

    /// <summary>
    /// A keyboard that says what makes the cross-platform app draw <paramref name="layout"/> with
    /// <paramref name="format"/>: kind 10 for the standard format, 0 or 1 for the legacy one, with a product ID the
    /// rule looks at. Null when no answer leads there — 4add1 and 4add2 exist only on the legacy format.
    /// </summary>
    public static SimulatedModel? IdentifiedAs(KeyboardLayout layout, RecordFormat format)
    {
        (byte Kind, ushort ProductId)[] candidates = format == RecordFormat.Standard
            ? [(10, 0x8840), (10, 0x8850), (10, 0x8851)]
            : [(0, 0x8840), (0, 0x8850), (0, 0x8851), (1, 0x8840)];
        if (layout.KeyCount > byte.MaxValue || layout.KnobCount > byte.MaxValue)
            return null;
        foreach (var (kind, productId) in candidates)
        {
            var identity = new KeyboardIdentity((byte)layout.KeyCount, (byte)layout.KnobCount, kind);
            if (KeyboardLayout.IdentifiedId(identity, productId) != layout.Id)
                continue;
            // 1189:8851 is not among the IDs the app looks for; 514C:8851 is.
            var vendor = productId == 0x8851 ? (ushort)0x514C : (ushort)0x1189;
            return new SimulatedModel(new UsbIdentity(vendor, productId), new SimulatedKind.Identified(identity));
        }
        return null;
    }
}
