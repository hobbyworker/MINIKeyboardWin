using System.Runtime.Versioning;
using MINIKeyboard.Domain;

namespace MINIKeyboard.Data;

/// <summary>
/// The keyboard the app talks to, switchable while it runs: the one on USB or a simulated one. A switch is reported as
/// "searching" and then the new keyboard, so the app starts over as after re-plugging. The USB connection is made the
/// first time it is chosen — an app that stays on a simulated keyboard never opens a real one.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class KeyboardSourceSwitch :
    IKeyboardConnectionRepository, IKeyboardSourceRepository, IChannelProvider, IDisposable
{
    private readonly DeviceLogStore log;
    private readonly Dictionary<SimulatedModel, SimulatedKeyboard> simulated = [];
    private readonly Broadcaster<ConnectionState> state = new(ConnectionState.Searching);
    private KeyboardSource source = KeyboardSource.Hardware;
    private HardwareConnection? hardware;

    /// <summary>Whether the hardware connection's states are passed on; read on the connection's thread.</summary>
    private volatile bool followingHardware;

    public KeyboardSourceSwitch(DeviceLogStore log, KeyboardSource initial)
    {
        this.log = log;
        Follow(initial);
        source = initial;
    }

    public KeyboardSource Current => source;

    public IAsyncEnumerable<ConnectionState> ConnectionStates(CancellationToken cancellation = default) =>
        state.Subscribe(cancellation);

    public void Retry()
    {
        if (source is KeyboardSource.HardwareSource)
            hardware?.Retry();
    }

    public void Select(KeyboardSource source)
    {
        if (this.source == source)
            return;
        followingHardware = false;
        state.Set(ConnectionState.Searching);
        Follow(source);
        this.source = source;
    }

    public IReportChannel? OpenChannel() => source switch
    {
        KeyboardSource.Simulated { Model: var model } => simulated.GetValueOrDefault(model),
        _ => hardware?.OpenChannel(),
    };

    public void Dispose() => hardware?.Dispose();

    private void Follow(KeyboardSource next)
    {
        switch (next)
        {
            case KeyboardSource.Simulated { Model: var model }:
                followingHardware = false;
                state.Set(new ConnectionState.Connected(SimulatedFor(model).Keyboard()));
                break;
            default:
                if (hardware is null)
                {
                    hardware = HardwareConnection.Start(log);
                    hardware.MirrorInto(state, () => followingHardware);
                }
                followingHardware = true;
                state.Set(hardware.State);
                break;
        }
    }

    private SimulatedKeyboard SimulatedFor(SimulatedModel model)
    {
        if (!simulated.TryGetValue(model, out var keyboard))
        {
            log.Append(new LogContent.Event(new DeviceLogEvent.Simulated(model.Id)));
            simulated[model] = keyboard = new SimulatedKeyboard(model);
        }
        return keyboard;
    }
}
