using System.Diagnostics;
using MINIKeyboard.Domain;

namespace MINIKeyboard.Data.Protocols;

/// <summary>
/// The 1189:8890's commands, as the vendor's Windows tool sends them (<c>FormMain.Download_Click</c>,
/// <c>Send_SwLayer</c>, <c>Send_WriteFlash_Cmd</c>, <c>Send_WriteFlashLED_Cmd</c>). Every packet is eight bytes after
/// the report ID; the keyboard answers none of them.
/// </summary>
/// <remarks>
/// One key is written as the layer switch <c>A1 &lt;layer&gt;</c>, the key's packets and <c>AA AA</c>; lights as the
/// layer switch, <c>B0 &lt;type&gt; &lt;mode&gt;</c> and <c>AA A1</c>. On report ID 0 there is no layer switch and no
/// layer in the type byte.
/// </remarks>
public static class K8890
{
    public const int PayloadLength = 8;

    /// <summary>Type nibbles (<c>KeyParam.KeyType_Num</c>).</summary>
    public enum Kind : byte
    {
        Keys = 1,
        Media = 2,
        Mouse = 3,
        Lighting = 8,
    }

    private static readonly (MediaKey Key, byte Low, byte High)[] ReportId0Media =
    [
        (MediaKey.PlayPause, 0x40, 0x00),
        (MediaKey.PreviousTrack, 0x80, 0x00),
        (MediaKey.NextTrack, 0x00, 0x01),
        (MediaKey.Mute, 0x04, 0x00),
        (MediaKey.VolumeUp, 0x02, 0x00),
        (MediaKey.VolumeDown, 0x01, 0x00),
    ];

    private static readonly (MediaKey Key, byte Low, byte High)[] ReportId2Media =
    [
        (MediaKey.PlayPause, 0x00, 0x04),
        (MediaKey.PreviousTrack, 0x00, 0x0B),
        (MediaKey.NextTrack, 0x00, 0x0A),
        (MediaKey.Mute, 0x00, 0x01),
        (MediaKey.VolumeUp, 0x40, 0x00),
        (MediaKey.VolumeDown, 0x80, 0x00),
    ];

    public static byte[] Pad(ReadOnlySpan<byte> bytes)
    {
        var packet = new byte[Math.Max(bytes.Length, PayloadLength)];
        bytes.CopyTo(packet);
        return packet;
    }

    /// <summary>A key's packets and the save that follows. <paramref name="layer"/> counts from 0.</summary>
    public static IReadOnlyList<byte[]> Download(byte number, KeyAction action, int layer, byte reportId)
    {
        var packets = new List<byte[]>(LayerSwitch(layer, reportId));
        packets.AddRange(KeyPackets(number, action, layer, reportId));
        packets.Add(Pad([0xAA, 0xAA]));
        return packets;
    }

    /// <summary>
    /// The lights and their save — and, with <paramref name="endsKeyRepeat"/>, a save with no key in it. After
    /// <c>AA A1</c> the keyboard repeats a key's first stroke without end, until the next key save or until it is
    /// plugged in again (the vendor's tool does the same); the bare <c>AA AA</c> ends that and leaves the keys as they
    /// were.
    /// </summary>
    public static IReadOnlyList<byte[]> DownloadLighting(byte mode, int layer, byte reportId, bool endsKeyRepeat = true)
    {
        var packets = new List<byte[]>(LayerSwitch(layer, reportId))
        {
            Pad([0xB0, TypeByte(Kind.Lighting, layer, reportId), mode]),
            Pad([0xAA, 0xA1]),
        };
        if (endsKeyRepeat)
        {
            packets.AddRange(LayerSwitch(layer, reportId));
            packets.Add(Pad([0xAA, 0xAA]));
        }
        return packets;
    }

    public static IReadOnlyList<byte[]> LayerSwitch(int layer, byte reportId) =>
        reportId == 0 ? [] : [Pad([0xA1, K884x.LayerByte(layer + 1)])];

    /// <summary><c>(layer &lt;&lt; 4) | kind</c>, or the kind alone on report ID 0.</summary>
    public static byte TypeByte(Kind kind, int layer, byte reportId) =>
        reportId == 0 ? (byte)kind : (byte)(K884x.LayerByte(layer + 1) << 4 | (byte)kind);

    /// <summary>
    /// A key press list is <c>count + 1</c> packets: the first carries the first press's modifiers, the rest one
    /// (modifiers, key) group each. A key with modifiers alone has no group, its modifiers in the first packet; a
    /// cleared key has nothing at all.
    /// </summary>
    /// <exception cref="KeyboardDeviceException">
    /// More than five groups, or a media key the report ID has no code for.
    /// </exception>
    public static IReadOnlyList<byte[]> KeyPackets(byte number, KeyAction action, int layer, byte reportId)
    {
        switch (action)
        {
            case KeyAction.UnassignedAction:
                return [[number, TypeByte(Kind.Keys, layer, reportId), 0, 0, 0, 0, 0, 0]];
            case KeyAction.Keys { Macro: var macro }:
                {
                    var kind = TypeByte(Kind.Keys, layer, reportId);
                    var groups = macro.Strokes.Where(stroke => !stroke.IsModifiersOnly).ToList();
                    if (groups.Count > 5)
                        throw DeviceErrors.Unsupported();
                    var first = macro.Strokes.Count > 0 ? (byte)macro.Strokes[0].Modifiers : (byte)0;
                    var count = (byte)groups.Count;
                    var packets = new List<byte[]> { new byte[] { number, kind, count, 0, first, 0, 0, 0 } };
                    for (var index = 0; index < groups.Count; index++)
                    {
                        var group = groups[index];
                        packets.Add([number, kind, count, (byte)(index + 1), (byte)group.Modifiers, group.Key, 0, 0]);
                    }
                    return packets;
                }
            case KeyAction.Media { Key: var key }:
                {
                    var (low, high) = MediaBytes(key, reportId) ?? throw DeviceErrors.Unsupported();
                    return [[number, TypeByte(Kind.Media, layer, reportId), low, high, 0, 0, 0, 0]];
                }
            case KeyAction.Mouse { Action: var mouse }:
                return
                [
                    [
                        number, TypeByte(Kind.Mouse, layer, reportId), mouse.Buttons(), 0, 0, mouse.Wheel(),
                        (byte)mouse.Modifiers(), 0,
                    ],
                ];
            default:
                throw new UnreachableException();
        }
    }

    /// <summary>
    /// The two bytes of a media key. Report ID 3 takes the consumer usage; the other two take codes of their own,
    /// known for the Windows tool's six keys only.
    /// </summary>
    public static (byte Low, byte High)? MediaBytes(MediaKey key, byte reportId)
    {
        var table = reportId switch
        {
            0 => ReportId0Media,
            2 => ReportId2Media,
            _ => null,
        };
        if (table is null)
            return ((byte)(key.Usage & 0xFF), (byte)(key.Usage >> 8));
        foreach (var entry in table)
        {
            if (entry.Key == key)
                return (entry.Low, entry.High);
        }
        return null;
    }
}
