namespace MINIKeyboard.Presentation;

/// <summary>One cap of the full-size keyboard, before the keyboard says which keys it is given.</summary>
internal readonly record struct Place(BoardFace Face, int Row, double Column, double Width);

/// <summary>
/// The full-size keyboard of the Keys page, laid out as the cross-platform app's page: 15 key widths of main block, the
/// navigation keys and the keypad, with a PC's modifiers in the bottom row.
/// </summary>
internal static class FullKeyboard
{
    /// <summary>A row's width in key widths: 15, a quarter, 3 of navigation, a quarter, 4 of keypad.</summary>
    public const double Width = 22.5;

    /// <summary>Every cap, row by row from the left.</summary>
    public static IReadOnlyList<Place> Places { get; } = Build();

    /// <summary>A cap, or a gap when it has no face.</summary>
    private readonly record struct Slot(BoardFace? Face, double Width);

    private static Slot K(byte usage, double width = 1) =>
        new(new BoardFace.Key(KeyUsage.Named(usage)
            ?? throw new InvalidOperationException("every key on the board has a name")), width);

    private static Slot M(ModifierKey modifier, double width) => new(new BoardFace.Modifier(modifier), width);

    private static Slot Gap(double width) => new(null, width);

    private static IEnumerable<Slot> Keys(params byte[] usages) => usages.Select(usage => K(usage));

    private static Slot[][] Rows() =>
    [
        [
            K(0x29), Gap(1), .. Keys(0x3A, 0x3B, 0x3C, 0x3D), Gap(0.5), .. Keys(0x3E, 0x3F, 0x40, 0x41), Gap(0.5),
            .. Keys(0x42, 0x43, 0x44, 0x45), Gap(0.25), .. Keys(0x46, 0x47, 0x48),
        ],
        [
            .. Keys(0x35, 0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x2D, 0x2E), K(0x2A, 2),
            Gap(0.25), .. Keys(0x49, 0x4A, 0x4B), Gap(0.25), .. Keys(0x53, 0x54, 0x55, 0x56),
        ],
        [
            K(0x2B, 1.5), .. Keys(0x14, 0x1A, 0x08, 0x15, 0x17, 0x1C, 0x18, 0x0C, 0x12, 0x13, 0x2F, 0x30),
            K(0x31, 1.5), Gap(0.25), .. Keys(0x4C, 0x4D, 0x4E), Gap(0.25), .. Keys(0x5F, 0x60, 0x61, 0x57),
        ],
        [
            K(0x39, 1.75), .. Keys(0x04, 0x16, 0x07, 0x09, 0x0A, 0x0B, 0x0D, 0x0E, 0x0F, 0x33, 0x34), K(0x28, 2.25),
            Gap(3.5), .. Keys(0x5C, 0x5D, 0x5E),
        ],
        [
            M(ModifierKey.Shift, 2.25), .. Keys(0x1D, 0x1B, 0x06, 0x19, 0x05, 0x11, 0x10, 0x36, 0x37, 0x38),
            M(ModifierKey.RightShift, 2.75), Gap(1.25), K(0x52), Gap(1.25), .. Keys(0x59, 0x5A, 0x5B, 0x58),
        ],
        [
            M(ModifierKey.Control, 1.25), M(ModifierKey.Super, 1.25), M(ModifierKey.Alt, 1.25), K(0x2C, 6.25),
            M(ModifierKey.RightAlt, 1.25), M(ModifierKey.RightSuper, 1.25), K(0x65, 1.25),
            M(ModifierKey.RightControl, 1.25), Gap(0.25), .. Keys(0x50, 0x51, 0x4F), Gap(0.25), K(0x62, 2), K(0x63),
        ],
    ];

    private static List<Place> Build()
    {
        var places = new List<Place>();
        var rows = Rows();
        for (var row = 0; row < rows.Length; row++)
        {
            var column = 0.0;
            foreach (var slot in rows[row])
            {
                if (slot.Face is { } face)
                    places.Add(new Place(face, row, column, slot.Width));
                column += slot.Width;
            }
        }
        return places;
    }
}
