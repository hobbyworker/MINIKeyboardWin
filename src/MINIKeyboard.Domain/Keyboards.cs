namespace MINIKeyboard.Domain;

/// <summary>
/// A USB device's vendor and product IDs. These keyboards carry no name or serial number, so this is all there is
/// to tell them apart by.
/// </summary>
public readonly record struct UsbIdentity(ushort VendorId, ushort ProductId) : IComparable<UsbIdentity>
{
    public int CompareTo(UsbIdentity other) =>
        VendorId != other.VendorId ? VendorId.CompareTo(other.VendorId) : ProductId.CompareTo(other.ProductId);

    /// <summary>"1189:8890".</summary>
    public override string ToString() => $"{VendorId:X4}:{ProductId:X4}";
}

/// <summary>
/// The two kinds of keyboard the app configures, told apart by USB ID. Each came with a vendor tool of its own, and
/// the two tools speak different commands.
/// </summary>
public enum KeyboardFamily
{
    /// <summary>
    /// 1189:8890, the keyboard of the vendor's Windows tool. Its configuration interface (USB interface 1) takes
    /// eight-byte packets and answers nothing.
    /// </summary>
    K8890,

    /// <summary>
    /// The keyboards of the vendor's cross-platform app: vendor 1189 or 514C with product 8842, 8840, 8830–8833,
    /// 8850 or 8851. Configured through 64-byte reports on interface 0; most say how many keys and knobs they have,
    /// and the newer ones read back what they hold.
    /// </summary>
    K884x,
}

public static class KeyboardFamilies
{
    public static readonly UsbIdentity K8890Usb = new(0x1189, 0x8890);

    /// <summary>
    /// Every keyboard the app looks for, in the order it looks. The cross-platform app looks for all eight product IDs
    /// under vendor 1189, then under 514C.
    /// </summary>
    public static readonly EquatableArray<UsbIdentity> Supported = new(SupportedIdentities());

    private static IEnumerable<UsbIdentity> SupportedIdentities()
    {
        ushort[] products = [0x8842, 0x8840, 0x8830, 0x8831, 0x8832, 0x8833, 0x8850, 0x8851];
        yield return K8890Usb;
        foreach (var product in products)
            yield return new UsbIdentity(0x1189, product);
        foreach (var product in products)
            yield return new UsbIdentity(0x514C, product);
    }

    public static KeyboardFamily? Of(UsbIdentity usb)
    {
        if (!Supported.Contains(usb))
            return null;
        return usb == K8890Usb ? KeyboardFamily.K8890 : KeyboardFamily.K884x;
    }

    extension(KeyboardFamily family)
    {
        /// <summary>The USB interface that takes the configuration.</summary>
        public byte ConfigurationInterface() => family == KeyboardFamily.K8890 ? (byte)1 : (byte)0;
    }
}

/// <summary>How a 884x keyboard stores a key's action.</summary>
public enum RecordFormat
{
    /// <summary>
    /// Firmware that answers identification with type 10: <c>FD</c> records of three-byte entries — a delay and a
    /// code, every modifier an entry of its own. They read back.
    /// </summary>
    Standard,

    /// <summary>
    /// Firmware that answers with any other type: <c>FD</c> records of (modifier, key) pairs and one delay for the
    /// whole key. They read back when asked.
    /// </summary>
    Legacy,

    /// <summary>
    /// A keyboard that does not answer identification: <c>FE</c> records of (modifier, key) pairs, written the way
    /// keyboards of the 1189:8840 kind are known to take them. Nothing reads back.
    /// </summary>
    Unidentified,
}

/// <summary>The commands a connected keyboard takes, settled once it is connected and identified.</summary>
public abstract record CommandSet
{
    private CommandSet() { }

    /// <summary>The 1189:8890: packets after a report ID — 3, or 0 or 2 where the keyboard's descriptor says so.</summary>
    public sealed record Packets(byte ReportId) : CommandSet;

    /// <summary>A 884x keyboard.</summary>
    public sealed record Records(RecordFormat Format) : CommandSet;
}

/// <summary>What a 884x keyboard answers when asked who it is (<c>FB FB FB</c>).</summary>
/// <param name="Kind">
/// 10 and up for the standard record format (the cross-platform app's <c>version &lt; 10</c>; 11 on a 514C:8850), below
/// 10 for the legacy one. On a keyboard of four keys and a knob it also picks between two layouts.
/// </param>
public readonly record struct KeyboardIdentity(byte KeyCount, byte KnobCount, byte Kind)
{
    public RecordFormat RecordFormat => Kind >= 10 ? RecordFormat.Standard : RecordFormat.Legacy;

    /// <summary>
    /// Whether the type is one the app's rules were made from: 0 and 1, which the cross-platform app tells two layouts of
    /// four keys and a knob apart by, 10, its standard firmware, and 11, the 514C:8850 tried. Any other is handled the
    /// same way, but untested.
    /// </summary>
    public bool IsKnownKind => Kind is 0 or 1 or 10 or 11;
}

/// <summary>What the app learned about a keyboard once it was connected.</summary>
/// <param name="Identity">What a 884x keyboard said about itself; null on the 8890 and on a keyboard that did not answer.</param>
public sealed record KeyboardIdentification(CommandSet Commands, KeyboardIdentity? Identity);
