namespace MINIKeyboard.Presentation;

/// <summary>
/// A key on the HID keyboard page and how to show it. Labels are the same in every language — letters, digits and what
/// PC keyboards print on their keys; the app names the keys in the user's language.
/// </summary>
/// <param name="Label">What a key cap shows: "A", "F5", "⌫".</param>
/// <param name="Shifted">The shifted character a key cap shows above, if any.</param>
public readonly record struct KeyUsage(byte Usage, string Label, string? Shifted = null)
{
    public static IReadOnlyList<KeyUsage> Letters { get; } =
    [
        new(0x04, "A"), new(0x05, "B"), new(0x06, "C"), new(0x07, "D"), new(0x08, "E"), new(0x09, "F"),
        new(0x0A, "G"), new(0x0B, "H"), new(0x0C, "I"), new(0x0D, "J"), new(0x0E, "K"), new(0x0F, "L"),
        new(0x10, "M"), new(0x11, "N"), new(0x12, "O"), new(0x13, "P"), new(0x14, "Q"), new(0x15, "R"),
        new(0x16, "S"), new(0x17, "T"), new(0x18, "U"), new(0x19, "V"), new(0x1A, "W"), new(0x1B, "X"),
        new(0x1C, "Y"), new(0x1D, "Z"),
    ];

    public static IReadOnlyList<KeyUsage> Digits { get; } =
    [
        new(0x1E, "1", "!"), new(0x1F, "2", "@"), new(0x20, "3", "#"), new(0x21, "4", "$"), new(0x22, "5", "%"),
        new(0x23, "6", "^"), new(0x24, "7", "&"), new(0x25, "8", "*"), new(0x26, "9", "("), new(0x27, "0", ")"),
    ];

    public static IReadOnlyList<KeyUsage> Editing { get; } =
    [
        new(0x28, "↵"), new(0x29, "Esc"), new(0x2A, "⌫"), new(0x2B, "↹"), new(0x2C, "Space"), new(0x39, "⇪"),
        new(0x65, "Menu"),
    ];

    public static IReadOnlyList<KeyUsage> Symbols { get; } =
    [
        new(0x2D, "-", "_"), new(0x2E, "=", "+"), new(0x2F, "[", "{"), new(0x30, "]", "}"), new(0x31, "\\", "|"),
        new(0x33, ";", ":"), new(0x34, "'", "\""), new(0x35, "`", "~"), new(0x36, ",", "<"), new(0x37, ".", ">"),
        new(0x38, "/", "?"),
    ];

    public static IReadOnlyList<KeyUsage> FunctionKeys { get; } =
    [
        new(0x3A, "F1"), new(0x3B, "F2"), new(0x3C, "F3"), new(0x3D, "F4"), new(0x3E, "F5"), new(0x3F, "F6"),
        new(0x40, "F7"), new(0x41, "F8"), new(0x42, "F9"), new(0x43, "F10"), new(0x44, "F11"), new(0x45, "F12"),
    ];

    public static IReadOnlyList<KeyUsage> ExtraFunctionKeys { get; } =
    [
        new(0x68, "F13"), new(0x69, "F14"), new(0x6A, "F15"), new(0x6B, "F16"), new(0x6C, "F17"), new(0x6D, "F18"),
        new(0x6E, "F19"), new(0x6F, "F20"), new(0x70, "F21"), new(0x71, "F22"), new(0x72, "F23"), new(0x73, "F24"),
    ];

    public static IReadOnlyList<KeyUsage> SystemKeys { get; } =
        [new(0x46, "PrtSc"), new(0x47, "ScrLk"), new(0x48, "Pause")];

    public static IReadOnlyList<KeyUsage> Navigation { get; } =
    [
        new(0x49, "Ins"), new(0x4A, "Home"), new(0x4B, "PgUp"), new(0x4C, "Del"), new(0x4D, "End"),
        new(0x4E, "PgDn"), new(0x4F, "→"), new(0x50, "←"), new(0x51, "↓"), new(0x52, "↑"),
    ];

    public static IReadOnlyList<KeyUsage> Keypad { get; } =
    [
        new(0x53, "Num"), new(0x54, "÷"), new(0x55, "×"), new(0x56, "−"), new(0x57, "+"), new(0x58, "⌤"),
        new(0x59, "1"), new(0x5A, "2"), new(0x5B, "3"), new(0x5C, "4"), new(0x5D, "5"), new(0x5E, "6"),
        new(0x5F, "7"), new(0x60, "8"), new(0x61, "9"), new(0x62, "0"), new(0x63, "."),
    ];

    /// <summary>The two keys ISO keyboards have and ANSI ones don't ("Non-US # and ~", "Non-US \ and |").</summary>
    public static IReadOnlyList<KeyUsage> International { get; } = [new(0x32, "#", "~"), new(0x64, "\\", "|")];

    /// <summary>
    /// Every key both vendor tools offer, and F13–F24 and the two non-US keys the cross-platform app adds.
    /// </summary>
    public static IReadOnlyList<KeyUsage> All { get; } =
    [
        .. Letters, .. Digits, .. Editing, .. Symbols, .. FunctionKeys, .. SystemKeys, .. Navigation, .. Keypad,
        .. ExtraFunctionKeys, .. International,
    ];

    private static readonly Dictionary<byte, KeyUsage> ByUsage = All.ToDictionary(key => key.Usage);

    public static KeyUsage? Named(byte usage) => ByUsage.TryGetValue(usage, out var key) ? key : null;
}
