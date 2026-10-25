using System.Runtime.CompilerServices;
using MINIKeyboard.Domain;
using MINIKeyboard.Domain.UseCases;
using Channels = System.Threading.Channels;

namespace MINIKeyboard.Presentation.Tests;

/// <summary>
/// A keyboard that is plugged in when told. <see cref="Idle"/> completes once the subscriber has taken every state and
/// asks for the next — the editor has finished with what it was sent.
/// </summary>
internal sealed class FakeConnection : IKeyboardConnectionRepository
{
    private readonly Channels.Channel<ConnectionState> states = Channels.Channel.CreateUnbounded<ConnectionState>();
    private readonly Lock gate = new();
    private TaskCompletionSource idle = NewIdle();
    private bool subscribed;

    public FakeConnection() => states.Writer.TryWrite(ConnectionState.Searching);

    public Task Idle
    {
        get
        {
            lock (gate)
                return idle.Task;
        }
    }

    public void Send(ConnectionState state)
    {
        lock (gate)
            idle = NewIdle();
        states.Writer.TryWrite(state);
    }

    public async IAsyncEnumerable<ConnectionState> ConnectionStates(
        [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        Assert.False(subscribed, "one subscriber");
        subscribed = true;
        while (true)
        {
            if (!states.Reader.TryRead(out var state))
            {
                lock (gate)
                    idle.TrySetResult();
                state = await states.Reader.ReadAsync(cancellation);
            }
            yield return state;
        }
    }

    public void Retry() { }

    private static TaskCompletionSource NewIdle() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

internal sealed class FakeDevice : IKeyboardDeviceRepository
{
    public KeyboardIdentity? Identity { get; set; }

    public Dictionary<ControlId, KeyAction> Actions { get; set; } = [];

    public KeyboardDeviceError? FailWrite { get; set; }

    public List<KeyboardConfiguration> Writes { get; } = [];

    public int Reads { get; private set; }

    public Task<KeyboardIdentity?> AskIdentityAsync(CancellationToken cancellation = default) =>
        Task.FromResult(Identity);

    public Task<IReadOnlyDictionary<ControlId, KeyAction>> ReadActionsAsync(
        int layer, RecordFormat format, KeyboardLayout layout, CancellationToken cancellation = default)
    {
        Reads++;
        return Task.FromResult<IReadOnlyDictionary<ControlId, KeyAction>>(new Dictionary<ControlId, KeyAction>(Actions));
    }

    public Task<LayerLighting> ReadLightingAsync(int layer, CancellationToken cancellation = default) =>
        Task.FromResult<LayerLighting>(new LayerLighting.PerKey(0, KeyColor.DefaultKeyColors));

    public Task WriteAsync(
        KeyboardConfiguration configuration, CommandSet commands, KeyboardLayout layout,
        CancellationToken cancellation = default)
    {
        if (FailWrite is { } error)
            throw new KeyboardDeviceException(error);
        Writes.Add(configuration.Clone());
        return Task.CompletedTask;
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

    private Fixture(FakeConnection connection, FakeDevice device, Profiles profiles, EditorModel editor)
    {
        Connection = connection;
        Device = device;
        Profiles = profiles;
        Editor = editor;
        stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        run = editor.RunAsync(stop.Token);
    }

    public FakeConnection Connection { get; }

    public FakeDevice Device { get; }

    public Profiles Profiles { get; }

    public EditorModel Editor { get; }

    /// <summary>A fixture whose device is set up by <paramref name="setUp"/> before the editor starts.</summary>
    public static async Task<Fixture> CreateAsync(Action<FakeDevice>? setUp = null)
    {
        var connection = new FakeConnection();
        var device = new FakeDevice();
        setUp?.Invoke(device);
        var profiles = new Profiles();
        var editor = new EditorModel(new EditorDependencies
        {
            ObserveConnection = new ObserveConnectionUseCase(connection),
            RetryConnection = new RetryConnectionUseCase(connection),
            IdentifyKeyboard = new IdentifyKeyboardUseCase(device, 1),
            ResolveLayout = new ResolveLayoutUseCase(profiles),
            ChooseLayout = new ChooseLayoutUseCase(profiles),
            ReadKeyboard = new ReadKeyboardUseCase(device),
            SaveKeyboard = new SaveKeyboardUseCase(device),
        });
        var fixture = new Fixture(connection, device, profiles, editor);
        await fixture.SettleAsync();
        return fixture;
    }

    public Task PlugAsync(ConnectedKeyboard keyboard) => SendAsync(new ConnectionState.Connected(keyboard));

    public async Task SendAsync(ConnectionState state)
    {
        Connection.Send(state);
        await SettleAsync();
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
