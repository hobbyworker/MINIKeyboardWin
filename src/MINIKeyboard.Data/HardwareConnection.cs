using System.Runtime.Versioning;
using MINIKeyboard.Data.Connection;
using MINIKeyboard.Domain;

namespace MINIKeyboard.Data;

/// <summary>
/// The connection to the keyboard on USB: finds a supported keyboard and keeps its configuration collection open,
/// looking again every second as the vendor tools do. Both families are opened through the Windows HID API — the
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
    private readonly ConnectionWatcher watcher;
    private readonly ManualResetEvent stop = new(false);
    private readonly AutoResetEvent retry = new(false);

    private HardwareConnection(DeviceLogStore log) => watcher = new ConnectionWatcher(log, new WindowsHid(), state);

    /// <summary>Starts looking for a keyboard.</summary>
    public static HardwareConnection Start(DeviceLogStore log)
    {
        var connection = new HardwareConnection(log);
        new Thread(connection.Run) { IsBackground = true, Name = "keyboard-connection" }.Start();
        return connection;
    }

    public ConnectionState State => state.Value;

    public IAsyncEnumerable<ConnectionState> ConnectionStates(CancellationToken cancellation = default) =>
        state.Subscribe(cancellation);

    public void Retry() => retry.Set();

    public IReportChannel? OpenChannel() => watcher.OpenChannel();

    public void Dispose() => stop.Set();

    /// <summary>The simulator's switch follows the connection through this.</summary>
    internal void MirrorInto(Broadcaster<ConnectionState> target, Func<bool> active) =>
        watcher.MirrorInto(target, active);

    private void Run()
    {
        WaitHandle[] signals = [stop, retry];
        watcher.Refresh();
        while (true)
        {
            var signal = WaitHandle.WaitAny(signals, Poll);
            if (signal == 0)
                break;
            if (signal == 1)
                watcher.Retry();
            else
                watcher.Refresh();
        }
        watcher.Disconnect();
    }
}
