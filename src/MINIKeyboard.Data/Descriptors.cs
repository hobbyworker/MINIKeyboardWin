using MINIKeyboard.Domain;

namespace MINIKeyboard.Data;

public readonly record struct UsbEndpoint(byte Address, ushort MaxPacketSize);

/// <summary>One interface of a configuration descriptor, alternate setting 0.</summary>
/// <param name="OutEndpoints">Every interrupt OUT endpoint, in descriptor order.</param>
/// <param name="ReportDescriptorLength">From the interface's HID descriptor.</param>
public sealed record UsbInterface(EquatableArray<UsbEndpoint> OutEndpoints, ushort? ReportDescriptorLength)
{
    public UsbEndpoint? OutEndpoint => OutEndpoints.IsEmpty ? null : OutEndpoints[0];
}

/// <summary>The output report a HID report descriptor declares.</summary>
/// <param name="Id">0 when the descriptor numbers no reports.</param>
/// <param name="Length">In bytes, without the report ID.</param>
public readonly record struct OutputReport(byte Id, int Length);

/// <summary>
/// What writing to the keyboard needs from its USB descriptors, parsed from the raw bytes. On Windows the HID API tells
/// the connection the same from the descriptors it parsed; these read the bytes themselves.
/// </summary>
public static class Descriptors
{
    /// <summary>
    /// Interface <paramref name="number"/> (alternate setting 0) in a configuration descriptor, or null when the
    /// configuration has no such interface.
    /// </summary>
    public static UsbInterface? Interface(byte number, ReadOnlySpan<byte> configuration)
    {
        List<UsbEndpoint>? endpoints = null;
        ushort? reportDescriptorLength = null;
        var inside = false;
        var offset = 0;
        while (offset + 2 <= configuration.Length)
        {
            int length = configuration[offset];
            if (length < 2 || offset + length > configuration.Length)
                break;
            var descriptor = configuration.Slice(offset, length);
            switch (descriptor[1])
            {
                // Interface
                case 0x04 when length >= 9:
                    inside = descriptor[2] == number && descriptor[3] == 0;
                    if (inside)
                    {
                        endpoints = [];
                        reportDescriptorLength = null;
                    }
                    break;
                // HID: the first class descriptor is the report descriptor.
                case 0x21 when inside && length >= 9:
                    if (descriptor[6] == 0x22 && endpoints is not null)
                        reportDescriptorLength = (ushort)(descriptor[7] | descriptor[8] << 8);
                    break;
                // Endpoint
                case 0x05 when inside && length >= 7:
                    var address = descriptor[2];
                    var isInterrupt = (descriptor[3] & 0x03) == 0x03;
                    if (isInterrupt && (address & 0x80) == 0 && endpoints is not null)
                    {
                        var maxPacketSize = (ushort)((descriptor[4] | descriptor[5] << 8) & 0x7FF);
                        endpoints.Add(new UsbEndpoint(address, maxPacketSize));
                    }
                    break;
            }
            offset += length;
        }
        return endpoints is null ? null : new UsbInterface(new(endpoints), reportDescriptorLength);
    }

    /// <summary>
    /// The first output report in a HID report descriptor, with its whole length (every Output item of that ID).
    /// </summary>
    public static OutputReport? FirstOutputReport(ReadOnlySpan<byte> reportDescriptor)
    {
        long size = 0;
        long count = 0;
        byte id = 0;
        var stack = new Stack<(long Size, long Count, byte Id)>();
        byte? first = null;
        long bits = 0;
        foreach (var item in Items(reportDescriptor))
        {
            switch (item.Kind, item.Tag)
            {
                case (1, 0x7):
                    size = item.Value;
                    break;
                case (1, 0x8):
                    id = (byte)item.Value;
                    break;
                case (1, 0x9):
                    count = item.Value;
                    break;
                // Push, Pop
                case (1, 0xA):
                    stack.Push((size, count, id));
                    break;
                case (1, 0xB):
                    if (stack.TryPop(out var saved))
                        (size, count, id) = saved;
                    break;
                // Output
                case (0, 0x9):
                    first ??= id;
                    if (id == first)
                        bits += size * count;
                    break;
            }
        }
        if (first is not { } firstId || bits <= 0)
            return null;
        return new OutputReport(firstId, (int)((bits + 7) / 8));
    }

    /// <summary>The first usage page the descriptor names — its top collection's. Vendor-defined from 0xFF00.</summary>
    public static uint? UsagePage(ReadOnlySpan<byte> reportDescriptor)
    {
        foreach (var item in Items(reportDescriptor))
        {
            if (item.Kind == 1 && item.Tag == 0x0)
                return item.Value;
        }
        return null;
    }

    /// <summary>The (usage page, usage) of each top-level collection — what an interface is for.</summary>
    public static EquatableArray<UsagePair> TopLevelUsages(ReadOnlySpan<byte> reportDescriptor)
    {
        var pairs = new List<UsagePair>();
        uint page = 0;
        uint? usage = null;
        var depth = 0;
        foreach (var item in Items(reportDescriptor))
        {
            switch (item.Kind, item.Tag)
            {
                case (1, 0x0):
                    page = item.Value;
                    break;
                case (2, 0x0):
                    usage ??= item.Value;
                    break;
                // Collection
                case (0, 0xA):
                    if (depth == 0 && usage is { } found)
                    {
                        // A four-byte usage carries its own page.
                        var pair = found > 0xFFFF
                            ? new UsagePair((ushort)(found >> 16), (ushort)(found & 0xFFFF))
                            : new UsagePair((ushort)page, (ushort)found);
                        if (!pairs.Contains(pair))
                            pairs.Add(pair);
                    }
                    depth++;
                    usage = null;
                    break;
                // End Collection
                case (0, 0xC):
                    depth = Math.Max(depth - 1, 0);
                    usage = null;
                    break;
                case (0, _):
                    usage = null;
                    break;
            }
        }
        return new(pairs);
    }

    /// <summary>A short item: its type (0 main, 1 global, 2 local), tag and value.</summary>
    private readonly record struct Item(byte Kind, byte Tag, uint Value);

    /// <summary>Every short item, until the bytes run out. Long items are skipped.</summary>
    private static List<Item> Items(ReadOnlySpan<byte> bytes)
    {
        var items = new List<Item>();
        var offset = 0;
        while (offset < bytes.Length)
        {
            var prefix = bytes[offset];
            if (prefix == 0xFE)
            {
                // Long item: data size, tag, data.
                if (offset + 1 >= bytes.Length)
                    break;
                offset += 3 + bytes[offset + 1];
                continue;
            }
            var size = (prefix & 0x03) switch
            {
                0 => 0,
                1 => 1,
                2 => 2,
                _ => 4,
            };
            if (offset + 1 + size > bytes.Length)
                break;
            uint value = 0;
            for (var index = size; index >= 1; index--)
                value = value << 8 | bytes[offset + index];
            items.Add(new Item((byte)((prefix >> 2) & 0x03), (byte)(prefix >> 4), value));
            offset += 1 + size;
        }
        return items;
    }
}
