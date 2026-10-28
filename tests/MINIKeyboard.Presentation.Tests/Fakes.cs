using System.Runtime.CompilerServices;
using MINIKeyboard.Domain;
using MINIKeyboard.Domain.UseCases;
using Channels = System.Threading.Channels;

namespace MINIKeyboard.Presentation.Tests;

/// <summary>
/// A keyboard that is plugged in when told — and the keyboards plugged in, and the ones chosen. <see cref="Idle"/>
/// completes once the subscriber has taken every state and asks for the next — the editor has finished with what it was
/// sent; <see cref="KeyboardsIdle"/> likewise for the keyboards plugged in.
/// </summary>
internal sealed class FakeConnection : IKeyboardConnectionRepository
{
    private readonly Feed<ConnectionState> states = new();
    private readonly Feed<AttachedKeyboards> keyboards = new();

    public FakeConnection() => states.Send(ConnectionState.Searching);

    public Task Idle => states.Idle;

    public Task KeyboardsIdle => keyboards.Idle;

    public List<string> Selected { get; } = [];

    public void Send(ConnectionState state) => states.Send(state);

    public void Send(AttachedKeyboards attached) => keyboards.Send(attached);

    public IAsyncEnumerable<ConnectionState> ConnectionStates(CancellationToken cancellation = default) =>
        states.Subscribe(cancellation);

    public void Retry() { }

    public IAsyncEnumerable<AttachedKeyboards> AttachedKeyboardUpdates(CancellationToken cancellation = default) =>
        keyboards.Subscribe(cancellation);

    public void SelectKeyboard(string id) => Selected.Add(id);

    /// <summary>Values sent when told, to one subscriber.</summary>
    private sealed class Feed<T>
    {
        private readonly Channels.Channel<T> values = Channels.Channel.CreateUnbounded<T>();
        private readonly Lock gate = new();
        private TaskCompletionSource idle = NewIdle();
        private bool subscribed;

        public Task Idle
        {
            get
            {
                lock (gate)
                    return idle.Task;
            }
        }

        public void Send(T value)
        {
            lock (gate)
                idle = NewIdle();
            values.Writer.TryWrite(value);
        }

        public async IAsyncEnumerable<T> Subscribe([EnumeratorCancellation] CancellationToken cancellation = default)
        {
            Assert.False(subscribed, "one subscriber");
            subscribed = true;
            while (true)
            {
                if (!values.Reader.TryRead(out var value))
                {
                    lock (gate)
                        idle.TrySetResult();
                    value = await values.Reader.ReadAsync(cancellation);
                }
                yield return value;
            }
        }

        private static TaskCompletionSource NewIdle() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

/// <summary>A keyboard whose reads and writes can be held (<see cref="Hold"/>) until the test lets them go.</summary>
internal sealed class FakeDevice : IKeyboardDeviceRepository
{
    private Task gate = Task.CompletedTask;

    public KeyboardIdentity? Identity { get; set; }

    public Dictionary<ControlId, KeyAction> Actions { get; set; } = [];

    public KeyboardDeviceError? FailWrite { get; set; }

    public List<KeyboardConfiguration> Writes { get; } = [];

    public int Reads { get; private set; }

    public Task<KeyboardIdentity?> AskIdentityAsync(CancellationToken cancellation = default) =>
        Task.FromResult(Identity);

    /// <summary>
    /// Holds each read and write from now on until what this returns completes; each reads <see cref="Actions"/> as
    /// they are when it is let go.
    /// </summary>
    public TaskCompletionSource Hold()
    {
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        gate = held.Task;
        return held;
    }

    public async Task<IReadOnlyDictionary<ControlId, KeyAction>> ReadActionsAsync(
        int layer, RecordFormat format, KeyboardLayout layout, CancellationToken cancellation = default)
    {
        Reads++;
        await gate.WaitAsync(cancellation);
        return new Dictionary<ControlId, KeyAction>(Actions);
    }

    public Task<LayerLighting> ReadLightingAsync(int layer, CancellationToken cancellation = default) =>
        Task.FromResult<LayerLighting>(new LayerLighting.PerKey(0, KeyColor.DefaultKeyColors));

    public async Task WriteAsync(
        KeyboardConfiguration configuration, CommandSet commands, KeyboardLayout layout,
        CancellationToken cancellation = default)
    {
        await gate.WaitAsync(cancellation);
        if (FailWrite is { } error)
            throw new KeyboardDeviceException(error);
        Writes.Add(configuration.Clone());
    }
}

/// <summary>The simulator's switch, which only keeps what was chosen.</summary>
internal sealed class FakeSources : IKeyboardSourceRepository
{
    public KeyboardSource Current { get; private set; } = KeyboardSource.Hardware;

    public List<KeyboardSource> Selected { get; } = [];

    public void Select(KeyboardSource source)
    {
        Selected.Add(source);
        Current = source;
    }
}

internal sealed class Profiles : ILayoutProfileRepository
{
    public Dictionary<UsbIdentity, LayoutProfile> Saved { get; } = [];

    public LayoutProfile? Profile(UsbIdentity keyboard) => Saved.GetValueOrDefault(keyboard);

    public void Save(LayoutProfile profile, UsbIdentity keyboard) => Saved[keyboard] = profile;
}

/// <summary>The editor on fakes, following the connection until the test ends.</summary>
internal sealed class Fixture : IAsyncDisposable
{
    private readonly CancellationTokenSource stop;
    private readonly Task run;

    private Fixture(
        FakeConnection connection, FakeDevice device, Profiles profiles, FakeSources sources, EditorModel editor)
    {
        Connection = connection;
        Device = device;
        Profiles = profiles;
        Sources = sources;
        Editor = editor;
        stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        run = editor.RunAsync(stop.Token);
    }

    public FakeConnection Connection { get; }

    public FakeDevice Device { get; }

    public Profiles Profiles { get; }

    public FakeSources Sources { get; }

    public EditorModel Editor { get; }

    /// <summary>A fixture whose device is set up by <paramref name="setUp"/> before the editor starts.</summary>
    public static async Task<Fixture> CreateAsync(Action<FakeDevice>? setUp = null)
    {
        var connection = new FakeConnection();
        var device = new FakeDevice();
        setUp?.Invoke(device);
        var profiles = new Profiles();
        var sources = new FakeSources();
        var editor = new EditorModel(new EditorDependencies
        {
            ObserveConnection = new ObserveConnectionUseCase(connection),
            RetryConnection = new RetryConnectionUseCase(connection),
            IdentifyKeyboard = new IdentifyKeyboardUseCase(device, 1),
            ResolveLayout = new ResolveLayoutUseCase(profiles),
            ChooseLayout = new ChooseLayoutUseCase(profiles),
            ReadKeyboard = new ReadKeyboardUseCase(device),
            SaveKeyboard = new SaveKeyboardUseCase(device),
            ObserveAttachedKeyboards = new ObserveAttachedKeyboardsUseCase(connection),
            SelectKeyboard = new SelectKeyboardUseCase(connection),
            KeyboardSource = new KeyboardSourceUseCase(sources),
        });
        var fixture = new Fixture(connection, device, profiles, sources, editor);
        await fixture.SettleAsync();
        return fixture;
    }

    public Task PlugAsync(ConnectedKeyboard keyboard) => SendAsync(new ConnectionState.Connected(keyboard));

    public async Task SendAsync(ConnectionState state)
    {
        Connection.Send(state);
        await SettleAsync();
    }

    public async Task SendAsync(AttachedKeyboards attached)
    {
        Connection.Send(attached);
        await Connection.KeyboardsIdle.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    /// <summary>Waits until the editor has dealt with every state sent.</summary>
    public Task SettleAsync() =>
        Connection.Idle.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync()
    {
        await stop.CancelAsync();
        await run;
        stop.Dispose();
    }
}
