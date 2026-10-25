using MINIKeyboard.Domain;
using MINIKeyboard.Domain.UseCases;

namespace MINIKeyboard.Presentation;

/// <summary>Which way a line of the log went.</summary>
public enum LogDirection
{
    Output,
    Input,
    Event,
}

/// <summary>What happened to the connection; the window names it.</summary>
public abstract record LogEvent
{
    private LogEvent() { }

    public sealed record Searching : LogEvent;

    public sealed record Connected(string Usb, byte Interface) : LogEvent;

    public sealed record NeedsPermission : LogEvent;

    public sealed record OpenFailed(int Code) : LogEvent;

    public sealed record WriteFailed(int Code) : LogEvent;

    public sealed record Simulated(string Id) : LogEvent;
}

/// <summary>One line of the device log window.</summary>
/// <param name="Bytes">Every byte of the report written or read; empty for an event.</param>
public sealed record LogLine(
    ulong Id, DateTimeOffset Time, LogDirection Direction, EquatableList<byte> Bytes, LogEvent? Event)
{
    /// <summary>The report up to the zero padding at its end: "03 FB FB FB".</summary>
    public string Summary() => PacketText.Summary(Bytes);

    /// <summary>Whether <see cref="Summary"/> leaves bytes out.</summary>
    public bool IsShortened => PacketText.SummaryCount(Bytes) < Bytes.Count;

    /// <summary>Every byte, 16 to a row after the row's offset: "0010  00 00 00 …".</summary>
    public IReadOnlyList<string> Rows() => PacketText.Rows(Bytes);

    /// <summary>Every byte on one line.</summary>
    public string Hex() => PacketText.Hex(Bytes);

    internal static LogLine Of(DeviceLogEntry entry) => entry.Content switch
    {
        LogContent.Output output => new LogLine(entry.Id, entry.Time, LogDirection.Output, [.. output.Bytes], null),
        LogContent.Input input => new LogLine(entry.Id, entry.Time, LogDirection.Input, [.. input.Bytes], null),
        LogContent.Event { What: var what } => new LogLine(entry.Id, entry.Time, LogDirection.Event, [], what switch
        {
            DeviceLogEvent.Connected connected =>
                new LogEvent.Connected(connected.Usb.ToString(), connected.Interface),
            DeviceLogEvent.NeedsPermission => new LogEvent.NeedsPermission(),
            DeviceLogEvent.OpenFailed failed => new LogEvent.OpenFailed(failed.Code),
            DeviceLogEvent.WriteFailed failed => new LogEvent.WriteFailed(failed.Code),
            DeviceLogEvent.Simulated simulated => new LogEvent.Simulated(simulated.ModelId),
            _ => new LogEvent.Searching(),
        }),
        _ => throw new ArgumentOutOfRangeException(nameof(entry)),
    };
}

/// <summary>
/// The device log window. It lives on the interface's thread; <see cref="Changed"/> says when the lines changed, and
/// the window reads them again.
/// </summary>
public sealed class DeviceLogModel(DeviceLogUseCase log)
{
    private IReadOnlyList<LogLine> lines = [];

    /// <summary>Raised after every change.</summary>
    public event EventHandler? Changed;

    public IReadOnlyList<LogLine> Lines => lines;

    public void Clear() => log.Clear();

    /// <summary>Follows the log until <paramref name="cancellation"/> is cancelled.</summary>
    public async Task RunAsync(CancellationToken cancellation = default)
    {
        try
        {
            await foreach (var entries in log.Entries(cancellation))
            {
                lines = [.. entries.Select(LogLine.Of)];
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
    }

    public override string ToString() => $"DeviceLogModel {{ Lines = {lines.Count} }}";
}
