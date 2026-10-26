using MINIKeyboard.Domain;
using MINIKeyboard.Domain.UseCases;
using ActivityKind = MINIKeyboard.Presentation.Activity;

namespace MINIKeyboard.Presentation;

/// <summary>The use cases the editor works through, wired in by the composition root.</summary>
public sealed class EditorDependencies
{
    public required ObserveConnectionUseCase ObserveConnection { get; init; }

    public required RetryConnectionUseCase RetryConnection { get; init; }

    public required IdentifyKeyboardUseCase IdentifyKeyboard { get; init; }

    public required ResolveLayoutUseCase ResolveLayout { get; init; }

    public required ChooseLayoutUseCase ChooseLayout { get; init; }

    public required ReadKeyboardUseCase ReadKeyboard { get; init; }

    public required SaveKeyboardUseCase SaveKeyboard { get; init; }

    /// <summary>The simulator's switch.</summary>
    public KeyboardSourceUseCase? KeyboardSource { get; init; }
}

/// <summary>What the editor holds between the user's doings.</summary>
internal sealed class EditorState
{
    public Activity? Activity { get; set; }

    public Notice? Notice { get; set; }

    public ulong NoticeSerial { get; set; }

    /// <summary>The layout sheet is up — on its own when a keyboard that can't say its shape needs one set.</summary>
    public bool IsChoosingLayout { get; set; }

    public EditorTab Tab { get; set; } = EditorTab.Keys;

    public int Layer { get; set; }

    public ConnectionState Connection { get; set; } = ConnectionState.Searching;

    public KeyboardIdentification? Identification { get; set; }

    public ResolvedLayout? ResolvedLayout { get; set; }

    public KeyboardConfiguration Configuration { get; set; } = new(3);

    public ControlId? Selection { get; set; }

    /// <summary>Modifiers picked for the next key.</summary>
    public Modifiers Held { get; set; } = Modifiers.None;

    /// <summary>The keyboard on USB, or a simulated one.</summary>
    public KeyboardSource KeyboardSource { get; set; } = KeyboardSource.Hardware;

    public ConnectedKeyboard? Keyboard => Connection.Keyboard;

    public KeyboardCapabilities? Capabilities =>
        Identification is { } identification ? new KeyboardCapabilities(identification.Commands) : null;

    /// <summary>Connected, identified and not busy.</summary>
    public bool IsReady => Keyboard is not null && Identification is not null && Activity is null;

    public int LayerCount => Capabilities?.LayerCount ?? 3;

    public IReadOnlyList<EditorTab> AvailableTabs()
    {
        if (Capabilities is not { } capabilities)
            return EditorTabs.All;
        return [.. EditorTabs.All.Where(tab => tab != EditorTab.Delay || capabilities.Delays is not DelaySupport.None)];
    }

    public bool ReadsBack => Capabilities is { Reading: not ReadSupport.Never };

    public KeyAction? Action(ControlId control) => Configuration.Action(control, Layer);

    public KeyAction? SelectedAction => Selection is { } control ? Action(control) : null;

    public LayerLighting? Lighting => Configuration.Lighting(Layer);

    /// <summary>The colour of a key on a keyboard that lights each key.</summary>
    public KeyColor? KeyColor(ControlId control)
    {
        if (Capabilities?.Lighting is not LightingSupport.PerKeyColor { Keys: var keys })
            return null;
        if (Lighting is not LayerLighting.PerKey { Colors: var colors })
            return null;
        return SetLightingUseCase.ColorIndex(control, keys) is { } index && index < colors.Count
            ? colors[index]
            : null;
    }

    public EditTarget? Target() =>
        Capabilities is { } capabilities ? new EditTarget(capabilities, Layer, Selection) : null;

    public void Show(Notice notice)
    {
        Notice = notice;
        NoticeSerial++;
    }

    public void Report(EditOutcome outcome)
    {
        switch (outcome)
        {
            case EditOutcome.NeedsSelectionOutcome:
                Show(new Notice.SelectControlFirst());
                break;
            case EditOutcome.UnsupportedOutcome:
                Show(new Notice.Unsupported());
                break;
            case EditOutcome.Full full:
                Show(new Notice.Full(full.Limit));
                break;
            case EditOutcome.ModifiersOnlyAloneOutcome:
                Show(new Notice.ModifiersOnlyAlone());
                break;
            case EditOutcome.NeedsColorKey needsColorKey:
                Show(new Notice.NeedsColorKey(needsColorKey.Keys));
                break;
        }
    }
}

/// <summary>
/// The main window's state and what the user does in it: the keyboard connected, its layout, the layer, key and page
/// shown, and the app's copy of the keyboard's configuration. The rules are the use cases'; this keeps the screen,
/// turns their outcomes into notices, and gives the views what they draw in the presentation's own types (see
/// <c>Screen.cs</c>).
/// </summary>
/// <remarks>
/// It lives on the interface's thread. Its work that waits returns a task that resumes there. It says when it changed
/// through <see cref="Changed"/>; the views read what they draw again.
/// </remarks>
public sealed partial class EditorModel(EditorDependencies dependencies)
{
    private static readonly AppendStrokeUseCase AppendStrokeRule = new();
    private static readonly RemoveLastStrokeUseCase RemoveLastStrokeRule = new();
    private static readonly SetActionUseCase SetActionRule = new();
    private static readonly SetPressDelayUseCase SetPressDelayRule = new();
    private static readonly SetKeyDelayUseCase SetKeyDelayRule = new();
    private static readonly ClearUseCase ClearRule = new();
    private static readonly SetLightingUseCase SetLightingRule = new();

    private readonly EditorState state = new();

    /// <summary>Raised after every change, once the change is done, so handlers can read it.</summary>
    public event EventHandler? Changed;

    /// <summary>Changes the state, then tells the listeners — after the change is done, so they can read it.</summary>
    private void Update(Action<EditorState> change)
    {
        change(state);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private T Update<T>(Func<EditorState, T> change)
    {
        var result = change(state);
        Changed?.Invoke(this, EventArgs.Empty);
        return result;
    }

    /// <summary>
    /// Follows the keyboard until <paramref name="cancellation"/> is cancelled; one that comes is identified at once.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellation = default)
    {
        if (dependencies.KeyboardSource is { } source)
        {
            var current = source.Current;
            Update(state => state.KeyboardSource = current);
        }
        try
        {
            await foreach (var connection in dependencies.ObserveConnection.Execute(cancellation))
                await ConnectionChangedAsync(connection);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
    }

    // State the views read

    public Activity? Activity => state.Activity;

    public Notice? Notice => state.Notice;

    /// <summary>Counts up with each notice shown, so the one to take away can be told from a newer one.</summary>
    public ulong NoticeSerial => state.NoticeSerial;

    public bool IsChoosingLayout => state.IsChoosingLayout;

    /// <summary>Opens or closes the layout sheet — it opens only with a keyboard to choose for.</summary>
    public void SetChoosingLayout(bool choosing) =>
        Update(state => state.IsChoosingLayout = choosing && state.ResolvedLayout is not null && state.Keyboard is not null);

    public EditorTab Tab => state.Tab;

    public void SetTab(EditorTab tab) => Update(state => state.Tab = tab);

    public int Layer => state.Layer;

    public void SetLayer(int layer) => Update(state => state.Layer = Math.Clamp(layer, 0, state.LayerCount - 1));

    public bool IsReady => state.IsReady;

    public int LayerCount => state.LayerCount;

    public IReadOnlyList<EditorTab> AvailableTabs => state.AvailableTabs();

    /// <summary>Whether the keyboard reads back at all — then the header has Read.</summary>
    public bool ReadsBack => state.ReadsBack;

    public bool CanRead => state.IsReady && state.ReadsBack;

    public bool HasUnsavedChanges => state.Configuration.HasUnsavedChanges;

    // Connection

    private async Task ConnectionChangedAsync(ConnectionState connection)
    {
        var identify = Update(state =>
        {
            var previous = state.Keyboard;
            state.Connection = connection;
            if (state.Keyboard == previous)
                return false;
            state.Identification = null;
            state.Selection = null;
            state.Held = Modifiers.None;
            // A sheet for the keyboard that went goes with it; the next one asks again if it needs to.
            state.IsChoosingLayout = false;
            if (state.Keyboard is not { } keyboard)
                return false;
            if (keyboard.Usb != previous?.Usb)
                state.ResolvedLayout = null;
            return true;
        });
        if (identify)
            await IdentifyAsync();
    }

    public void RetryConnection() => dependencies.RetryConnection.Execute();

    /// <summary>Settles the keyboard's commands and layout, then reads it if it reads back on its own.</summary>
    public async Task IdentifyAsync()
    {
        if (state.Keyboard is not { } keyboard || state.Identification is not null || state.Activity is not null)
            return;
        Update(state => state.Activity = ActivityKind.Identifying);
        KeyboardIdentification identified;
        try
        {
            identified = await dependencies.IdentifyKeyboard.ExecuteAsync(keyboard);
        }
        catch (KeyboardDeviceException)
        {
            Update(state =>
            {
                state.Activity = null;
                state.Show(new Notice.IdentifyFailed());
            });
            return;
        }
        var readNow = Update(state =>
        {
            state.Activity = null;
            if (state.Keyboard != keyboard)
                return false;
            state.Identification = identified;
            var capabilities = new KeyboardCapabilities(identified.Commands);
            var resolved = dependencies.ResolveLayout.Execute(keyboard, identified);
            state.IsChoosingLayout = resolved.NeedsChoice;
            state.ResolvedLayout = resolved;
            state.Configuration = new KeyboardConfiguration(capabilities.LayerCount);
            state.Layer = Math.Min(state.Layer, capabilities.LayerCount - 1);
            if (!state.AvailableTabs().Contains(state.Tab))
                state.Tab = EditorTab.Keys;
            return capabilities.Reading == ReadSupport.OnConnect;
        });
        if (readNow)
            await ReadAsync(announce: false);
    }

    // Reading and saving

    public Task ReadAsync() => ReadAsync(announce: true);

    private async Task ReadAsync(bool announce)
    {
        if (state.Capabilities is not { } capabilities || state.ResolvedLayout is not { } resolved)
            return;
        if (!(state.IsReady && state.ReadsBack))
            return;
        var configuration = state.Configuration.Clone();
        Update(state => state.Activity = ActivityKind.Reading);
        var outcome = await dependencies.ReadKeyboard.ExecuteAsync(configuration, capabilities, resolved.Layout);
        Update(state =>
        {
            state.Activity = null;
            state.Configuration = outcome.Configuration;
            if (outcome.Error is not null)
                state.Show(new Notice.ReadFailed());
            else if (announce)
                state.Show(new Notice.ReadComplete());
        });
    }

    public async Task SaveAsync()
    {
        if (state.Capabilities is not { } capabilities || state.ResolvedLayout is not { } resolved || !state.IsReady)
            return;
        var configuration = state.Configuration.Clone();
        if (!configuration.HasUnsavedChanges)
        {
            Update(state => state.Show(new Notice.NothingToSave()));
            return;
        }
        Update(state => state.Activity = ActivityKind.Saving);
        SaveOutcome? outcome = null;
        KeyboardDeviceError? error = null;
        try
        {
            outcome = await dependencies.SaveKeyboard.ExecuteAsync(configuration, capabilities, resolved.Layout);
        }
        catch (KeyboardDeviceException exception)
        {
            error = exception.Error;
        }
        Update(state =>
        {
            state.Activity = null;
            switch (outcome, error)
            {
                case (SaveOutcome.Saved saved, _):
                    state.Configuration = saved.Configuration;
                    state.Show(new Notice.Saved(state.KeyboardSource.IsSimulated));
                    break;
                case (SaveOutcome.NothingToSave, _):
                    state.Show(new Notice.NothingToSave());
                    break;
                case (_, KeyboardDeviceError.WriteFailed failed):
                    state.Show(new Notice.WriteFailed(failed.Code));
                    break;
                case (_, KeyboardDeviceError.Unsupported):
                    state.Show(new Notice.Unsupported());
                    break;
                default:
                    state.Show(new Notice.KeyboardGone());
                    break;
            }
        });
    }

    // Layout

    /// <summary>The layout and rotation the user chose; on a keyboard that said its shape, only the rotation.</summary>
    private void ApplyLayout(KeyboardLayout layout, Rotation rotation) => Update(state =>
    {
        if (state.Keyboard is not { } keyboard || state.ResolvedLayout is not { } resolved)
            return;
        var chosen = dependencies.ChooseLayout.Execute(resolved, layout, rotation, keyboard.Usb);
        if (state.Selection is { } selection && !chosen.Layout.Has(selection))
            state.Selection = null;
        state.ResolvedLayout = chosen;
        state.IsChoosingLayout = false;
    });

    /// <summary>
    /// One of the cross-platform app's layouts by its name, or with null the keyboard's own — only turned.
    /// </summary>
    public void ChooseLayout(string? id, int quarterTurns)
    {
        var layout = (id is null ? null : KeyboardLayout.Named(id)) ?? state.ResolvedLayout?.Layout;
        if (layout is not null)
            ApplyLayout(layout, Rotations.FromQuarterTurns(quarterTurns));
    }

    /// <summary>An arrangement: keys, keys to a row, knobs.</summary>
    public void ChooseArrangement(byte keys, byte columns, byte knobs, int quarterTurns)
    {
        if (GridShape.Create(keys, columns, knobs) is { } shape)
            ApplyLayout(shape.Layout(), Rotations.FromQuarterTurns(quarterTurns));
    }

    /// <summary>
    /// Whether there is a layout to turn or change — the connected keyboard's. With none connected the last one is
    /// still drawn, dimmed, but there is nothing to change it on.
    /// </summary>
    public bool HasLayout => state.ResolvedLayout is not null && state.Keyboard is not null;

    public void Rotate()
    {
        if (state.ResolvedLayout is { } resolved)
            ApplyLayout(resolved.Layout, resolved.Rotation.Next());
    }

    // Editing

    public void Select(ControlRef control) => Update(state =>
    {
        if (state.IsReady)
            state.Selection = control.Id;
    });

    private void Edit(Func<KeyboardConfiguration, EditTarget, EditOutcome> change) => Update(state =>
    {
        if (!state.IsReady || state.Target() is not { } target)
            return;
        state.Report(change(state.Configuration, target));
    });

    /// <summary>A key, with the modifiers held for it.</summary>
    public void Press(KeyUsage key)
    {
        var stroke = new KeyStroke(state.Held, key.Usage);
        Edit((configuration, target) => AppendStrokeRule.Execute(stroke, configuration, target));
        Update(state => state.Held = Modifiers.None);
    }

    /// <summary>A shifted symbol: Shift and its key.</summary>
    public void PressShifted(KeyUsage key)
    {
        var stroke = new KeyStroke(state.Held | Modifiers.Shift, key.Usage);
        Edit((configuration, target) => AppendStrokeRule.Execute(stroke, configuration, target));
        Update(state => state.Held = Modifiers.None);
    }

    public void Toggle(ModifierKey modifier) => Update(state => state.Held ^= modifier.ToModifiers());

    /// <summary>The held modifiers alone, which makes the key a modifier key.</summary>
    public void AssignHeldModifiers()
    {
        var held = state.Held;
        if (held == Modifiers.None)
            return;
        Edit((configuration, target) => AppendStrokeRule.Execute(new KeyStroke(held, 0), configuration, target));
        Update(state => state.Held = Modifiers.None);
    }

    public void RemoveLastStroke() => Edit(RemoveLastStrokeRule.Execute);

    public void AssignMedia(MediaChoice media)
    {
        var action = new KeyAction.Media(media.Key());
        Edit((configuration, target) => SetActionRule.Execute(action, configuration, target));
    }

    public void AssignMouse(MouseChoice mouse)
    {
        var action = new KeyAction.Mouse(mouse.ToAction());
        Edit((configuration, target) => SetActionRule.Execute(action, configuration, target));
    }

    public void SetDelay(int press, uint milliseconds) =>
        Edit((configuration, target) => SetPressDelayRule.Execute(press, milliseconds, configuration, target));

    public void SetKeyDelay(uint milliseconds) =>
        Edit((configuration, target) => SetKeyDelayRule.Execute(milliseconds, configuration, target));

    public void ClearSelected() => Edit(ClearRule.Execute);

    public void ClearLayer()
    {
        if (state.ResolvedLayout is not { } resolved)
            return;
        var layout = resolved.Layout;
        Edit((configuration, target) => ClearRule.ExecuteAll(layout, configuration, target.Layer));
    }

    public void SetLightingMode(byte mode) =>
        Edit((configuration, target) => SetLightingRule.SetMode(mode, configuration, target));

    public void SetLightColor(LedColor color) =>
        Edit((configuration, target) => SetLightingRule.SetColor(color.ToLightColor(), configuration, target));

    public void SetKeyColor(Rgb color) =>
        Edit((configuration, target) => SetLightingRule.SetKeyColor(color.ToKeyColor(), configuration, target));

    // Simulator

    public bool CanSelectSource => dependencies.KeyboardSource is not null;

    /// <summary>Every keyboard the simulator plays, when it is wired in.</summary>
    public IReadOnlyList<SimulatorChoice> SimulatorChoices =>
        dependencies.KeyboardSource is null ? [] : [.. SimulatedModel.All().Select(SimulatorChoice.Of)];

    /// <summary>The simulated keyboard in use, or null for the keyboard on USB.</summary>
    public string? ActiveSimulatorId =>
        state.KeyboardSource is KeyboardSource.Simulated { Model: var model } ? model.Id : null;

    /// <summary>
    /// Switches to a simulated keyboard, or with null to the keyboard on USB. The switch reports "searching" first, so
    /// the window starts over as after re-plugging.
    /// </summary>
    public void SelectSimulator(string? id)
    {
        if (dependencies.KeyboardSource is not { } keyboardSource)
            return;
        KeyboardSource selected;
        if (id is null)
            selected = KeyboardSource.Hardware;
        else if (SimulatedModel.All().FirstOrDefault(model => model.Id == id) is { } model)
            selected = new KeyboardSource.Simulated(model);
        else
            return;
        if (selected == state.KeyboardSource)
            return;
        keyboardSource.Select(selected);
        Update(state => state.KeyboardSource = selected);
    }

    // Notices

    public void DismissNotice() => Update(state => state.Notice = null);

    /// <summary>Takes the notice away when it is still the one shown as <paramref name="serial"/>.</summary>
    public void ExpireNotice(ulong serial)
    {
        if (state.NoticeSerial == serial && state.Notice is not null)
            DismissNotice();
    }

    public override string ToString() =>
        $"EditorModel {{ Connection = {state.Connection}, Activity = {state.Activity}, Layer = {state.Layer} }}";
}
