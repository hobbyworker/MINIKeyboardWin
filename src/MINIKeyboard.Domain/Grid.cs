namespace MINIKeyboard.Domain;

/// <summary>
/// The arrangement of a keyboard whose shape no vendor tool draws — the 1189:8890's Windows tool shows the same panel
/// of buttons for every keyboard. It says only what is known: how many keys, how many to a row, how many knobs. A
/// schematic, not the keyboard's real shape.
/// </summary>
public readonly record struct GridShape
{
    private GridShape(byte keys, byte columns, byte knobs)
    {
        Keys = keys;
        Columns = columns;
        Knobs = knobs;
    }

    public byte Keys { get; }

    public byte Columns { get; }

    public byte Knobs { get; }

    public static GridShape? Create(byte keys, byte columns, byte knobs)
    {
        var valid = (keys > 0 || knobs > 0) && columns >= 1 && columns <= Math.Max(keys, (byte)1);
        return valid ? new GridShape(keys, columns, knobs) : null;
    }

    /// <summary>"grid-6-3-1": six keys, three to a row, one knob.</summary>
    public string Id => $"grid-{Keys}-{Columns}-{Knobs}";

    public static GridShape? FromId(string id)
    {
        var parts = id.Split('-');
        if (parts.Length != 4 || parts[0] != "grid")
            return null;
        if (!byte.TryParse(parts[1], out var keys) || !byte.TryParse(parts[2], out var columns)
            || !byte.TryParse(parts[3], out var knobs))
        {
            return null;
        }
        return Create(keys, columns, knobs);
    }

    public byte Rows => Keys == 0 ? (byte)0 : (byte)((Keys + Columns - 1) / Columns);

    /// <summary>The arrangement's controls: keys 1… filling the rows from the top left, then each knob's three actions.</summary>
    public KeyboardLayout Layout()
    {
        var keys = Enumerable.Range(1, Keys).Select(number => ControlId.Key((byte)number));
        var knobs = Enumerable.Range(1, Knobs)
            .SelectMany(knob => KnobParts.Turns.Select(part => ControlId.Knob((byte)knob, part)));
        return new KeyboardLayout(Id, new EquatableArray<ControlId>(keys.Concat(knobs)));
    }

    /// <summary>Only <see cref="GridLimits"/> builds one without checking — its own limits are valid.</summary>
    internal static GridShape Unchecked(byte keys, byte columns, byte knobs) => new(keys, columns, knobs);
}

/// <summary>How large an arrangement a keyboard can have.</summary>
public readonly record struct GridLimits(byte MaxKeys, byte MaxKnobs)
{
    /// <summary>
    /// What the vendor's Windows tool can program: twelve keys and two knobs. Until the user says otherwise, a
    /// 1189:8890 is drawn that large, four to a row as the tool's panel has them.
    /// </summary>
    public static readonly GridLimits K8890 = new(12, 2);

    public bool Contains(GridShape shape) => shape.Keys <= MaxKeys && shape.Knobs <= MaxKnobs;

    public GridShape Assumed() => GridShape.Unchecked(MaxKeys, (byte)Math.Min(4, Math.Max((int)MaxKeys, 1)), MaxKnobs);
}
