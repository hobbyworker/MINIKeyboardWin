using MINIKeyboard.Domain;

namespace MINIKeyboard.Presentation.Tests;

public class EditorTests
{
    private const byte A = 0x04;
    private const byte C = 0x06;

    private static ConnectedKeyboard K8890() =>
        new(KeyboardFamilies.K8890Usb, KeyboardFamily.K8890, 1, Channel.Hid, 3, 65, null);

    private static ConnectedKeyboard K8840() =>
        new(new UsbIdentity(0x1189, 0x8840), KeyboardFamily.K884x, 0, Channel.Hid, 3, 65, null);

    private static KeyUsage Key(byte usage) => KeyUsage.Named(usage)!.Value;

    private static ControlDrawing Control(KeyboardDrawing drawing, ControlRef control) =>
        drawing.Controls.Single(drawn => drawn.Control == control);

    [Fact]
    public async Task The8890IsArranged()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PlugAsync(K8890());
        var editor = fixture.Editor;
        Assert.True(editor.IsReady);
        Assert.Equal(new ConnectionStatus.Connected("1189:8890", null, null, true, false), editor.Status);
        Assert.True(editor.IsChoosingLayout);
        Assert.False(editor.ReadsBack);
        Assert.Equal([EditorTab.Keys, EditorTab.Media, EditorTab.Mouse, EditorTab.Lighting], editor.AvailableTabs);
        Assert.Equal(0, fixture.Device.Reads);
        var sheet = editor.LayoutSheet!;
        Assert.Equal(new LayoutSheetMode.Arrangement(12, 4, 2, 12, 2), sheet.Mode);
        Assert.True(sheet.IsRequired);

        editor.ChooseArrangement(6, 3, 1, 0);
        Assert.False(editor.IsChoosingLayout);
        Assert.Equal("grid-6-3-1", fixture.Profiles.Saved[KeyboardFamilies.K8890Usb].LayoutId);
        Assert.Equal(9, editor.Drawing!.Controls.Count);

        editor.Select(ControlRef.Key(1));
        editor.Toggle(ModifierKey.Super);
        Assert.Equal("Win", editor.HeldModifierText);
        editor.Press(Key(C));
        Assert.Empty(editor.HeldModifiers);
        Assert.Equal(new BindingState.Presses(["Win+C"], 0, 1, 5), editor.SelectionState!.Binding);
        var drawing = editor.Drawing!;
        Assert.True(Control(drawing, ControlRef.Key(1)).IsChanged);
        Assert.Equal(new ActionCaption.Presses("Win+C"), Control(drawing, ControlRef.Key(1)).Caption);
        Assert.True(editor.HasUnsavedChanges);

        await editor.SaveAsync();
        Assert.Equal(new Notice.Saved(), editor.Notice);
        Assert.False(editor.HasUnsavedChanges);
        Assert.Single(fixture.Device.Writes);

        // The Windows tool's keys, in its arrangement: no F13–F24, non-US keys or Keypad Enter.
        Assert.Equal(KeyArrangement.Alphabetical, editor.VendorKeyArrangement);
        Assert.Empty(editor.MoreKeys);
        var off = editor.KeyBoard.Where(cap => !cap.IsEnabled).Select(cap => cap.Face);
        Assert.Equal([new BoardFace.Key(Key(0x58))], off);
        var groups = editor.KeyGroups;
        Assert.Equal(12, groups.Single(group => group.Kind == KeyGroupKind.FunctionKeys).Keys.Count);
        Assert.Equal(95, groups.Sum(group => group.Keys.Count));
        editor.Press(Key(0x58));
        Assert.Equal(new Notice.Unsupported(), editor.Notice);
        Assert.False(editor.HasUnsavedChanges);
    }

    /// <summary>A legacy keyboard is read on connecting too, as the cross-platform app's 2025-06 builds do; one that
    /// doesn't answer identification can't be read.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ALegacyKeyboardIsReadOnConnectAnUnidentifiedOneIsNot(bool answers)
    {
        await using var fixture = await Fixture.CreateAsync(device =>
            device.Identity = answers ? new KeyboardIdentity(6, 1, 0) : null);
        await fixture.PlugAsync(K8840());
        Assert.True(fixture.Editor.IsReady);
        Assert.Equal(answers ? 3 : 0, fixture.Device.Reads);
        Assert.Null(fixture.Editor.Notice);
    }

    [Fact]
    public async Task AStandardKeyboardIsReadOnConnect()
    {
        await using var fixture = await Fixture.CreateAsync(device =>
        {
            device.Identity = new KeyboardIdentity(6, 1, 10);
            device.Actions = new() { [ControlId.Key(2)] = new KeyAction.Media(MediaKey.Mute) };
        });
        await fixture.PlugAsync(K8840());
        var editor = fixture.Editor;
        Assert.True(editor.IsReady && fixture.Device.Reads > 0);
        Assert.Equal(new ConnectionStatus.Connected("1189:8840", 6, 1, true, false), editor.Status);
        Assert.False(editor.IsChoosingLayout);
        Assert.Equal(new LayoutSheetMode.Fixed(), editor.LayoutSheet!.Mode);
        Assert.Equal(
            new ActionCaption.Media(MediaChoice.Mute), Control(editor.Drawing!, ControlRef.Key(2)).Caption);
        Assert.Equal(EditorTabs.All, editor.AvailableTabs);
        Assert.Null(editor.Notice);
        Assert.Equal(KeyArrangement.Qwerty, editor.VendorKeyArrangement);
        Assert.All(editor.KeyBoard, cap => Assert.True(cap.IsEnabled));
        Assert.Equal(14, editor.MoreKeys.Count);

        editor.SetTab(EditorTab.Lighting);
        editor.Select(ControlRef.Key(1));
        var first = KeyColor.DefaultKeyColors[0];
        Assert.Equal(
            new Rgb(first.Red, first.Green, first.Blue), Control(editor.Drawing!, ControlRef.Key(1)).LedColor);
        var page = Assert.IsType<LightingPage.PerKeyColor>(editor.LightingPage);
        Assert.Equal<EquatableList<byte>>([0, 1, 2, 3, 4, 5], page.Modes);
        Assert.Equal(16, page.Keys);
        Assert.Equal(new ControlTitle.Key(1), page.Selected?.Title);
        editor.SetKeyColor(new Rgb(1, 2, 3));
        editor.SetLightingMode(4);
        page = Assert.IsType<LightingPage.PerKeyColor>(editor.LightingPage);
        Assert.Equal((byte)4, page.ActiveMode);
        Assert.Equal(new Rgb(1, 2, 3), page.Selected?.Color);

        editor.SetTab(EditorTab.Delay);
        editor.Press(Key(A));
        editor.Press(Key(C));
        editor.SetDelay(1, 250);
        Assert.Equal(
            new DelayPage.PerPress(new DelayRange(0, 60000), [new PressDelay(0, "A", 0), new PressDelay(1, "C", 250)]),
            editor.DelayPage);
    }

    [Fact]
    public async Task EditsNeedASelectionAndRoom()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PlugAsync(K8890());
        var editor = fixture.Editor;
        editor.Press(Key(A));
        Assert.Equal(new Notice.SelectControlFirst(), editor.Notice);
        editor.Select(ControlRef.Key(1));
        for (var press = 0; press < 6; press++)
            editor.Press(Key(A));
        Assert.Equal(new Notice.Full(5), editor.Notice);
        Assert.Equal(
            [
                MediaChoice.PlayPause, MediaChoice.PreviousTrack, MediaChoice.NextTrack, MediaChoice.Mute,
                MediaChoice.VolumeUp, MediaChoice.VolumeDown,
            ],
            editor.MediaChoices);
        editor.AssignMedia(MediaChoice.Calculator);
        Assert.Equal(new Notice.Unsupported(), editor.Notice);
        editor.AssignMedia(MediaChoice.Mute);
        Assert.Equal(new ActionCaption.Media(MediaChoice.Mute), editor.SelectedCaption);
        editor.Toggle(ModifierKey.Shift);
        editor.AssignHeldModifiers();
        Assert.Equal(new BindingState.Presses(["Shift"], 0, 1, 5), editor.SelectionState!.Binding);
        editor.Press(Key(A));
        Assert.Equal(new BindingState.Presses(["Shift+A"], 0, 1, 5), editor.SelectionState!.Binding);
        editor.ClearSelected();
        Assert.Equal(new BindingState.Nothing(), editor.SelectionState!.Binding);
        editor.Select(ControlRef.Key(2));
        Assert.Equal(new BindingState.Unknown(), editor.SelectionState!.Binding);
        editor.SetLightingMode(2);
        Assert.Equal(new LightingPage.ModesOnly([0, 1, 2], 2), editor.LightingPage);
    }

    [Fact]
    public async Task AFailedSaveKeepsTheChanges()
    {
        await using var fixture = await Fixture.CreateAsync(
            device => device.FailWrite = new KeyboardDeviceError.WriteFailed(-7));
        await fixture.PlugAsync(K8890());
        var editor = fixture.Editor;
        await editor.SaveAsync();
        Assert.Equal(new Notice.NothingToSave(), editor.Notice);
        editor.SetLightingMode(1);
        await editor.SaveAsync();
        Assert.Equal(new Notice.WriteFailed(-7), editor.Notice);
        Assert.True(editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task UnpluggingForgetsTheKeyboard()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PlugAsync(K8890());
        fixture.Editor.Select(ControlRef.Key(2));
        await fixture.SendAsync(ConnectionState.Searching);
        var editor = fixture.Editor;
        Assert.Equal(new ConnectionStatus.Searching(), editor.Status);
        Assert.Null(editor.SelectedControl);
        Assert.False(editor.IsReady);
        Assert.Null(editor.KeyboardInfo);
    }

    /// <summary>Unplugged, the last keyboard is still drawn, but its shape can't be changed — and its sheet goes.</summary>
    [Fact]
    public async Task UnpluggedTheShapeCantChange()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PlugAsync(K8890());
        var editor = fixture.Editor;
        Assert.True(editor.HasLayout);
        Assert.True(editor.IsChoosingLayout);
        await fixture.SendAsync(ConnectionState.Searching);
        Assert.NotNull(editor.Drawing);
        Assert.False(editor.HasLayout);
        Assert.False(editor.IsChoosingLayout);
        Assert.Null(editor.LayoutSheet);
        editor.SetChoosingLayout(true);
        Assert.False(editor.IsChoosingLayout);
    }

    /// <summary>
    /// With several keyboards plugged in, each is a choice — those with the same USB ID numbered — and choosing one asks
    /// the connection to open it. One keyboard alone is no choice.
    /// </summary>
    [Fact]
    public async Task SeveralKeyboardsAreChoices()
    {
        await using var fixture = await Fixture.CreateAsync();
        var editor = fixture.Editor;
        var first = new AttachedKeyboard("a", K8890().Usb, "1");
        var second = new AttachedKeyboard("b", K8890().Usb, "2");
        var third = new AttachedKeyboard("c", K8840().Usb, "3");
        await fixture.SendAsync(new AttachedKeyboards([first], "a"));
        Assert.Empty(editor.KeyboardChoices);
        await fixture.SendAsync(new AttachedKeyboards([first, second, third], "b"));
        Assert.Equal(
            [
                new KeyboardChoice("a", "1189:8890 (1)", false),
                new KeyboardChoice("b", "1189:8890 (2)", true),
                new KeyboardChoice("c", "1189:8840", false),
            ],
            editor.KeyboardChoices);
        editor.SelectKeyboard("c");
        Assert.Equal(["c"], fixture.Connection.Selected);
    }

    /// <summary>
    /// A read during which the keyboard went is dropped — what came after came from no keyboard, or another: the
    /// keyboard keeps what it had, and the app says the keyboard went.
    /// </summary>
    [Fact]
    public async Task AReadWhoseKeyboardWentIsDropped()
    {
        await using var fixture = await Fixture.CreateAsync(device =>
        {
            device.Identity = new KeyboardIdentity(6, 1, 10);
            device.Actions = new() { [ControlId.Key(2)] = new KeyAction.Media(MediaKey.Mute) };
        });
        await fixture.PlugAsync(K8840());
        var editor = fixture.Editor;
        var held = fixture.Device.Hold();
        var reading = editor.ReadAsync();
        Assert.Equal(Activity.Reading, editor.Activity);
        fixture.Device.Actions = new() { [ControlId.Key(2)] = new KeyAction.Media(MediaKey.VolumeUp) };
        await fixture.SendAsync(ConnectionState.Searching);
        held.SetResult();
        await reading;
        Assert.Null(editor.Activity);
        Assert.Equal(new Notice.KeyboardGone(), editor.Notice);
        Assert.Equal(
            new ActionCaption.Media(MediaChoice.Mute), Control(editor.Drawing!, ControlRef.Key(2)).Caption);
    }

    /// <summary>
    /// Another keyboard with the same USB ID in place of the one being read: the read is dropped, and the new
    /// keyboard — whose identification waited for it — is identified, and read, once it ends.
    /// </summary>
    [Fact]
    public async Task TheKeyboardThatCameDuringAReadIsIdentifiedAfterIt()
    {
        await using var fixture = await Fixture.CreateAsync(device => device.Identity = new KeyboardIdentity(6, 1, 10));
        await fixture.PlugAsync(K8840());
        var editor = fixture.Editor;
        var device = fixture.Device;
        var held = device.Hold();
        var reading = editor.ReadAsync();
        device.Identity = new KeyboardIdentity(3, 1, 10);
        device.Actions = new() { [ControlId.Key(2)] = new KeyAction.Media(MediaKey.Mute) };
        await fixture.SendAsync(ConnectionState.Searching);
        await fixture.PlugAsync(K8840());
        Assert.Equal(Activity.Reading, editor.Activity);
        Assert.Equal(new ConnectionStatus.Connected("1189:8840", null, null, false, false), editor.Status);

        held.SetResult();
        await reading;
        Assert.Equal(new Notice.KeyboardGone(), editor.Notice);
        Assert.True(editor.IsReady);
        Assert.Equal(new ConnectionStatus.Connected("1189:8840", 3, 1, true, false), editor.Status);
        Assert.Equal(
            new ActionCaption.Media(MediaChoice.Mute), Control(editor.Drawing!, ControlRef.Key(2)).Caption);
    }

    /// <summary>
    /// A save during which the keyboard went says so, and the changes stay unsaved — what reached which keyboard isn't
    /// known.
    /// </summary>
    [Fact]
    public async Task ASaveWhoseKeyboardWentStaysUnsaved()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PlugAsync(K8890());
        var editor = fixture.Editor;
        editor.SetLightingMode(1);
        var held = fixture.Device.Hold();
        var saving = editor.SaveAsync();
        Assert.Equal(Activity.Saving, editor.Activity);
        await fixture.SendAsync(ConnectionState.Searching);
        held.SetResult();
        await saving;
        Assert.Null(editor.Activity);
        Assert.Equal(new Notice.KeyboardGone(), editor.Notice);
        Assert.True(editor.HasUnsavedChanges);
    }

    /// <summary>Another keyboard can be chosen, but not while a transfer is under way.</summary>
    [Fact]
    public async Task CanSwitchKeyboardFollowsTheActivity()
    {
        await using var fixture = await Fixture.CreateAsync();
        var editor = fixture.Editor;
        Assert.True(editor.CanSwitchKeyboard);
        await fixture.PlugAsync(K8890());
        Assert.True(editor.CanSwitchKeyboard);
        editor.SetLightingMode(1);
        var held = fixture.Device.Hold();
        var saving = editor.SaveAsync();
        Assert.False(editor.CanSwitchKeyboard);
        held.SetResult();
        await saving;
        Assert.Equal(new Notice.Saved(), editor.Notice);
        Assert.True(editor.CanSwitchKeyboard);
    }

    /// <summary>While the keyboard is being read, choosing another — plugged in or simulated — does nothing.</summary>
    [Fact]
    public async Task NoOtherKeyboardWhileReading()
    {
        await using var fixture = await Fixture.CreateAsync(device => device.Identity = new KeyboardIdentity(6, 1, 10));
        await fixture.SendAsync(new AttachedKeyboards([new("a", K8840().Usb, "1"), new("b", K8840().Usb, "2")], "a"));
        await fixture.PlugAsync(K8840());
        var editor = fixture.Editor;
        var held = fixture.Device.Hold();
        var reading = editor.ReadAsync();
        Assert.False(editor.CanSwitchKeyboard);
        editor.SelectKeyboard("b");
        editor.SelectSimulator("8890-3");
        Assert.Empty(fixture.Connection.Selected);
        Assert.Empty(fixture.Sources.Selected);
        Assert.Null(editor.PendingSwitch);
        Assert.Null(editor.ActiveSimulatorId);

        held.SetResult();
        await reading;
        Assert.Equal(new Notice.ReadComplete(), editor.Notice);
        editor.SelectKeyboard("b");
        Assert.Equal(["b"], fixture.Connection.Selected);
    }

    /// <summary>
    /// With changes not saved, choosing another keyboard — plugged in or simulated — asks first. Cancelled, the
    /// keyboard and its changes stay; confirmed, the switch is made. When the keyboard goes meanwhile, there is nothing
    /// to ask.
    /// </summary>
    [Fact]
    public async Task SwitchingWithUnsavedChangesAsksFirst()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.SendAsync(new AttachedKeyboards([new("a", K8890().Usb, "1"), new("b", K8890().Usb, "2")], "a"));
        await fixture.PlugAsync(K8890());
        var editor = fixture.Editor;
        editor.SetLightingMode(1);

        editor.SelectKeyboard("b");
        Assert.Equal(new KeyboardSwitch.Keyboard("b"), editor.PendingSwitch);
        Assert.Empty(fixture.Connection.Selected);
        editor.CancelSwitch();
        Assert.Null(editor.PendingSwitch);
        Assert.Empty(fixture.Connection.Selected);
        Assert.True(editor.IsReady);
        Assert.True(editor.HasUnsavedChanges);
        editor.SelectKeyboard("b");
        editor.ConfirmSwitch();
        Assert.Null(editor.PendingSwitch);
        Assert.Equal(["b"], fixture.Connection.Selected);

        editor.SelectSimulator("8890-3");
        Assert.Equal(new KeyboardSwitch.Simulator("8890-3"), editor.PendingSwitch);
        editor.CancelSwitch();
        Assert.Empty(fixture.Sources.Selected);
        Assert.Null(editor.ActiveSimulatorId);
        editor.SelectSimulator("8890-3");
        editor.ConfirmSwitch();
        Assert.Null(editor.PendingSwitch);
        Assert.Equal("8890-3", editor.ActiveSimulatorId);
        var simulated = Assert.IsType<KeyboardSource.Simulated>(Assert.Single(fixture.Sources.Selected));
        Assert.Equal("8890-3", simulated.Model.Id);

        editor.SelectSimulator(null);
        Assert.Equal(new KeyboardSwitch.Simulator(null), editor.PendingSwitch);
        await fixture.SendAsync(ConnectionState.Searching);
        Assert.Null(editor.PendingSwitch);
    }

    /// <summary>Without changes to lose, the switch is made at once; to where the app already is, none.</summary>
    [Fact]
    public async Task WithoutUnsavedChangesThereIsNoQuestion()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.SendAsync(new AttachedKeyboards([new("a", K8890().Usb, "1"), new("b", K8890().Usb, "2")], "a"));
        await fixture.PlugAsync(K8890());
        var editor = fixture.Editor;
        editor.SelectKeyboard("a");
        editor.SelectSimulator(null);
        Assert.Empty(fixture.Connection.Selected);
        Assert.Empty(fixture.Sources.Selected);

        editor.SelectKeyboard("b");
        Assert.Null(editor.PendingSwitch);
        Assert.Equal(["b"], fixture.Connection.Selected);
        editor.SelectSimulator("8890-3");
        Assert.Null(editor.PendingSwitch);
        Assert.Equal("8890-3", editor.ActiveSimulatorId);
        editor.ConfirmSwitch();
        Assert.Single(fixture.Sources.Selected);
    }

    /// <summary>
    /// Once the keyboard has gone, its changes can't be saved anywhere: choosing another — plugged in or simulated —
    /// doesn't ask.
    /// </summary>
    [Fact]
    public async Task UnpluggedWithChangesThereIsNoQuestion()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.SendAsync(new AttachedKeyboards([new("a", K8890().Usb, "1"), new("b", K8890().Usb, "2")], "a"));
        await fixture.PlugAsync(K8890());
        var editor = fixture.Editor;
        editor.SetLightingMode(1);
        await fixture.SendAsync(ConnectionState.Searching);
        Assert.True(editor.HasUnsavedChanges);

        editor.SelectKeyboard("b");
        Assert.Null(editor.PendingSwitch);
        Assert.Equal(["b"], fixture.Connection.Selected);
        editor.SelectSimulator("8890-3");
        Assert.Null(editor.PendingSwitch);
        Assert.Equal("8890-3", editor.ActiveSimulatorId);
    }

    /// <summary>A 884x keyboard that doesn't answer picks one of the app's layouts; rotating turns the drawing.</summary>
    [Fact]
    public async Task TheLayoutSheet()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PlugAsync(K8840());
        var editor = fixture.Editor;
        var catalog = Assert.IsType<LayoutSheetMode.Catalog>(editor.LayoutSheet?.Mode);
        Assert.Equal(26, catalog.Choices.Count);
        Assert.Equal("15add3", catalog.SelectedId);
        editor.ChooseLayout("9add2", 1);
        Assert.Equal(1, editor.LayoutSheet!.QuarterTurns);
        var upright = editor.PreviewDrawing("9add2", 0)!;
        var turned = editor.PreviewDrawing("9add2", 1)!;
        Assert.True(Math.Abs(upright.Bounds.Width - turned.Bounds.Height) < 0.001);
        Assert.Null(editor.PreviewArrangement(0, 1, 0, 0));
        editor.Rotate();
        Assert.Equal(2, editor.LayoutSheet!.QuarterTurns);
    }

    [Fact]
    public async Task ListenersHearEveryChange()
    {
        await using var fixture = await Fixture.CreateAsync();
        var heard = 0;
        fixture.Editor.Changed += (_, _) => heard++;
        await fixture.PlugAsync(K8890());
        Assert.True(heard > 0);
        var before = heard;
        fixture.Editor.SetTab(EditorTab.Media);
        Assert.Equal(before + 1, heard);
    }

    [Fact]
    public async Task ANoticeGoesOnlyWhenItIsStillShown()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PlugAsync(K8890());
        var editor = fixture.Editor;
        editor.Press(Key(A));
        var first = editor.NoticeSerial;
        editor.Select(ControlRef.Knob(1, KnobAction.Press));
        editor.AssignMedia(MediaChoice.Calculator);
        Assert.Equal(new Notice.Unsupported(), editor.Notice);
        editor.ExpireNotice(first);
        Assert.Equal(new Notice.Unsupported(), editor.Notice);
        editor.ExpireNotice(editor.NoticeSerial);
        Assert.Null(editor.Notice);
    }

    /// <summary>
    /// A read whose answers for some controls didn't come says so — the read made on connecting too, though it says
    /// nothing when all came.
    /// </summary>
    [Fact]
    public async Task AReadThatLeftControlsUnreadSaysSo()
    {
        await using var fixture = await Fixture.CreateAsync(device =>
        {
            device.Identity = new KeyboardIdentity(6, 1, 10);
            device.Unread = [ControlId.Key(6)];
        });
        await fixture.PlugAsync(K8840());
        var editor = fixture.Editor;
        Assert.Equal(new Notice.ReadIncomplete(), editor.Notice);
        Assert.True(editor.Notice!.IsError);

        fixture.Device.Unread = [];
        await editor.ReadAsync();
        Assert.Equal(new Notice.ReadComplete(), editor.Notice);
        fixture.Device.FailRead = new KeyboardDeviceError.NoReply();
        await editor.ReadAsync();
        Assert.Equal(new Notice.ReadFailed(), editor.Notice);
    }

    /// <summary>
    /// After a save, a keyboard that reads back is read: what matches is saved; what reads back otherwise stays
    /// unsaved; a keyboard that can't be read back leaves it saved, and says so.
    /// </summary>
    [Fact]
    public async Task ASaveIsReadBack()
    {
        await using var fixture = await Fixture.CreateAsync(device => device.Identity = new KeyboardIdentity(6, 1, 10));
        await fixture.PlugAsync(K8840());
        var editor = fixture.Editor;
        var device = fixture.Device;
        editor.Select(ControlRef.Key(1));
        var reads = device.Reads;

        editor.AssignMedia(MediaChoice.Mute);
        device.Actions = new() { [ControlId.Key(1)] = new KeyAction.Media(MediaKey.Mute) };
        await editor.SaveAsync();
        Assert.Equal(new Notice.Saved(), editor.Notice);
        Assert.False(editor.HasUnsavedChanges);
        Assert.Equal(reads + 1, device.Reads);

        editor.AssignMedia(MediaChoice.VolumeUp);
        await editor.SaveAsync();
        Assert.Equal(new Notice.SavedButDiffers(), editor.Notice);
        Assert.True(editor.HasUnsavedChanges);
        var drawn = Control(editor.Drawing!, ControlRef.Key(1));
        Assert.True(drawn.IsChanged);
        Assert.Equal(new ActionCaption.Media(MediaChoice.VolumeUp), drawn.Caption);

        device.FailRead = new KeyboardDeviceError.NoReply();
        await editor.SaveAsync();
        Assert.Equal(new Notice.SavedUnchecked(), editor.Notice);
        Assert.True(editor.Notice!.IsError);
        Assert.False(editor.HasUnsavedChanges);
        Assert.Equal(3, device.Writes.Count);
    }

    /// <summary>
    /// A keyboard of a type the app wasn't tried with is said so once it is connected — in place of what the read on
    /// connecting said — and its information says so too.
    /// </summary>
    [Theory]
    [InlineData(10, null)]
    [InlineData(11, null)]
    [InlineData(12, 12)]
    [InlineData(2, 2)]
    public async Task AnUntestedTypeIsSaidSo(byte kind, int? untested)
    {
        await using var fixture = await Fixture.CreateAsync(device =>
        {
            device.Identity = new KeyboardIdentity(12, 2, kind);
            device.Unread = [ControlId.Key(12)];
        });
        await fixture.PlugAsync(K8840());
        var editor = fixture.Editor;
        var standard = kind >= 10;
        Notice expected = untested is { } type ? new Notice.UntestedFirmware(type) : new Notice.ReadIncomplete();
        Assert.Equal(expected, editor.Notice);
        var info = editor.KeyboardInfo!;
        Assert.Equal(new Firmware.Reported(kind, 12, 2, untested is null), info.Firmware);
        Assert.Equal(standard ? new InfoCommands.Standard() : new InfoCommands.Legacy(), info.Commands);
        Assert.Equal(TimeSpan.FromSeconds(10), new Notice.UntestedFirmware(12).Lifetime);
    }
}
