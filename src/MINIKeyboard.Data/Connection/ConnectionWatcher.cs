using MINIKeyboard.Domain;

namespace MINIKeyboard.Data.Connection;

/// <summary>
/// The connection's work, done on its thread: looks at the keyboards plugged in, opens the chosen one's configuration
/// collection (<see cref="KeyboardChoice"/>), and lets go when it is unplugged — or plugged in again since it last
/// looked, which opens it anew (<see cref="HidCandidate.Entry"/>). A keyboard that didn't open is left alone — and its
/// failure stays on screen — until it is plugged in again or the user retries. The others are left alone too, so
/// another app can have them.
/// </summary>
/// <param name="selectedKeyboard">Where the keyboard chosen last time is kept; null keeps nothing.</param>
internal sealed class ConnectionWatcher(
    DeviceLogStore log, IHidSystem hid, Broadcaster<ConnectionState> state, Broadcaster<AttachedKeyboards> attached,
    string? selectedKeyboard)
{
    private readonly Lock gate = new();
    private readonly List<Mirror> mirrors = [];
    private readonly HashSet<string> failed = new(StringComparer.Ordinal);
    private IoChannel? open;
    private string? connected;

    /// <summary>The keyboard <see cref="connected"/> is on.</summary>
    private string? connectedKeyboard;

    /// <summary>The one the user chose this session; null once it is unplugged — the app doesn't go back to it by itself.</summary>
    private string? chosen;

    /// <summary>The open channel, from any thread.</summary>
    public IReportChannel? OpenChannel()
    {
        lock (gate)
            return open;
    }

    /// <summary>Other broadcasters take the same values while <see cref="Mirror.Active"/> says so.</summary>
    public void MirrorInto(Mirror mirror)
    {
        lock (gate)
            mirrors.Add(mirror);
    }

    public void Retry()
    {
        failed.Clear();
        if (connected is null)
            SetState(ConnectionState.Searching);
        Refresh();
    }

    public void Select(string id)
    {
        if (attached.Value.Keyboards.FirstOrDefault(keyboard => keyboard.Id == id) is not { } keyboard)
            return;
        chosen = id;
        Remember(keyboard);
        if (connectedKeyboard == id)
            return;
        CloseChannel();
        failed.Clear();
        SetState(ConnectionState.Searching);
        Refresh();
    }

    public void Refresh()
    {
        var candidates = HidCollections.Candidates(hid.Collections());
        var present = candidates.Select(candidate => candidate.Entry).ToHashSet(StringComparer.Ordinal);
        failed.IntersectWith(present);
        var keyboards = KeyboardChoice.Ordered(candidates.Select(candidate => candidate.Keyboard));
        if (chosen is not null && keyboards.All(keyboard => keyboard.Id != chosen))
            chosen = null;

        if (connected is not null)
        {
            if (present.Contains(connected))
            {
                Publish(keyboards, connectedKeyboard);
                return;
            }
            CloseChannel();
            SetState(ConnectionState.Searching);
        }
        HidCandidate? Usable(AttachedKeyboard keyboard) => candidates.FirstOrDefault(
            candidate => candidate.Keyboard.Id == keyboard.Id && !failed.Contains(candidate.Entry));
        var target = KeyboardChoice.Target(keyboards, chosen, Remembered(), keyboard => Usable(keyboard) is not null);
        Publish(keyboards, target?.Id);
        if (target is null || Usable(target) is not { } usable)
        {
            // A failure stays on screen while that keyboard is still plugged in.
            var failing = state.Value is ConnectionState.NeedsPermission or ConnectionState.OpenFailed;
            if (!(failing && present.Count > 0))
                SetState(ConnectionState.Searching);
            return;
        }
        Open(usable);
    }

    /// <summary>Lets go of the keyboard.</summary>
    public void CloseChannel()
    {
        IoChannel? channel;
        lock (gate)
        {
            channel = open;
            open = null;
        }
        channel?.Close();
        connected = null;
        connectedKeyboard = null;
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
        connectedKeyboard = candidate.Keyboard.Id;
        SetState(new ConnectionState.Connected(keyboard));
    }

    private string? Remembered()
    {
        if (selectedKeyboard is null)
            return null;
        try
        {
            return File.ReadAllText(selectedKeyboard).Trim();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>A write that fails is dropped — the first keyboard opens next time.</summary>
    private void Remember(AttachedKeyboard keyboard)
    {
        if (selectedKeyboard is null)
            return;
        try
        {
            if (Path.GetDirectoryName(selectedKeyboard) is { Length: > 0 } directory)
                Directory.CreateDirectory(directory);
            File.WriteAllText(selectedKeyboard, KeyboardChoice.Key(keyboard));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
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
            foreach (var mirror in mirrors)
            {
                if (mirror.Active())
                    mirror.State.Set(next);
            }
        }
    }

    private void Publish(IReadOnlyList<AttachedKeyboard> keyboards, string? selected)
    {
        var next = new AttachedKeyboards([.. keyboards], selected);
        attached.Set(next);
        lock (gate)
        {
            foreach (var mirror in mirrors)
            {
                if (mirror.Active())
                    mirror.Attached.Set(next);
            }
        }
    }
}

/// <summary>Other broadcasters that take the connection's values while <paramref name="Active"/> says so.</summary>
internal sealed record Mirror(
    Broadcaster<ConnectionState> State, Broadcaster<AttachedKeyboards> Attached, Func<bool> Active);
