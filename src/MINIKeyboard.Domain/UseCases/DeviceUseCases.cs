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

/// <summary>
/// What reading left: the configuration with every layer that arrived, the controls whose answers didn't, and why it
/// stopped, if it did.
/// </summary>
/// <param name="Unread">By layer, the controls that weren't read and stay as they were; empty when everything came.</param>
public sealed record ReadOutcome(
    KeyboardConfiguration Configuration, KeyboardDeviceError? Error, EquatableArray<LayerControls> Unread = default);

/// <summary>
/// Reads what the keyboard holds into a configuration: every layer's actions and, on the standard format, every
/// layer's lights. Controls whose answers don't arrive are left as they were, and reading goes on; a failure stops it.
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
        var unread = new List<LayerControls>();
        try
        {
            for (var layer = 0; layer < updated.LayerCount; layer++)
            {
                var read = await repository.ReadActionsAsync(layer, format, layout, cancellation);
                updated.ApplyRead(read.Actions, layer);
                if (!read.Unread.IsEmpty)
                    unread.Add(new LayerControls(layer, read.Unread));
            }
            if (capabilities.Lighting is LightingSupport.PerKeyColor)
            {
                for (var layer = 0; layer < updated.LayerCount; layer++)
                    updated.ApplyReadLighting(await repository.ReadLightingAsync(layer, cancellation), layer);
            }
            return new ReadOutcome(updated, null, [.. unread]);
        }
        catch (KeyboardDeviceException exception)
        {
            return new ReadOutcome(updated, exception.Error, [.. unread]);
        }
    }
}

/// <summary>Whether what was saved reads back as it was written.</summary>
public abstract record SaveCheck
{
    private SaveCheck() { }

    /// <summary>The keyboard doesn't read back.</summary>
    public static SaveCheck NotChecked { get; } = new NotCheckedCheck();

    /// <summary>Everything written read back as written.</summary>
    public static SaveCheck Matched { get; } = new MatchedCheck();

    /// <summary>Nothing read back otherwise, but something written couldn't be read back to compare.</summary>
    public static SaveCheck CouldNotCheck { get; } = new CouldNotCheckCheck();

    public sealed record NotCheckedCheck : SaveCheck;

    public sealed record MatchedCheck : SaveCheck;

    public sealed record CouldNotCheckCheck : SaveCheck;

    /// <summary>These controls, and the lights of these layers, read back otherwise than written.</summary>
    public sealed record Differs(EquatableArray<LayerControls> Controls, EquatableArray<int> Lighting) : SaveCheck;
}

/// <summary>What saving came to.</summary>
public abstract record SaveOutcome
{
    private SaveOutcome() { }

    /// <summary>
    /// Written. Every change is marked saved but those that read back otherwise (<see cref="SaveCheck.Differs"/>),
    /// which stay changed.
    /// </summary>
    public sealed record Saved(KeyboardConfiguration Configuration, SaveCheck Check) : SaveOutcome;

    public sealed record NothingToSave : SaveOutcome;
}

/// <summary>
/// Writes what changed, then — on a keyboard that reads back — reads back what was written and compares it. On a
/// failure the changes stay marked, so saving can be tried again; a read-back that fails doesn't fail the save.
/// </summary>
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
        var check = await CheckAsync(configuration, capabilities, layout, cancellation);
        if (check is SaveCheck.Differs differs)
            saved.MarkSaved(differs.Controls, differs.Lighting);
        else
            saved.MarkSaved();
        return new SaveOutcome.Saved(saved, check);
    }

    /// <summary>
    /// Reads back each layer with changed controls, and the lights of each layer whose lights changed where the
    /// keyboard reads lights back, and compares them with what was written in the form the keyboard keeps it. A
    /// control outside the read isn't compared; with nothing compared, nothing differs.
    /// </summary>
    private async Task<SaveCheck> CheckAsync(
        KeyboardConfiguration configuration, KeyboardCapabilities capabilities, KeyboardLayout layout,
        CancellationToken cancellation)
    {
        if (capabilities.Commands is not CommandSet.Records
            {
                Format: (RecordFormat.Standard or RecordFormat.Legacy) and var format,
            }
            || capabilities.Reading == ReadSupport.Never)
        {
            return SaveCheck.NotChecked;
        }
        var differing = new List<LayerControls>();
        var differingLighting = new List<int>();
        var uncertain = false;
        for (var layer = 0; layer < configuration.LayerCount; layer++)
        {
            // The changes written: the layout's.
            var changed = layout.Controls.Where(control => configuration.IsChanged(control, layer)).Order().ToList();
            if (changed.Count == 0)
                continue;
            LayerRead read;
            try
            {
                read = await repository.ReadActionsAsync(layer, format, layout, cancellation);
            }
            catch (KeyboardDeviceException)
            {
                uncertain = true;
                continue;
            }
            var differs = new List<ControlId>();
            foreach (var control in changed.Where(control => !read.Outside.Contains(control)))
            {
                if (configuration.Action(control, layer) is not { } written
                    || repository.StoredForm(written, format) is not { } expected
                    || read.Unread.Contains(control))
                {
                    uncertain = true;
                }
                else if (!read.Actions.TryGetValue(control, out var actual) || actual != expected)
                {
                    differs.Add(control);
                }
            }
            if (differs.Count > 0)
                differing.Add(new LayerControls(layer, [.. differs]));
        }
        if (capabilities.Lighting is LightingSupport.PerKeyColor)
        {
            for (var layer = 0; layer < configuration.LayerCount; layer++)
            {
                if (!configuration.Layers[layer].LightingChanged || configuration.Lighting(layer) is not { } written)
                    continue;
                try
                {
                    if (await repository.ReadLightingAsync(layer, cancellation) != written)
                        differingLighting.Add(layer);
                }
                catch (KeyboardDeviceException)
                {
                    uncertain = true;
                }
            }
        }
        if (differing.Count > 0 || differingLighting.Count > 0)
            return new SaveCheck.Differs([.. differing], [.. differingLighting]);
        return uncertain ? SaveCheck.CouldNotCheck : SaveCheck.Matched;
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
