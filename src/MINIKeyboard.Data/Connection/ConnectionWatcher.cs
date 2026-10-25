using MINIKeyboard.Domain;

namespace MINIKeyboard.Data.Connection;

/// <summary>
/// The connection's work, done on its thread: looks at the keyboards plugged in, opens the first one's configuration
/// collection, and lets go when it is unplugged. A keyboard that didn't open is left alone — and its failure stays on
/// screen — until it is plugged in again or the user retries.
/// </summary>
internal sealed class ConnectionWatcher(DeviceLogStore log, IHidSystem hid, Broadcaster<ConnectionState> state)
{
    private readonly Lock gate = new();
    private readonly List<(Broadcaster<ConnectionState> Target, Func<bool> Active)> mirrors = [];
    private readonly HashSet<string> failed = new(StringComparer.Ordinal);
    private IoChannel? open;
    private string? connected;

    /// <summary>The open channel, from any thread.</summary>
    public IReportChannel? OpenChannel()
    {
        lock (gate)
            return open;
    }

    /// <summary>Another broadcaster takes the same states while <paramref name="active"/> says so.</summary>
    public void MirrorInto(Broadcaster<ConnectionState> target, Func<bool> active)
    {
        lock (gate)
            mirrors.Add((target, active));
    }

    public void Retry()
    {
        failed.Clear();
        if (connected is null)
            SetState(ConnectionState.Searching);
        Refresh();
    }

    public void Refresh()
    {
        var candidates = HidCollections.Candidates(hid.Collections());
        var present = candidates.Select(candidate => candidate.Entry).ToHashSet(StringComparer.Ordinal);
        failed.IntersectWith(present);

        if (connected is not null)
        {
            if (present.Contains(connected))
                return;
            Disconnect();
        }
        if (candidates.FirstOrDefault(candidate => !failed.Contains(candidate.Entry)) is not { } usable)
        {
            // A failure stays on screen while that keyboard is still plugged in.
            var failing = state.Value is ConnectionState.NeedsPermission or ConnectionState.OpenFailed;
            if (!(failing && present.Count > 0))
                SetState(ConnectionState.Searching);
            return;
        }
        Open(usable);
    }

    public void Disconnect()
    {
        IoChannel? channel;
        lock (gate)
        {
            channel = open;
            open = null;
        }
        channel?.Close();
        if (connected is not null)
        {
            connected = null;
            SetState(ConnectionState.Searching);
        }
    }

    private void Open(HidCandidate candidate)
    {
        var collection = candidate.Collection;
        var framing = HidCollections.Framing(collection);
        if (!hid.TryOpen(collection, framing, HidCollections.ReadsAnswers(candidate), out var port, out var error))
        {
            failed.Add(candidate.Entry);
            SetState(new ConnectionState.OpenFailed(error));
            return;
        }
        var keyboard = new ConnectedKeyboard(
            collection.Usb,
            candidate.Family,
            collection.InterfaceNumber,
            Channel.Hid,
            framing.ReportId,
            framing.Length,
            HidCollections.Details(candidate, port.Strings()));
        lock (gate)
            open = IoChannel.Start(port, framing);
        connected = candidate.Entry;
        SetState(new ConnectionState.Connected(keyboard));
    }

    private void SetState(ConnectionState next)
    {
        if (next == state.Value)
            return;
        DeviceLogEvent what = next switch
        {
            ConnectionState.Connected { Device: var keyboard } =>
                new DeviceLogEvent.Connected(keyboard.Usb, keyboard.InterfaceNumber),
            ConnectionState.NeedsPermission => new DeviceLogEvent.NeedsPermission(),
            ConnectionState.OpenFailed { Code: var code } => new DeviceLogEvent.OpenFailed(code),
            _ => new DeviceLogEvent.Searching(),
        };
        log.Append(new LogContent.Event(what));
        state.Set(next);
        lock (gate)
        {
            foreach (var (target, active) in mirrors)
            {
                if (active())
                    target.Set(next);
            }
        }
    }
}
