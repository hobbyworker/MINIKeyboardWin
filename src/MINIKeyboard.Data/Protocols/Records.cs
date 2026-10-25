using MINIKeyboard.Domain;

namespace MINIKeyboard.Data.Protocols;

/// <summary>
/// The records 884x keyboards store actions in, as the cross-platform app writes and reads them
/// (<c>PHY2_KEY_Value</c> and <c>PHY_KEY_Value</c>), and the <c>FE</c> record of keyboards that don't say which they
/// take.
/// </summary>
/// <remarks>
/// A record starts <c>&lt;marker&gt; &lt;record&gt; &lt;layer + 1&gt; &lt;kind&gt;</c>; the kind is 1 for key
/// presses, 2 for a media key, 3 for a mouse action, 5 for key presses after a wait (legacy format) and 8 for lights.
/// <list type="bullet">
/// <item>Standard (64 bytes): <c>[5]</c> the entry count, then three-byte entries from <c>[6]</c> — the wait before
/// the entry in milliseconds, high byte first, and a code: a key usage, or <c>F1</c>–<c>F8</c> for a modifier held
/// until the next key.</item>
/// <item>Legacy (50 bytes): <c>[4…5]</c> the wait, low byte first, <c>[9]</c> the count, then (modifiers, key) pairs
/// from <c>[10]</c>. Modifiers with no key after them sit in the pair after the last.</item>
/// <item><c>FE</c>: as legacy, without a wait.</item>
/// </list>
/// </remarks>
public static class Records
{
    public const int StandardLength = 64;
    public const int LegacyLength = 50;
    public const int StandardEntries = 18;
    public const int LegacyPairs = 20;

    private enum Kind : byte
    {
        Keys = 1,
        Media = 2,
        Mouse = 3,
        DelayedKeys = 5,
        Lighting = 8,
    }

    // Encoding

    /// <summary>
    /// The record that stores <paramref name="action"/> for the control in record <paramref name="recordId"/> of
    /// <paramref name="layer"/> (from 0).
    /// </summary>
    /// <exception cref="KeyboardDeviceException">The action doesn't fit the record.</exception>
    public static byte[] Encode(KeyAction action, byte recordId, int layer, RecordFormat format) => format switch
    {
        RecordFormat.Standard => Standard(action, recordId, layer),
        RecordFormat.Legacy => Pairs(action, 0xFD, recordId, layer, LegacyLength, keepsDelay: true),
        _ => Unidentified(action, recordId, layer),
    };

    public static byte[] Standard(KeyAction action, byte recordId, int layer)
    {
        var record = Header(0xFD, recordId, layer, StandardLength);
        switch (action)
        {
            case KeyAction.UnassignedAction:
                record[3] = (byte)Kind.Keys;
                break;
            case KeyAction.Keys { Macro: var macro }:
                {
                    record[3] = (byte)Kind.Keys;
                    var entries = new List<(uint Delay, byte Code)>();
                    foreach (var stroke in macro.Strokes)
                    {
                        var codes = new List<byte>(ModifierCodes(stroke.Modifiers));
                        if (!stroke.IsModifiersOnly)
                            codes.Add(stroke.Key);
                        for (var index = 0; index < codes.Count; index++)
                            entries.Add((index == 0 ? stroke.Delay : 0, codes[index]));
                    }
                    if (entries.Count > StandardEntries)
                        throw DeviceErrors.Unsupported();
                    record[5] = (byte)entries.Count;
                    for (var index = 0; index < entries.Count; index++)
                    {
                        var (delay, code) = entries[index];
                        var wait = (ushort)Math.Min(delay, ushort.MaxValue);
                        record[6 + 3 * index] = (byte)(wait >> 8);
                        record[7 + 3 * index] = (byte)wait;
                        record[8 + 3 * index] = code;
                    }
                    break;
                }
            case KeyAction.Media { Key: var key }:
                record[3] = (byte)Kind.Media;
                record[5] = 2;
                record[8] = (byte)(key.Usage & 0xFF);
                record[11] = (byte)(key.Usage >> 8);
                break;
            case KeyAction.Mouse { Action: var mouse }:
                record[3] = (byte)Kind.Mouse;
                record[5] = 4;
                record[8] = ModifierCodes(mouse.Modifiers()).FirstOrDefault();
                record[11] = mouse.Buttons();
                record[20] = mouse.Wheel();
                break;
        }
        return record;
    }

    /// <summary>
    /// The legacy record, or with marker <c>FE</c> and no wait the record of an unidentified keyboard.
    /// </summary>
    public static byte[] Pairs(KeyAction action, byte marker, byte recordId, int layer, int length, bool keepsDelay)
    {
        var record = Header(marker, recordId, layer, length);
        switch (action)
        {
            case KeyAction.UnassignedAction:
                record[3] = (byte)Kind.Keys;
                break;
            case KeyAction.Keys { Macro: var macro }:
                {
                    var delay = keepsDelay ? (ushort)Math.Min(macro.Delay, ushort.MaxValue) : (ushort)0;
                    record[3] = (byte)(delay > 0 ? Kind.DelayedKeys : Kind.Keys);
                    record[4] = (byte)delay;
                    record[5] = (byte)(delay >> 8);
                    if (macro.Strokes is [{ IsModifiersOnly: true } only])
                    {
                        record[10] = (byte)only.Modifiers;
                    }
                    else
                    {
                        if (macro.Strokes.Count > LegacyPairs || 10 + 2 * macro.Strokes.Count > length)
                            throw DeviceErrors.Unsupported();
                        record[9] = (byte)macro.Strokes.Count;
                        for (var index = 0; index < macro.Strokes.Count; index++)
                        {
                            record[10 + 2 * index] = (byte)macro.Strokes[index].Modifiers;
                            record[11 + 2 * index] = macro.Strokes[index].Key;
                        }
                    }
                    break;
                }
            case KeyAction.Media { Key: var key }:
                record[3] = (byte)Kind.Media;
                record[9] = 2;
                record[10] = (byte)(key.Usage & 0xFF);
                record[11] = (byte)(key.Usage >> 8);
                break;
            case KeyAction.Mouse { Action: var mouse }:
                record[3] = (byte)Kind.Mouse;
                record[9] = 1;
                record[10] = (byte)mouse.Modifiers();
                record[11] = mouse.Buttons();
                record[14] = mouse.Wheel();
                break;
        }
        return record;
    }

    /// <summary>
    /// The <c>FE</c> record: the legacy form with these differences, which keyboards of the 1189:8840 kind are known to
    /// take — a media key has a count of 0, a wheel move a count of 3.
    /// </summary>
    public static byte[] Unidentified(KeyAction action, byte recordId, int layer)
    {
        var record = Pairs(action, 0xFE, recordId, layer, StandardLength, keepsDelay: false);
        if (action is KeyAction.Media)
            record[9] = 0;
        else if (action is KeyAction.Mouse { Action: var mouse } && mouse.Wheel() != 0)
            record[9] = 3;
        return record;
    }

    /// <summary>
    /// A legacy-format layer's lights, which it keeps in record 0: <c>FE B0 &lt;layer + 1&gt; 08</c>, a count of 1 and
    /// <c>colour &lt;&lt; 4 | mode</c> in <c>[11]</c>.
    /// </summary>
    public static byte[] LegacyLighting(LightColor color, byte mode, int layer, int length)
    {
        var record = new byte[length];
        record[0] = 0xFE;
        record[1] = 0xB0;
        record[2] = K884x.LayerByte(layer + 1);
        record[3] = (byte)Kind.Lighting;
        record[9] = 1;
        record[11] = (byte)((byte)color << 4 | (mode & 0x0F));
        return record;
    }

    /// <summary>
    /// <c>F1</c>–<c>F8</c> for the modifiers set, lowest bit first: left Control, Shift, Alt, Super, then right.
    /// </summary>
    public static IReadOnlyList<byte> ModifierCodes(Modifiers modifiers)
    {
        var codes = new List<byte>();
        byte code = 0xF1;
        foreach (var modifier in ModifiersExtensions.Each())
        {
            if (modifiers.HasFlag(modifier))
                codes.Add(code);
            code++;
        }
        return codes;
    }

    public static Modifiers? ModifiersOfCode(byte code) =>
        code is >= 0xF1 and <= 0xF8 ? (Modifiers)(1 << (code - 0xF1)) : null;

    private static byte[] Header(byte marker, byte recordId, int layer, int length)
    {
        var record = new byte[length];
        record[0] = marker;
        record[1] = recordId;
        record[2] = K884x.LayerByte(layer + 1);
        return record;
    }

    // Decoding

    /// <summary>The action a record the keyboard reported stores, or null for a kind the app doesn't know.</summary>
    public static KeyAction? Decode(ReadOnlySpan<byte> record, RecordFormat format)
    {
        if (KindOf(At(record, 3)) is not { } kind)
            return At(record, 3) == 0 ? KeyAction.Unassigned : null;
        var standard = format == RecordFormat.Standard;
        switch (kind)
        {
            case Kind.Keys or Kind.DelayedKeys when standard:
                return StandardKeys(record);
            case Kind.Keys or Kind.DelayedKeys:
                return PairedKeys(record, kind);
            case Kind.Media:
                {
                    var usage = standard
                        ? (ushort)(At(record, 8) | At(record, 11) << 8)
                        : (ushort)(At(record, 10) | At(record, 11) << 8);
                    return usage == 0 ? KeyAction.Unassigned : new KeyAction.Media(new MediaKey(usage));
                }
            case Kind.Mouse when standard:
                return Mouse(At(record, 11), At(record, 20), ModifiersOfCode(At(record, 8)) ?? Modifiers.None);
            case Kind.Mouse:
                {
                    var held = ((Modifiers)At(record, 10)).Members().Cast<Modifiers?>().FirstOrDefault()
                        ?? Modifiers.None;
                    return Mouse(At(record, 11), At(record, 14), held);
                }
            default:
                return null;
        }
    }

    private static KeyAction StandardKeys(ReadOnlySpan<byte> record)
    {
        var strokes = new List<KeyStroke>();
        var held = Modifiers.None;
        uint? heldDelay = null;
        var count = Math.Min((int)At(record, 5), StandardEntries);
        for (var index = 0; index < count; index++)
        {
            var code = At(record, 8 + 3 * index);
            var delay = (uint)(At(record, 6 + 3 * index) << 8 | At(record, 7 + 3 * index));
            if (ModifiersOfCode(code) is { } modifier)
            {
                held |= modifier;
                heldDelay ??= delay;
            }
            else if (code != 0)
            {
                strokes.Add(new KeyStroke(held, code).WithDelay(heldDelay ?? delay));
                held = Modifiers.None;
                heldDelay = null;
            }
        }
        if (held != Modifiers.None && strokes.Count == 0)
            strokes.Add(new KeyStroke(held, 0).WithDelay(heldDelay ?? 0));
        return strokes.Count == 0 ? KeyAction.Unassigned : new KeyAction.Keys(new KeyMacro(new(strokes)));
    }

    private static KeyAction PairedKeys(ReadOnlySpan<byte> record, Kind kind)
    {
        var delay = kind == Kind.DelayedKeys ? (uint)(At(record, 4) | At(record, 5) << 8) : 0;
        var count = Math.Min((int)At(record, 9), LegacyPairs);
        var strokes = new List<KeyStroke>();
        for (var index = 0; index < count; index++)
        {
            var modifiers = (Modifiers)At(record, 10 + 2 * index);
            var key = At(record, 11 + 2 * index);
            if (modifiers != Modifiers.None || key != 0)
                strokes.Add(new KeyStroke(modifiers, key));
        }
        if (count == 0 && At(record, 10) != 0)
            strokes = [new KeyStroke((Modifiers)At(record, 10), 0)];
        if (strokes.Count > 1)
            strokes.RemoveAll(stroke => stroke.IsModifiersOnly);
        return strokes.Count == 0 && delay == 0
            ? KeyAction.Unassigned
            : new KeyAction.Keys(new KeyMacro(new(strokes), delay));
    }

    private static KeyAction? Mouse(byte buttons, byte wheel, Modifiers modifiers)
    {
        if (buttons == 0 && wheel == 0)
            return KeyAction.Unassigned;
        return MouseActions.FromParts(buttons, wheel, modifiers) is { } action ? new KeyAction.Mouse(action) : null;
    }

    private static Kind? KindOf(byte value) => value is 1 or 2 or 3 or 5 or 8 ? (Kind)value : null;

    private static byte At(ReadOnlySpan<byte> record, int index) => index < record.Length ? record[index] : (byte)0;
}
