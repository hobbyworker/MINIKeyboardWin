namespace MINIKeyboard.Data;

// The open configuration interface, as the device repository uses it.

/// <summary>
/// How payloads become reports on a keyboard's configuration interface: the report ID first unless it is 0, then the
/// payload, zero-padded to the report's length.
/// </summary>
/// <param name="Length">Bytes per report, the report ID included.</param>
public readonly record struct ReportFraming(byte ReportId, int Length)
{
    public byte[] Frame(ReadOnlySpan<byte> payload)
    {
        var prefix = ReportId != 0 ? 1 : 0;
        var bytes = new byte[Math.Max(Length, prefix + payload.Length)];
        if (prefix == 1)
            bytes[0] = ReportId;
        payload.CopyTo(bytes.AsSpan(prefix));
        return bytes;
    }
}

/// <summary>The open configuration interface: reports out, and the keyboard's answers in.</summary>
public interface IReportChannel
{
    ReportFraming Framing { get; }

    /// <summary>
    /// Writes one report, as framed. One that doesn't go out throws a <c>KeyboardDeviceException</c>.
    /// </summary>
    Task WriteAsync(byte[] report, CancellationToken cancellation = default);

    /// <summary>Drops answers that came before a new request.</summary>
    void DiscardPendingReplies();

    /// <summary>The next answer, its report ID first, or null when none comes in time.</summary>
    Task<byte[]?> NextReplyAsync(TimeSpan timeout, CancellationToken cancellation = default);
}

/// <summary>Whoever has the channel open — the connection to the keyboard on USB, or the simulator.</summary>
public interface IChannelProvider
{
    IReportChannel? OpenChannel();
}
