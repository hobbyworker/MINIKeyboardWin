using MINIKeyboard.Domain;

namespace MINIKeyboard.Data.Protocols;

/// <summary>
/// The 884x keyboards' requests and answers, as the cross-platform app sends and reads them. Every request is a 64-byte
/// payload after report ID 3; every answer a 64-byte input report, its first byte the report ID.
/// </summary>
public static class K884x
{
    public const byte ReportId = 0x03;

    /// <summary>The report ID and 64 bytes.</summary>
    public const int ReportLength = 65;

    /// <summary><c>FB FB FB</c>: how many keys and knobs, and the type (<c>Widget::Read_KeyBoard_KeyNum</c>).</summary>
    public static ReadOnlySpan<byte> Identify => [0xFB, 0xFB, 0xFB];

    /// <summary><c>FD FE FF</c>: keep what was written. The app waits 200 ms after it.</summary>
    public static ReadOnlySpan<byte> Commit => [0xFD, 0xFE, 0xFF];

    public static readonly TimeSpan CommitPause = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// <c>FA &lt;records&gt; &lt;knob records&gt; &lt;layer + 1&gt;</c> — the standard format reads 25 records, the
    /// legacy one 15 keys and 3 knobs (<c>Read_CurKeyBoard_Data</c>, <c>Read_configuration_clicked</c>).
    /// </summary>
    public static byte[]? ReadRecords(int layer, RecordFormat format)
    {
        var layerByte = LayerByte(layer + 1);
        return format switch
        {
            RecordFormat.Standard => [0xFA, 25, 0, layerByte],
            RecordFormat.Legacy => [0xFA, 15, 3, layerByte],
            _ => null,
        };
    }

    /// <summary>How many answers follow <see cref="ReadRecords"/>, one record each, records 1… in order.</summary>
    public static int ReadReplyCount(RecordFormat format) => format == RecordFormat.Standard ? 25 : 15 + 3 * 3;

    /// <summary>
    /// <c>FA B0 &lt;layer&gt;</c>: one layer's lights (standard format, <c>Read_RgbLed_DataDsp</c>). The layer counts
    /// from 0.
    /// </summary>
    public static byte[] ReadLighting(int layer) => [0xFA, 0xB0, LayerByte(layer)];

    /// <summary>
    /// <c>FE B0 &lt;layer&gt; &lt;mode&gt; &lt;r g b × 16&gt;</c> (standard format). The layer counts from 0.
    /// </summary>
    public static byte[] Lighting(int layer, byte mode, IEnumerable<KeyColor> colors)
    {
        var payload = new List<byte> { 0xFE, 0xB0, LayerByte(layer), mode };
        foreach (var color in colors.Take(LightingSupport.ColorKeyCount))
            payload.AddRange([color.Red, color.Green, color.Blue]);
        return [.. payload];
    }

    // Answers, the report ID first.

    public static KeyboardIdentity? Identity(ReadOnlySpan<byte> reply) =>
        reply.Length >= 5 ? new KeyboardIdentity(reply[2], reply[3], reply[4]) : null;

    /// <summary>A record answer carries the record after the report ID.</summary>
    public static ReadOnlySpan<byte> Record(ReadOnlySpan<byte> reply) => reply.IsEmpty ? [] : reply[1..];

    /// <summary><c>[2]</c> the mode, <c>[3…50]</c> sixteen colours.</summary>
    public static LayerLighting? LightingReply(ReadOnlySpan<byte> reply)
    {
        const int count = LightingSupport.ColorKeyCount;
        if (reply.Length < 3 + 3 * count)
            return null;
        var colors = new KeyColor[count];
        for (var index = 0; index < count; index++)
            colors[index] = new KeyColor(reply[3 + 3 * index], reply[4 + 3 * index], reply[5 + 3 * index]);
        return new LayerLighting.PerKey(reply[2], new(colors));
    }

    // Unidentified keyboards.

    /// <summary>After an <c>FE</c> record: <c>AA AA</c>, <c>FD FE FF</c>, <c>AA AA</c>.</summary>
    public static IReadOnlyList<byte[]> UnidentifiedSave() => [[0xAA, 0xAA], Commit.ToArray(), [0xAA, 0xAA]];

    /// <summary>
    /// Lights of an unidentified keyboard: <c>FE B0 &lt;layer + 1&gt; 08 … 01 00 &lt;code&gt;</c> and the commit, after
    /// the identification and reads the cross-platform app sends first on the same connection — without them the light
    /// is known to change but not to stay.
    /// </summary>
    public static IReadOnlyList<byte[]> UnidentifiedLighting(LightColor color, byte mode, int layer)
    {
        var payloads = new List<byte[]> { new byte[] { 0xFB, 0xFB, 0xFB, 0x01 } };
        for (byte record = 1; record <= 3; record++)
            payloads.Add([0xFA, 0x0F, 0x03, record, 0x7F]);
        payloads.Add(Records.LegacyLighting(color, mode, layer, 12));
        payloads.Add(Commit.ToArray());
        return payloads;
    }

    /// <summary>Layers count from 0 or 1 in a byte; there are at most three.</summary>
    internal static byte LayerByte(int layer) => checked((byte)layer);
}
