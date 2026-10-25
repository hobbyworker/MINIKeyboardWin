namespace MINIKeyboard.Domain;

/// <summary>One action of a knob. Some knobs also turn while pressed.</summary>
public enum KnobPart : byte
{
    RotateLeft,
    Press,
    RotateRight,
    PressRotateLeft,
    PressRotateRight,
}

public static class KnobParts
{
    public static readonly EquatableArray<KnobPart> All =
    [
        KnobPart.RotateLeft, KnobPart.Press, KnobPart.RotateRight, KnobPart.PressRotateLeft,
        KnobPart.PressRotateRight,
    ];

    /// <summary>A knob's usual three actions.</summary>
    public static readonly EquatableArray<KnobPart> Turns = [KnobPart.RotateLeft, KnobPart.Press, KnobPart.RotateRight];
}

/// <summary>
/// A key, or one action of a knob — what the user selects and binds. Keys come before knobs, each in number order,
/// a knob's actions in <see cref="KnobPart"/> order.
/// </summary>
public readonly record struct ControlId : IComparable<ControlId>
{
    private ControlId(bool isKey, byte number, KnobPart part)
    {
        IsKey = isKey;
        Number = number;
        Part = part;
    }

    public bool IsKey { get; }

    /// <summary>The key's or the knob's number, from 1.</summary>
    public byte Number { get; }

    /// <summary>The knob's action; <see cref="KnobPart.RotateLeft"/> on a key.</summary>
    public KnobPart Part { get; }

    public static ControlId Key(byte number) => new(true, number, KnobPart.RotateLeft);

    public static ControlId Knob(byte number, KnobPart part) => new(false, number, part);

    public byte? KnobNumber => IsKey ? null : Number;

    public int CompareTo(ControlId other)
    {
        if (IsKey != other.IsKey)
            return IsKey ? -1 : 1;
        return Number != other.Number ? Number.CompareTo(other.Number) : Part.CompareTo(other.Part);
    }

    public static bool operator <(ControlId left, ControlId right) => left.CompareTo(right) < 0;

    public static bool operator >(ControlId left, ControlId right) => left.CompareTo(right) > 0;

    public static bool operator <=(ControlId left, ControlId right) => left.CompareTo(right) <= 0;

    public static bool operator >=(ControlId left, ControlId right) => left.CompareTo(right) >= 0;

    public override string ToString() => IsKey ? $"Key({Number})" : $"Knob({Number}, {Part})";
}

/// <summary>How far a keyboard is turned clockwise from the way its layout is drawn — kept with the user's choice.</summary>
public enum Rotation : byte
{
    Upright,
    Quarter,
    Half,
    ThreeQuarters,
}

public static class Rotations
{
    public static readonly EquatableArray<Rotation> All =
        [Rotation.Upright, Rotation.Quarter, Rotation.Half, Rotation.ThreeQuarters];

    /// <summary>Any number of quarter turns, either way.</summary>
    public static Rotation FromQuarterTurns(long turns) => (Rotation)(((turns % 4) + 4) % 4);

    extension(Rotation rotation)
    {
        /// <summary>Quarter turns clockwise, 0–3.</summary>
        public byte QuarterTurns() => (byte)rotation;

        public Rotation Next() => FromQuarterTurns((long)rotation + 1);
    }
}

/// <summary>
/// A keyboard's controls, named as the cross-platform app names its layouts ("6add1" — six keys and a knob), or as an
/// arrangement the user set (<see cref="GridShape"/>). Where they are drawn is the presentation's business, where they
/// are stored the device's.
/// </summary>
public sealed record KeyboardLayout(string Id, EquatableArray<ControlId> Controls)
{
    /// <summary>What the cross-platform app draws when a keyboard's answer matches no layout.</summary>
    public const string FallbackId = "15add3";

    public int KeyCount => Controls.Count(control => control.IsKey);

    public int KnobCount => Controls.Select(control => control.KnobNumber).OfType<byte>().Distinct().Count();

    public bool Has(ControlId control) => Controls.Contains(control);

    /// <summary>The cross-platform app's layouts.</summary>
    public static IReadOnlyList<KeyboardLayout> All => KeyboardLayouts.All;

    public static KeyboardLayout? Named(string id) => All.FirstOrDefault(layout => layout.Id == id);

    /// <summary>
    /// The layout the cross-platform app draws for what a keyboard says of itself (<c>Widget::Identify_KeyBoard_style</c>).
    /// The kind also tells two keyboards of four keys and a knob apart; the product ID tells two others apart.
    /// </summary>
    public static string IdentifiedId(KeyboardIdentity identity, ushort productId) =>
        (identity.KeyCount, identity.KnobCount) switch
        {
            (0, 1) => "0add1",
            (0, 2) => productId == 0x8850 ? "0add2" : "0add1",
            (0, 3) => "0add3",
            (1, 0) => "1add0",
            (2, 0) => "2add0",
            (3, 0) => "3add0",
            (3, 1) => "3add1",
            (4, 0) => "4Key",
            (4, 1) when identity.Kind == 0 => productId == 0x8851 ? "4add2" : "4add1",
            (4, 1) when identity.Kind == 1 => "4add2",
            (4, 3) => "4add3",
            (5, 0) => "5Key_Mute",
            (6, 0) => "6Key",
            (6, 1) => "6add1",
            (6, 2) => "6add2",
            (9, 2) => "9add2",
            (9, 3) => "9add3",
            (11, 3) => "11add3",
            (12, 0) => "12add0",
            (12, 2) => "12add2",
            (12, 3) => "12add3",
            (12, 4) => "12add4",
            (16, 0) => "16add0",
            (16, 3) => "16add3",
            (21, 1) => "21add1",
            _ => FallbackId,
        };
}
