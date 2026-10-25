using MINIKeyboard.Data.Protocols;
using MINIKeyboard.Domain;

namespace MINIKeyboard.Data;

/// <summary>Waits the given time; tests wait none.</summary>
public delegate Task Pause(TimeSpan duration, CancellationToken cancellation);

/// <summary>Talks to whichever keyboard the channel provider has open, in the commands it takes.</summary>
public sealed class KeyboardDevice : IKeyboardDeviceRepository
{
    private readonly IChannelProvider channels;
    private readonly DeviceLogStore log;
    private readonly TimeSpan identifyTimeout;
    private readonly TimeSpan replyTimeout;
    private readonly Pause pause;

    /// <summary>
    /// Whether a lighting save on the 1189:8890 ends with the save that stops its key repeat — asked at every write, so
    /// a change in Settings applies at once (K8890.DownloadLighting).
    /// </summary>
    public Func<bool> PreventsLightingKeyRepeat { get; init; } = () => true;

    /// <summary>
    /// The cross-platform app waits 10 ms for the identification answer; here a slow keyboard gets 600 ms, and every
    /// other answer a second.
    /// </summary>
    public KeyboardDevice(IChannelProvider channels, DeviceLogStore log)
        : this(channels, log, TimeSpan.FromMilliseconds(600), TimeSpan.FromSeconds(1), Task.Delay)
    {
    }

    public KeyboardDevice(
        IChannelProvider channels, DeviceLogStore log, TimeSpan identifyTimeout, TimeSpan replyTimeout, Pause pause)
    {
        this.channels = channels;
        this.log = log;
        this.identifyTimeout = identifyTimeout;
        this.replyTimeout = replyTimeout;
        this.pause = pause;
    }

    public async Task<KeyboardIdentity?> AskIdentityAsync(CancellationToken cancellation = default)
    {
        var channel = CurrentChannel();
        channel.DiscardPendingReplies();
        await SendAsync(K884x.Identify.ToArray(), channel, cancellation);
        if (await ReplyAsync(channel, identifyTimeout, cancellation) is not { } reply)
            return null;
        return K884x.Identity(reply) ?? throw Failure(new KeyboardDeviceError.InvalidReply());
    }

    public async Task<IReadOnlyDictionary<ControlId, KeyAction>> ReadActionsAsync(
        int layer, RecordFormat format, KeyboardLayout layout, CancellationToken cancellation = default)
    {
        var request = K884x.ReadRecords(layer, format) ?? throw Failure(new KeyboardDeviceError.Unsupported());
        var channel = CurrentChannel();
        channel.DiscardPendingReplies();
        await SendAsync(request, channel, cancellation);
        var actions = new Dictionary<ControlId, KeyAction>();
        var count = K884x.ReadReplyCount(format);
        for (var recordId = 1; recordId <= count; recordId++)
        {
            var reply = await ReplyAsync(channel, replyTimeout, cancellation)
                ?? throw Failure(new KeyboardDeviceError.NoReply());
            if (Numbers.ControlInRecord(checked((byte)recordId), layout) is { } control
                && Records.Decode(K884x.Record(reply), format) is { } action)
            {
                actions[control] = action;
            }
        }
        return actions;
    }

    public async Task<LayerLighting> ReadLightingAsync(int layer, CancellationToken cancellation = default)
    {
        var channel = CurrentChannel();
        channel.DiscardPendingReplies();
        await SendAsync(K884x.ReadLighting(layer), channel, cancellation);
        var reply = await ReplyAsync(channel, replyTimeout, cancellation)
            ?? throw Failure(new KeyboardDeviceError.NoReply());
        return K884x.LightingReply(reply) ?? throw Failure(new KeyboardDeviceError.InvalidReply());
    }

    public async Task WriteAsync(
        KeyboardConfiguration configuration, CommandSet commands, KeyboardLayout layout,
        CancellationToken cancellation = default)
    {
        var plan = new WritePlan(configuration, commands, layout, PreventsLightingKeyRepeat());
        var channel = CurrentChannel();
        foreach (var step in plan.Steps)
        {
            await SendAsync(step.Payload, channel, cancellation);
            if (step.PauseAfter > TimeSpan.Zero)
                await pause(step.PauseAfter, cancellation);
        }
    }

    private IReportChannel CurrentChannel() =>
        channels.OpenChannel() ?? throw Failure(new KeyboardDeviceError.NotConnected());

    private async Task SendAsync(byte[] payload, IReportChannel channel, CancellationToken cancellation)
    {
        var report = channel.Framing.Frame(payload);
        log.Append(new LogContent.Output(new(report)));
        try
        {
            await channel.WriteAsync(report, cancellation);
        }
        catch (KeyboardDeviceException exception) when (exception.Error is KeyboardDeviceError.WriteFailed failed)
        {
            log.Append(new LogContent.Event(new DeviceLogEvent.WriteFailed(failed.Code)));
            throw;
        }
    }

    private async Task<byte[]?> ReplyAsync(IReportChannel channel, TimeSpan timeout, CancellationToken cancellation)
    {
        if (await channel.NextReplyAsync(timeout, cancellation) is not { } reply)
            return null;
        log.Append(new LogContent.Input(new(reply)));
        return reply;
    }

    private static KeyboardDeviceException Failure(KeyboardDeviceError error) => new(error);
}
