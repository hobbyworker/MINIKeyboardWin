namespace MINIKeyboard.Domain.UseCases;

/// <summary>Follows the keyboard as it comes and goes.</summary>
public sealed class ObserveConnectionUseCase(IKeyboardConnectionRepository repository)
{
    public IAsyncEnumerable<ConnectionState> Execute(CancellationToken cancellation = default) =>
        repository.ConnectionStates(cancellation);
}

/// <summary>Looks for the keyboard again.</summary>
public sealed class RetryConnectionUseCase(IKeyboardConnectionRepository repository)
{
    public void Execute() => repository.Retry();
}

/// <summary>Follows the supported keyboards plugged in and the one the app opens.</summary>
public sealed class ObserveAttachedKeyboardsUseCase(IKeyboardConnectionRepository repository)
{
    public IAsyncEnumerable<AttachedKeyboards> Execute(CancellationToken cancellation = default) =>
        repository.AttachedKeyboardUpdates(cancellation);
}

/// <summary>Opens another of the keyboards plugged in.</summary>
public sealed class SelectKeyboardUseCase(IKeyboardConnectionRepository repository)
{
    public void Execute(string id) => repository.SelectKeyboard(id);
}

/// <summary>
/// Settles the commands a newly connected keyboard takes. The 8890's report ID is in its descriptor, so it is asked
/// nothing; a 884x keyboard is asked who it is, up to <c>attempts</c> times, and one that never answers is written in
/// the <c>FE</c> form.
/// </summary>
public sealed class IdentifyKeyboardUseCase(IKeyboardDeviceRepository repository, int attempts = 3)
{
    private readonly int attempts = Math.Max(attempts, 1);

    public async Task<KeyboardIdentification> ExecuteAsync(
        ConnectedKeyboard keyboard, CancellationToken cancellation = default)
    {
        if (keyboard.Family == KeyboardFamily.K8890)
            return new KeyboardIdentification(new CommandSet.Packets(keyboard.ReportId), null);
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                if (await repository.AskIdentityAsync(cancellation) is { } identity)
                    return new KeyboardIdentification(new CommandSet.Records(identity.RecordFormat), identity);
            }
            catch (KeyboardDeviceException exception) when (exception.Error is KeyboardDeviceError.InvalidReply)
            {
            }
        }
        return new KeyboardIdentification(new CommandSet.Records(RecordFormat.Unidentified), null);
    }
}

/// <summary>What reading left: the configuration with every layer that arrived, and why it stopped, if it did.</summary>
public sealed record ReadOutcome(KeyboardConfiguration Configuration, KeyboardDeviceError? Error);

/// <summary>
/// Reads what the keyboard holds into a configuration: every layer's actions and, on the standard format, every
/// layer's lights. A layer whose answers don't all arrive is left as it was.
/// </summary>
public sealed class ReadKeyboardUseCase(IKeyboardDeviceRepository repository)
{
    public async Task<ReadOutcome> ExecuteAsync(
        KeyboardConfiguration configuration, KeyboardCapabilities capabilities, KeyboardLayout layout,
        CancellationToken cancellation = default)
    {
        if (capabilities.Commands is not CommandSet.Records { Format: var format }
            || capabilities.Reading == ReadSupport.Never)
        {
            return new ReadOutcome(configuration, new KeyboardDeviceError.Unsupported());
        }
        var updated = configuration.Clone();
        try
        {
            for (var layer = 0; layer < updated.LayerCount; layer++)
                updated.ApplyRead(await repository.ReadActionsAsync(layer, format, layout, cancellation), layer);
            if (capabilities.Lighting is LightingSupport.PerKeyColor)
            {
                for (var layer = 0; layer < updated.LayerCount; layer++)
                    updated.ApplyReadLighting(await repository.ReadLightingAsync(layer, cancellation), layer);
            }
            return new ReadOutcome(updated, null);
        }
        catch (KeyboardDeviceException exception)
        {
            return new ReadOutcome(updated, exception.Error);
        }
    }
}

/// <summary>What saving came to.</summary>
public abstract record SaveOutcome
{
    private SaveOutcome() { }

    public sealed record Saved(KeyboardConfiguration Configuration) : SaveOutcome;

    public sealed record NothingToSave : SaveOutcome;
}

/// <summary>Writes what changed. On a failure the changes stay marked, so saving can be tried again.</summary>
public sealed class SaveKeyboardUseCase(IKeyboardDeviceRepository repository)
{
    /// <exception cref="KeyboardDeviceException">The keyboard didn't take it.</exception>
    public async Task<SaveOutcome> ExecuteAsync(
        KeyboardConfiguration configuration, KeyboardCapabilities capabilities, KeyboardLayout layout,
        CancellationToken cancellation = default)
    {
        if (!configuration.HasUnsavedChanges)
            return new SaveOutcome.NothingToSave();
        await repository.WriteAsync(configuration, capabilities.Commands, layout, cancellation);
        var saved = configuration.Clone();
        saved.MarkSaved();
        return new SaveOutcome.Saved(saved);
    }
}

/// <summary>The device log.</summary>
public sealed class DeviceLogUseCase(IDeviceLogRepository repository)
{
    public IAsyncEnumerable<IReadOnlyList<DeviceLogEntry>> Entries(CancellationToken cancellation = default) =>
        repository.Entries(cancellation);

    public void Clear() => repository.Clear();
}

/// <summary>Switches between the keyboard on USB and a simulated one.</summary>
public sealed class KeyboardSourceUseCase(IKeyboardSourceRepository repository)
{
    public KeyboardSource Current => repository.Current;

    public void Select(KeyboardSource source) => repository.Select(source);
}
