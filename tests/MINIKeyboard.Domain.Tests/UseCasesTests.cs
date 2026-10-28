using MINIKeyboard.Domain.UseCases;

namespace MINIKeyboard.Domain.Tests;

public class UseCasesTests
{
    private static readonly ControlId Key1 = ControlId.Key(1);
    private static readonly CommandSet K8890 = new CommandSet.Packets(3);
    private static readonly CommandSet Standard = new CommandSet.Records(RecordFormat.Standard);
    private static readonly CommandSet Legacy = new CommandSet.Records(RecordFormat.Legacy);

    private static EditTarget Target(CommandSet commands, ControlId? control, int layer) =>
        new(new KeyboardCapabilities(commands), layer, control);

    private static EditTarget OnKey1(CommandSet commands) => Target(commands, Key1, 0);

    private static KeyAction Keys(params KeyStroke[] strokes) => new KeyAction.Keys(new KeyMacro([.. strokes]));

    private static KeyboardLayout Named(string id) => KeyboardLayout.Named(id)!;

    [Fact]
    public void PressesAddUp()
    {
        var configuration = new KeyboardConfiguration(3);
        var target = OnKey1(Standard);
        Assert.Equal(EditOutcome.Applied, new AppendStrokeUseCase().Execute(new KeyStroke(Modifiers.Super, 0x06), configuration, target));
        Assert.Equal(EditOutcome.Applied, new AppendStrokeUseCase().Execute(KeyStroke.OfKey(0x28), configuration, target));
        Assert.Equal(Keys(new KeyStroke(Modifiers.Super, 0x06), KeyStroke.OfKey(0x28)), configuration.Action(Key1, 0));
        Assert.True(configuration.IsChanged(Key1, 0));
        Assert.Null(configuration.Action(Key1, 1));
    }

    [Fact]
    public void NothingWithoutASelection()
    {
        var configuration = new KeyboardConfiguration(3);
        var target = Target(Standard, null, 0);
        Assert.Equal(EditOutcome.NeedsSelection, new AppendStrokeUseCase().Execute(KeyStroke.OfKey(0x04), configuration, target));
        Assert.False(configuration.HasUnsavedChanges);
    }

    /// <summary>
    /// Modifiers alone make a key a modifier key; a key pressed next takes them, as the vendor tools' modifier buttons
    /// work.
    /// </summary>
    [Fact]
    public void ModifiersAlone()
    {
        var configuration = new KeyboardConfiguration(3);
        var target = OnKey1(K8890);
        var append = new AppendStrokeUseCase();
        Assert.Equal(EditOutcome.Applied, append.Execute(new KeyStroke(Modifiers.Control, 0), configuration, target));
        Assert.Equal(Keys(new KeyStroke(Modifiers.Control, 0)), configuration.Action(Key1, 0));
        Assert.Equal(EditOutcome.Applied, append.Execute(new KeyStroke(Modifiers.Shift, 0x06), configuration, target));
        Assert.Equal(Keys(new KeyStroke(Modifiers.Control | Modifiers.Shift, 0x06)), configuration.Action(Key1, 0));
        Assert.Equal(EditOutcome.ModifiersOnlyAlone, append.Execute(new KeyStroke(Modifiers.Alt, 0), configuration, target));
    }

    /// <summary>A key the keyboard's vendor tool doesn't offer isn't added, pressed or set as a whole action.</summary>
    [Fact]
    public void AKeyTheKeyboardIsNotGivenStaysOut()
    {
        var configuration = new KeyboardConfiguration(3);
        var k8890 = OnKey1(K8890);
        Assert.Equal(EditOutcome.Unsupported, new AppendStrokeUseCase().Execute(KeyStroke.OfKey(0x68), configuration, k8890));
        Assert.Equal(EditOutcome.Unsupported, new SetActionUseCase().Execute(Keys(KeyStroke.OfKey(0x58)), configuration, k8890));
        Assert.False(configuration.HasUnsavedChanges);
        Assert.Equal(EditOutcome.Applied, new AppendStrokeUseCase().Execute(KeyStroke.OfKey(0x68), configuration, OnKey1(Standard)));
    }

    [Fact]
    public void AFullKeyTakesNoMore()
    {
        var configuration = new KeyboardConfiguration(3);
        var target = OnKey1(K8890);
        for (var press = 0; press < 5; press++)
            Assert.Equal(EditOutcome.Applied, new AppendStrokeUseCase().Execute(KeyStroke.OfKey(0x04), configuration, target));
        Assert.Equal(new EditOutcome.Full(5), new AppendStrokeUseCase().Execute(KeyStroke.OfKey(0x04), configuration, target));
    }

    [Fact]
    public void AMediaKeyReplacesPresses()
    {
        var configuration = new KeyboardConfiguration(3);
        var target = OnKey1(K8890);
        new AppendStrokeUseCase().Execute(KeyStroke.OfKey(0x04), configuration, target);
        Assert.Equal(EditOutcome.Applied, new SetActionUseCase().Execute(new KeyAction.Media(MediaKey.Mute), configuration, target));
        Assert.Equal(new KeyAction.Media(MediaKey.Mute), configuration.Action(Key1, 0));
        // The 8890 knows six media keys.
        Assert.Equal(EditOutcome.Unsupported, new SetActionUseCase().Execute(new KeyAction.Media(MediaKey.Calculator), configuration, target));
        // A key pressed now starts over.
        new AppendStrokeUseCase().Execute(KeyStroke.OfKey(0x05), configuration, target);
        Assert.Equal(Keys(KeyStroke.OfKey(0x05)), configuration.Action(Key1, 0));
    }

    [Fact]
    public void RemovingPresses()
    {
        var configuration = new KeyboardConfiguration(3);
        var target = OnKey1(Legacy);
        new AppendStrokeUseCase().Execute(KeyStroke.OfKey(0x04), configuration, target);
        new AppendStrokeUseCase().Execute(KeyStroke.OfKey(0x05), configuration, target);
        var remove = new RemoveLastStrokeUseCase();
        Assert.Equal(EditOutcome.Applied, remove.Execute(configuration, target));
        Assert.Equal(Keys(KeyStroke.OfKey(0x04)), configuration.Action(Key1, 0));
        Assert.Equal(EditOutcome.Applied, remove.Execute(configuration, target));
        Assert.Equal(KeyAction.Unassigned, configuration.Action(Key1, 0));
        Assert.Equal(EditOutcome.Unsupported, remove.Execute(configuration, target));
    }

    [Fact]
    public void Delays()
    {
        var configuration = new KeyboardConfiguration(3);
        var standard = OnKey1(Standard);
        new AppendStrokeUseCase().Execute(KeyStroke.OfKey(0x04), configuration, standard);
        new AppendStrokeUseCase().Execute(KeyStroke.OfKey(0x05), configuration, standard);
        Assert.Equal(EditOutcome.Applied, new SetPressDelayUseCase().Execute(1, 70000, configuration, standard));
        Assert.Equal(Keys(KeyStroke.OfKey(0x04), KeyStroke.OfKey(0x05).WithDelay(60000)), configuration.Action(Key1, 0));
        Assert.Equal(EditOutcome.Unsupported, new SetKeyDelayUseCase().Execute(100, configuration, standard));

        var legacy = OnKey1(Legacy);
        Assert.Equal(EditOutcome.Applied, new SetKeyDelayUseCase().Execute(9000, configuration, legacy));
        var keys = Assert.IsType<KeyAction.Keys>(configuration.Action(Key1, 0));
        Assert.Equal(6000u, keys.Macro.Delay);
        Assert.Equal(EditOutcome.Unsupported, new SetPressDelayUseCase().Execute(0, 5, configuration, OnKey1(K8890)));
    }

    [Fact]
    public void Clearing()
    {
        var configuration = new KeyboardConfiguration(3);
        Assert.Equal(EditOutcome.Applied, new ClearUseCase().Execute(configuration, OnKey1(Standard)));
        Assert.Equal(KeyAction.Unassigned, configuration.Action(Key1, 0));
        var layout = Named("6add1");
        new ClearUseCase().ExecuteAll(layout, configuration, 2);
        Assert.All(layout.Controls, control => Assert.Equal(KeyAction.Unassigned, configuration.Action(control, 2)));
        configuration.MarkSaved();
        Assert.False(configuration.HasUnsavedChanges);
    }

    [Fact]
    public void Lighting()
    {
        var configuration = new KeyboardConfiguration(3);
        var lighting = new SetLightingUseCase();
        var k8890 = OnKey1(K8890);
        Assert.Equal(EditOutcome.Applied, lighting.SetMode(2, configuration, k8890));
        Assert.Equal(new LayerLighting.ModeOnly(2), configuration.Lighting(0));
        Assert.Equal(EditOutcome.Unsupported, lighting.SetMode(3, configuration, k8890));

        var legacy = Target(Legacy, Key1, 1);
        Assert.Equal(EditOutcome.Applied, lighting.SetColor(LightColor.Red, configuration, legacy));
        Assert.Equal(EditOutcome.Applied, lighting.SetMode(4, configuration, legacy));
        Assert.Equal(new LayerLighting.ColorMode(LightColor.Red, 4), configuration.Lighting(1));

        var color = new KeyColor(1, 2, 3);
        var standard = Target(Standard, ControlId.Key(16), 2);
        Assert.Equal(EditOutcome.Applied, lighting.SetKeyColor(color, configuration, standard));
        var perKey = Assert.IsType<LayerLighting.PerKey>(configuration.Lighting(2));
        Assert.Equal(0, perKey.Mode);
        Assert.Equal(color, perKey.Colors[15]);
        var knob = Target(Standard, ControlId.Knob(1, KnobPart.Press), 2);
        Assert.Equal(new EditOutcome.NeedsColorKey(16), lighting.SetKeyColor(color, configuration, knob));
    }

    [Fact]
    public void ReadingReplacesWhatTheAppHad()
    {
        var configuration = new KeyboardConfiguration(3);
        configuration.Set(new KeyAction.Media(MediaKey.Mute), Key1, 0);
        configuration.ApplyRead(new Dictionary<ControlId, KeyAction> { [Key1] = new KeyAction.Mouse(MouseAction.LeftClick) }, 0);
        Assert.Equal(new KeyAction.Mouse(MouseAction.LeftClick), configuration.Action(Key1, 0));
        Assert.False(configuration.HasUnsavedChanges);
    }

    /// <summary>A keyboard that answers as told.</summary>
    private sealed class FakeDevice : IKeyboardDeviceRepository
    {
        public Queue<Func<KeyboardIdentity?>> Identities { get; } = new();

        /// <summary>What every layer reads as.</summary>
        public Dictionary<ControlId, KeyAction> Actions { get; set; } = [];

        /// <summary>By layer, the controls a read leaves unread.</summary>
        public Dictionary<int, EquatableArray<ControlId>> Unread { get; } = [];

        /// <summary>The controls no read asks for.</summary>
        public EquatableArray<ControlId> Outside { get; set; } = [];

        public KeyboardDeviceError? FailRead { get; set; }

        public KeyboardDeviceError? FailWrite { get; set; }

        public int Asked { get; private set; }

        /// <summary>Reads of actions and of lights.</summary>
        public int Reads { get; private set; }

        public List<KeyboardConfiguration> Writes { get; } = [];

        public LayerLighting LightingResult { get; set; } = new LayerLighting.PerKey(1, KeyColor.DefaultKeyColors);

        public Task<KeyboardIdentity?> AskIdentityAsync(CancellationToken cancellation = default)
        {
            Asked++;
            return Task.FromResult(Identities.TryDequeue(out var next) ? next() : null);
        }

        public Task<LayerRead> ReadActionsAsync(
            int layer, RecordFormat format, KeyboardLayout layout, CancellationToken cancellation = default)
        {
            Reads++;
            if (FailRead is { } error)
                throw new KeyboardDeviceException(error);
            var unread = Unread.GetValueOrDefault(layer);
            var actions = Actions
                .Where(pair => !unread.Contains(pair.Key) && !Outside.Contains(pair.Key))
                .ToDictionary();
            return Task.FromResult(new LayerRead(actions, unread, Outside));
        }

        public KeyAction? StoredForm(KeyAction action, RecordFormat format) => action;

        public Task<LayerLighting> ReadLightingAsync(int layer, CancellationToken cancellation = default)
        {
            Reads++;
            return FailRead is { } error
                ? Task.FromException<LayerLighting>(new KeyboardDeviceException(error))
                : Task.FromResult(LightingResult);
        }

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

    private static KeyboardIdentity? Throws(KeyboardDeviceError error) => throw new KeyboardDeviceException(error);

    private static ConnectedKeyboard K884xKeyboard() =>
        new(new UsbIdentity(0x1189, 0x8840), KeyboardFamily.K884x, 0, Channel.Hid, 3, 65, null);

    private static ConnectedKeyboard K8890Keyboard() =>
        new(KeyboardFamilies.K8890Usb, KeyboardFamily.K8890, 1, Channel.Hid, 3, 65, null);

    [Fact]
    public async Task The8890IsAskedNothing()
    {
        var device = new FakeDevice();
        var identification = await new IdentifyKeyboardUseCase(device).ExecuteAsync(K8890Keyboard(), TestContext.Current.CancellationToken);
        Assert.Equal(new CommandSet.Packets(3), identification.Commands);
        Assert.Equal(0, device.Asked);
    }

    [Fact]
    public async Task A884xKeyboardIsAskedUntilItAnswers()
    {
        var device = new FakeDevice();
        var identity = new KeyboardIdentity(6, 1, 10);
        device.Identities.Enqueue(() => Throws(new KeyboardDeviceError.InvalidReply()));
        device.Identities.Enqueue(() => null);
        device.Identities.Enqueue(() => identity);
        var identification = await new IdentifyKeyboardUseCase(device).ExecuteAsync(K884xKeyboard(), TestContext.Current.CancellationToken);
        Assert.Equal(new KeyboardIdentification(Standard, identity), identification);
        Assert.Equal(3, device.Asked);
    }

    [Fact]
    public async Task AKeyboardThatNeverAnswersTakesFeRecords()
    {
        var device = new FakeDevice();
        var identification = await new IdentifyKeyboardUseCase(device).ExecuteAsync(K884xKeyboard(), TestContext.Current.CancellationToken);
        Assert.Equal(new KeyboardIdentification(new CommandSet.Records(RecordFormat.Unidentified), null), identification);
        Assert.Equal(3, device.Asked);
    }

    [Fact]
    public async Task AWriteFailureEndsTheAsking()
    {
        var device = new FakeDevice();
        device.Identities.Enqueue(() => Throws(new KeyboardDeviceError.WriteFailed(1)));
        var exception = await Assert.ThrowsAsync<KeyboardDeviceException>(
            () => new IdentifyKeyboardUseCase(device).ExecuteAsync(K884xKeyboard(), TestContext.Current.CancellationToken));
        Assert.Equal(new KeyboardDeviceError.WriteFailed(1), exception.Error);
        Assert.Equal(1, device.Asked);
    }

    [Fact]
    public async Task ReadingTheStandardFormatReadsLightsToo()
    {
        var device = new FakeDevice { Actions = new() { [Key1] = new KeyAction.Media(MediaKey.Mute) } };
        var layout = Named("6add1");
        var read = new ReadKeyboardUseCase(device);
        var cancellation = TestContext.Current.CancellationToken;
        var outcome = await read.ExecuteAsync(new KeyboardConfiguration(3), new KeyboardCapabilities(Standard), layout, cancellation);
        Assert.Null(outcome.Error);
        Assert.Equal(new KeyAction.Media(MediaKey.Mute), outcome.Configuration.Action(Key1, 2));
        Assert.Equal(device.LightingResult, outcome.Configuration.Lighting(1));

        var legacy = await read.ExecuteAsync(new KeyboardConfiguration(3), new KeyboardCapabilities(Legacy), layout, cancellation);
        Assert.Null(legacy.Configuration.Lighting(0));

        var k8890 = await read.ExecuteAsync(new KeyboardConfiguration(3), new KeyboardCapabilities(K8890), layout, cancellation);
        Assert.Equal(new KeyboardDeviceError.Unsupported(), k8890.Error);
    }

    /// <summary>
    /// A layer whose answers stop short is read as far as they came, and reading goes on with the next layers; what
    /// didn't come is listed and stays as it was.
    /// </summary>
    [Fact]
    public async Task AShortLayerDoesntStopTheRead()
    {
        var key2 = ControlId.Key(2);
        var device = new FakeDevice
        {
            Actions = new() { [Key1] = new KeyAction.Media(MediaKey.Mute), [key2] = KeyAction.Unassigned },
        };
        device.Unread[0] = [key2];
        var configuration = new KeyboardConfiguration(3);
        configuration.Set(new KeyAction.Media(MediaKey.VolumeUp), key2, 0);
        var outcome = await new ReadKeyboardUseCase(device).ExecuteAsync(
            configuration, new KeyboardCapabilities(Legacy), Named("6add1"), TestContext.Current.CancellationToken);
        Assert.Null(outcome.Error);
        Assert.Equal([new LayerControls(0, [key2])], outcome.Unread);
        Assert.Equal(new KeyAction.Media(MediaKey.Mute), outcome.Configuration.Action(Key1, 0));
        Assert.Equal(new KeyAction.Media(MediaKey.VolumeUp), outcome.Configuration.Action(key2, 0));
        Assert.True(outcome.Configuration.IsChanged(key2, 0));
        Assert.Equal(KeyAction.Unassigned, outcome.Configuration.Action(key2, 2));
        Assert.Equal(3, device.Reads);

        device.Unread.Clear();
        var full = await new ReadKeyboardUseCase(device).ExecuteAsync(
            configuration, new KeyboardCapabilities(Legacy), Named("6add1"), TestContext.Current.CancellationToken);
        Assert.Empty(full.Unread);
    }

    [Fact]
    public async Task SavingMarksTheChangesSaved()
    {
        var device = new FakeDevice();
        var layout = Named("6add1");
        var capabilities = new KeyboardCapabilities(Standard);
        var save = new SaveKeyboardUseCase(device);
        var cancellation = TestContext.Current.CancellationToken;
        var configuration = new KeyboardConfiguration(3);
        Assert.Equal(new SaveOutcome.NothingToSave(), await save.ExecuteAsync(configuration, capabilities, layout, cancellation));
        configuration.Set(new KeyAction.Media(MediaKey.Mute), Key1, 0);
        device.Actions[Key1] = new KeyAction.Media(MediaKey.Mute);
        var saved = Assert.IsType<SaveOutcome.Saved>(await save.ExecuteAsync(configuration, capabilities, layout, cancellation));
        Assert.Equal(SaveCheck.Matched, saved.Check);
        Assert.False(saved.Configuration.HasUnsavedChanges);
        Assert.Equal(new KeyAction.Media(MediaKey.Mute), saved.Configuration.Action(Key1, 0));
        Assert.Equal([configuration], device.Writes);
        // Only the layer with changes is read back.
        Assert.Equal(1, device.Reads);

        device.FailWrite = new KeyboardDeviceError.WriteFailed(7);
        var exception = await Assert.ThrowsAsync<KeyboardDeviceException>(
            () => save.ExecuteAsync(configuration, capabilities, layout, cancellation));
        Assert.Equal(new KeyboardDeviceError.WriteFailed(7), exception.Error);
    }

    /// <summary>
    /// What read back otherwise stays changed, with the value the app has; the rest is saved.
    /// </summary>
    [Fact]
    public async Task WhatReadsBackOtherwiseStaysUnsaved()
    {
        var key2 = ControlId.Key(2);
        var device = new FakeDevice
        {
            Actions = new()
            {
                [Key1] = new KeyAction.Media(MediaKey.Mute),
                [key2] = new KeyAction.Media(MediaKey.VolumeDown),
            },
        };
        var configuration = new KeyboardConfiguration(3);
        configuration.Set(new KeyAction.Media(MediaKey.Mute), Key1, 1);
        configuration.Set(new KeyAction.Media(MediaKey.VolumeUp), key2, 1);
        configuration.SetLighting(new LayerLighting.ColorMode(LightColor.Red, 2), 0);
        var outcome = await new SaveKeyboardUseCase(device).ExecuteAsync(
            configuration, new KeyboardCapabilities(Legacy), Named("6add1"), TestContext.Current.CancellationToken);
        var saved = Assert.IsType<SaveOutcome.Saved>(outcome);
        Assert.Equal(new SaveCheck.Differs([new LayerControls(1, [key2])], []), saved.Check);
        Assert.True(saved.Configuration.IsChanged(key2, 1));
        Assert.Equal(new KeyAction.Media(MediaKey.VolumeUp), saved.Configuration.Action(key2, 1));
        Assert.False(saved.Configuration.IsChanged(Key1, 1));
        // A legacy keyboard's lights don't read back.
        Assert.False(saved.Configuration.Layers[0].LightingChanged);
        Assert.Equal(1, device.Reads);

        // A control that doesn't read back at all differs too.
        device.Actions.Remove(key2);
        saved = Assert.IsType<SaveOutcome.Saved>(await new SaveKeyboardUseCase(device).ExecuteAsync(
            configuration, new KeyboardCapabilities(Legacy), Named("6add1"), TestContext.Current.CancellationToken));
        Assert.Equal(new SaveCheck.Differs([new LayerControls(1, [key2])], []), saved.Check);
    }

    /// <summary>A read-back that fails, or leaves a control written unread, can't tell — the save stands.</summary>
    [Fact]
    public async Task AReadBackThatFailsCantTell()
    {
        var device = new FakeDevice
        {
            Actions = new() { [Key1] = new KeyAction.Media(MediaKey.Mute) },
            FailRead = new KeyboardDeviceError.NoReply(),
        };
        var configuration = new KeyboardConfiguration(3);
        configuration.Set(new KeyAction.Media(MediaKey.Mute), Key1, 0);
        configuration.Set(new KeyAction.Media(MediaKey.Mute), Key1, 2);
        var save = new SaveKeyboardUseCase(device);
        var capabilities = new KeyboardCapabilities(Standard);
        var cancellation = TestContext.Current.CancellationToken;
        var saved = Assert.IsType<SaveOutcome.Saved>(
            await save.ExecuteAsync(configuration, capabilities, Named("6add1"), cancellation));
        Assert.Equal(SaveCheck.CouldNotCheck, saved.Check);
        Assert.False(saved.Configuration.HasUnsavedChanges);
        Assert.Equal(2, device.Reads);

        device.FailRead = null;
        device.Unread[2] = [Key1];
        saved = Assert.IsType<SaveOutcome.Saved>(
            await save.ExecuteAsync(configuration, capabilities, Named("6add1"), cancellation));
        Assert.Equal(SaveCheck.CouldNotCheck, saved.Check);
        Assert.False(saved.Configuration.HasUnsavedChanges);
    }

    /// <summary>The 8890 and keyboards that don't answer identification aren't read back.</summary>
    [Fact]
    public async Task KeyboardsThatDontReadBackArentChecked()
    {
        var device = new FakeDevice();
        var configuration = new KeyboardConfiguration(3);
        configuration.Set(new KeyAction.Media(MediaKey.Mute), Key1, 0);
        configuration.SetLighting(new LayerLighting.ModeOnly(1), 0);
        foreach (var commands in new[] { K8890, new CommandSet.Records(RecordFormat.Unidentified) })
        {
            var saved = Assert.IsType<SaveOutcome.Saved>(await new SaveKeyboardUseCase(device).ExecuteAsync(
                configuration, new KeyboardCapabilities(commands), Named("6add1"), TestContext.Current.CancellationToken));
            Assert.Equal(SaveCheck.NotChecked, saved.Check);
            Assert.False(saved.Configuration.HasUnsavedChanges);
        }
        Assert.Equal(0, device.Reads);
    }

    /// <summary>A standard keyboard's lights read back too: those that come back otherwise stay changed.</summary>
    [Fact]
    public async Task LightsThatReadBackOtherwiseStayUnsaved()
    {
        var device = new FakeDevice();
        var configuration = new KeyboardConfiguration(3);
        var written = new LayerLighting.PerKey(3, KeyColor.DefaultKeyColors);
        configuration.SetLighting(written, 1);
        var save = new SaveKeyboardUseCase(device);
        var capabilities = new KeyboardCapabilities(Standard);
        var cancellation = TestContext.Current.CancellationToken;
        var saved = Assert.IsType<SaveOutcome.Saved>(
            await save.ExecuteAsync(configuration, capabilities, Named("6add1"), cancellation));
        Assert.Equal(new SaveCheck.Differs([], [1]), saved.Check);
        Assert.True(saved.Configuration.Layers[1].LightingChanged);
        Assert.Equal(written, saved.Configuration.Lighting(1));
        Assert.Equal(1, device.Reads);

        device.LightingResult = written;
        saved = Assert.IsType<SaveOutcome.Saved>(
            await save.ExecuteAsync(configuration, capabilities, Named("6add1"), cancellation));
        Assert.Equal(SaveCheck.Matched, saved.Check);
        Assert.False(saved.Configuration.HasUnsavedChanges);
    }

    /// <summary>
    /// Where nothing written could be compared — a legacy keyboard's lights, a control the read doesn't ask for — the
    /// save matched; such a control is saved.
    /// </summary>
    [Fact]
    public async Task NothingComparedIsAMatch()
    {
        var device = new FakeDevice();
        var save = new SaveKeyboardUseCase(device);
        var capabilities = new KeyboardCapabilities(Legacy);
        var layout = Named("16add3");
        var cancellation = TestContext.Current.CancellationToken;
        var configuration = new KeyboardConfiguration(3);
        configuration.SetLighting(new LayerLighting.ColorMode(LightColor.Green, 1), 0);
        var saved = Assert.IsType<SaveOutcome.Saved>(await save.ExecuteAsync(configuration, capabilities, layout, cancellation));
        Assert.Equal(SaveCheck.Matched, saved.Check);
        Assert.Equal(0, device.Reads);

        var outside = ControlId.Knob(3, KnobPart.RotateRight);
        device.Outside = [outside];
        configuration = new KeyboardConfiguration(3);
        configuration.Set(new KeyAction.Media(MediaKey.Mute), outside, 0);
        saved = Assert.IsType<SaveOutcome.Saved>(await save.ExecuteAsync(configuration, capabilities, layout, cancellation));
        Assert.Equal(SaveCheck.Matched, saved.Check);
        Assert.False(saved.Configuration.HasUnsavedChanges);
    }
}
