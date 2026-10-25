namespace MINIKeyboard.Presentation;

/// <summary>Bytes as the device log shows them, in hex.</summary>
internal static class PacketText
{
    /// <summary>"03 FB FB FB" — the zero padding a report ends with left out, keeping its first byte.</summary>
    public static string Summary(IReadOnlyList<byte> bytes) => Hex(bytes.Take(SummaryCount(bytes)));

    /// <summary>How many bytes the summary shows.</summary>
    public static int SummaryCount(IReadOnlyList<byte> bytes)
    {
        var end = bytes.Count;
        while (end > 1 && bytes[end - 1] == 0)
            end--;
        return end;
    }

    /// <summary>Every byte, 16 to a row after the row's offset: "0010  00 00 00 …".</summary>
    public static IReadOnlyList<string> Rows(IReadOnlyList<byte> bytes) =>
        bytes.Chunk(16).Select((chunk, row) => $"{row * 16:X4}  {Hex(chunk)}").ToArray();

    public static string Hex(IEnumerable<byte> bytes) => string.Join(" ", bytes.Select(value => value.ToString("X2")));
}
