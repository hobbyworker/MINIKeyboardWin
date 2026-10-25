using System.Numerics;

namespace MINIKeyboard.Domain;

/// <summary>A range that holds both its ends.</summary>
public readonly record struct InclusiveRange<T>(T First, T Last) where T : struct, INumber<T>
{
    public bool Contains(T value) => value >= First && value <= Last;

    public T Clamp(T value) => T.Clamp(value, First, Last);
}

/// <summary>A colour with one byte per channel.</summary>
public readonly record struct KeyColor(byte Red, byte Green, byte Blue)
{
    /// <summary>
    /// The colours keys 1–16 start with in the cross-platform app (<c>KeyBoard_KeyLed</c>), the same on every layer.
    /// </summary>
    public static readonly EquatableArray<KeyColor> DefaultKeyColors =
    [
        new(0xff, 0x00, 0x00), new(0xff, 0x80, 0x30), new(0xff, 0xff, 0x30), new(0x00, 0xff, 0x00),
        new(0x00, 0xff, 0xff), new(0x00, 0x00, 0xff), new(0x80, 0x00, 0x80), new(0x8b, 0x00, 0x00),
        new(0xff, 0xa5, 0x00), new(0xff, 0xff, 0x96), new(0x7d, 0xff, 0x00), new(0x00, 0x8b, 0x8b),
        new(0x00, 0x00, 0x8b), new(0xff, 0x00, 0xff), new(0xff, 0x66, 0x66), new(0xff, 0xc8, 0x64),
    ];
}

/// <summary>The colours a legacy-format or unidentified 884x keyboard lights in, as the high nibble it takes.</summary>
public enum LightColor : byte
{
    Red = 1,
    Orange,
    Yellow,
    Green,
    Cyan,
    Blue,
    Purple,
}

public static class LightColors
{
    public static readonly EquatableArray<LightColor> All =
    [
        LightColor.Red, LightColor.Orange, LightColor.Yellow, LightColor.Green, LightColor.Cyan, LightColor.Blue,
        LightColor.Purple,
    ];
}

/// <summary>What one layer's lights do, in the form the keyboard takes.</summary>
public abstract record LayerLighting
{
    private LayerLighting() { }

    public abstract byte Mode { get; }

    /// <summary>The 1189:8890: one of its modes (<see cref="LightingSupport.Modes"/>).</summary>
    public sealed record ModeOnly(byte Mode) : LayerLighting
    {
        public override byte Mode { get; } = Mode;
    }

    /// <summary>A legacy-format or unidentified 884x keyboard: a colour and a mode, 0–5.</summary>
    public sealed record ColorMode(LightColor Color, byte Mode) : LayerLighting
    {
        public override byte Mode { get; } = Mode;
    }

    /// <summary>A standard-format keyboard: a mode, 0–5, and a colour for each of keys 1–16.</summary>
    public sealed record PerKey(byte Mode, EquatableArray<KeyColor> Colors) : LayerLighting
    {
        public override byte Mode { get; } = Mode;
    }
}

/// <summary>What a keyboard can do with its lights.</summary>
public abstract record LightingSupport
{
    private LightingSupport() { }

    /// <summary>The 1189:8890's modes 0–2, named for what they do on a 1189:8890 with six keys and a knob.</summary>
    public static readonly EquatableArray<byte> K8890Modes = [0, 1, 2];

    /// <summary>Keys whose colour the standard format stores.</summary>
    public const int ColorKeyCount = 16;

    /// <summary>The 1189:8890's three modes, one per layer.</summary>
    public sealed record Modes(EquatableArray<byte> Values) : LightingSupport;

    /// <summary>A colour and a mode per layer.</summary>
    public sealed record ColorAndMode(InclusiveRange<byte> ModeRange) : LightingSupport;

    /// <summary>A mode per layer and a colour for each of the first keys.</summary>
    public sealed record PerKeyColor(InclusiveRange<byte> ModeRange, int Keys) : LightingSupport;
}
