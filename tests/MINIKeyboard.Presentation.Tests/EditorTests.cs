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
}
