using System.Collections.Concurrent;
using System.Runtime.Versioning;
using MINIKeyboard.Data.Connection;
using MINIKeyboard.Domain;

namespace MINIKeyboard.Data;

/// <summary>
/// The connection to the keyboard on USB: finds the supported keyboards plugged in and keeps the chosen one's
/// configuration collection open, looking again every second as the vendor tools do. Both families are opened through the Windows HID API — the
/// 1189:8890's interface 1 as its Windows tool opens it, a 884x keyboard's interface 0 as the cross-platform app does.
/// </summary>
/// <remarks>
/// The looking happens on a thread of its own; the state reaches the interface's thread through
/// <see cref="ConnectionStates"/>. Disposing stops looking and lets go of the keyboard.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class HardwareConnection : IKeyboardConnectionRepository, IChannelProvider, IDisposable
{
    /// <summary>How often the keyboards plugged in are looked at.</summary>
    private static readonly TimeSpan Poll = TimeSpan.FromSeconds(1);

    private readonly Broadcaster<ConnectionState> state = new(ConnectionState.Searching);
    private readonly Broadcaster<AttachedKeyboards> attached = new(AttachedKeyboards.None);
    private readonly ConnectionWatcher watcher;
    private readonly ManualResetEvent stop = new(false);
    private readonly AutoResetEvent retry = new(false);
    private readonly AutoResetEvent select = new(false);
    private readonly ConcurrentQueue<string> selections = new();

    private HardwareConnection(DeviceLogStore log, string selectedKeyboard) =>
        watcher = new ConnectionWatcher(log, new WindowsHid(), state, attached, selectedKeyboard);

    /// <summary>
    /// Starts looking for keyboards. The one chosen is kept in <paramref name="selectedKeyboard"/>, to be opened again
    /// next time.
    /// </summary>
    public static HardwareConnection Start(DeviceLogStore log, string selectedKeyboard)
    {
        var connection = new HardwareConnection(log, selectedKeyboard);
        new Thread(connection.Run) { IsBackground = true, Name = "keyboard-connection" }.Start();
        return connection;
    }

    public ConnectionState State => state.Value;

    public AttachedKeyboards Attached => attached.Value;

    public IAsyncEnumerable<ConnectionState> ConnectionStates(CancellationToken cancellation = default) =>
        state.Subscribe(cancellation);

    public void Retry() => retry.Set();

    public IAsyncEnumerable<AttachedKeyboards> AttachedKeyboardUpdates(CancellationToken cancellation = default) =>
        attached.Subscribe(cancellation);

    public void SelectKeyboard(string id)
    {
        selections.Enqueue(id);
        select.Set();
    }

    public IReportChannel? OpenChannel() => watcher.OpenChannel();

    public void Dispose() => stop.Set();

    /// <summary>The simulator's switch follows the connection through this.</summary>
    internal void MirrorInto(Mirror mirror) => watcher.MirrorInto(mirror);

    private void Run()
    {
        WaitHandle[] signals = [stop, retry, select];
        watcher.Refresh();
        while (true)
        {
            var signal = WaitHandle.WaitAny(signals, Poll);
            if (signal == 0)
                break;
            if (signal == 1)
                watcher.Retry();
            else if (signal == 2)
            {
                while (selections.TryDequeue(out var id))
                    watcher.Select(id);
            }
            else
                watcher.Refresh();
        }
        watcher.CloseChannel();
    }
}
