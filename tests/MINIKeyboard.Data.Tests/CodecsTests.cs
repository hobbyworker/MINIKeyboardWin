using MINIKeyboard.Data.Protocols;
using MINIKeyboard.Domain;

namespace MINIKeyboard.Data.Tests;

public class CodecsTests
{
    private static KeyAction Keys(params KeyStroke[] strokes) => new KeyAction.Keys(new KeyMacro([.. strokes]));

    private static byte[] Bytes(params byte[] bytes) => bytes;

    /// <summary>Packets as hex, so a difference shows where it is.</summary>
    private static string[] Hex(params IEnumerable<byte[]> packets) => [.. packets.Select(Convert.ToHexString)];

    private static KeyboardDeviceError ErrorOf(Action action) => Assert.Throws<KeyboardDeviceException>(action).Error;

    // The 1189:8890's packets, as the Windows tool sends them.

    [Fact]
    public void AKeyIsTheLayerItsGroupsAndTheSave()
    {
        var action = Keys(new KeyStroke(Modifiers.Super, 0x06), KeyStroke.OfKey(0x28));
        Assert.Equal(
            Hex(
                [0xA1, 0x02, 0, 0, 0, 0, 0, 0],
                [0x03, 0x21, 0x02, 0x00, 0x08, 0x00, 0, 0],
                [0x03, 0x21, 0x02, 0x01, 0x08, 0x06, 0, 0],
                [0x03, 0x21, 0x02, 0x02, 0x00, 0x28, 0, 0],
                [0xAA, 0xAA, 0, 0, 0, 0, 0, 0]),
            Hex(K8890.Download(3, action, 1, 3)));
    }

    /// <summary>Report ID 0 takes no layer switch and no layer in the type byte.</summary>
    [Fact]
    public void ReportIdZeroHasNoLayers()
    {
        var packets = K8890.Download(1, Keys(KeyStroke.OfKey(0x04)), 2, 0);
        Assert.Equal(Bytes(0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0, 0), packets[0]);
        Assert.Equal(3, packets.Count);
    }

    [Fact]
    public void ModifiersAloneAndClearedKeys()
    {
        var alone = Keys(new KeyStroke(Modifiers.Control | Modifiers.Shift, 0));
        Assert.Equal(Hex([14, 0x11, 0, 0, 0x03, 0, 0, 0]), Hex(K8890.KeyPackets(14, alone, 0, 3)));
        Assert.Equal(Hex([14, 0x11, 0, 0, 0, 0, 0, 0]), Hex(K8890.KeyPackets(14, KeyAction.Unassigned, 0, 3)));
    }

    [Fact]
    public void MediaKeysPerReportId()
    {
        static KeyAction Media(MediaKey key) => new KeyAction.Media(key);
        var playPause = Media(MediaKey.PlayPause);
        Assert.Equal(Hex([2, 0x12, 0xCD, 0x00, 0, 0, 0, 0]), Hex(K8890.KeyPackets(2, playPause, 0, 3)));
        Assert.Equal(Hex([2, 0x02, 0x40, 0x00, 0, 0, 0, 0]), Hex(K8890.KeyPackets(2, playPause, 0, 0)));
        var volumeDown = Media(MediaKey.VolumeDown);
        Assert.Equal(Hex([2, 0x12, 0x80, 0x00, 0, 0, 0, 0]), Hex(K8890.KeyPackets(2, volumeDown, 0, 2)));
        Assert.IsType<KeyboardDeviceError.Unsupported>(
            ErrorOf(() => K8890.KeyPackets(2, Media(MediaKey.Calculator), 0, 2)));
    }

    [Fact]
    public void Mouse()
    {
        static KeyAction Mouse(MouseAction action) => new KeyAction.Mouse(action);
        var shiftWheelDown = Mouse(MouseAction.ShiftWheelDown);
        Assert.Equal(Hex([4, 0x13, 0x00, 0, 0, 0xFF, 0x02, 0]), Hex(K8890.KeyPackets(4, shiftWheelDown, 0, 3)));
        var rightClick = Mouse(MouseAction.RightClick);
        Assert.Equal(Hex([4, 0x13, 0x02, 0, 0, 0x00, 0x00, 0]), Hex(K8890.KeyPackets(4, rightClick, 0, 3)));
    }

    /// <summary>The lights, their save, and a save with no key in it (it ends the key repeat the lights' save starts).</summary>
    [Fact]
    public void K8890Lighting()
    {
        Assert.Equal(
            Hex(
                [0xA1, 0x01, 0, 0, 0, 0, 0, 0],
                [0xB0, 0x18, 0x02, 0, 0, 0, 0, 0],
                [0xAA, 0xA1, 0, 0, 0, 0, 0, 0],
                [0xA1, 0x01, 0, 0, 0, 0, 0, 0],
                [0xAA, 0xAA, 0, 0, 0, 0, 0, 0]),
            Hex(K8890.DownloadLighting(2, 0, 3)));
    }

    /// <summary>With the setting off, just what the vendor's tool sends.</summary>
    [Fact]
    public void K8890LightingAsTheVendorsTool()
    {
        Assert.Equal(
            Hex([0xA1, 0x01, 0, 0, 0, 0, 0, 0], [0xB0, 0x18, 0x02, 0, 0, 0, 0, 0], [0xAA, 0xA1, 0, 0, 0, 0, 0, 0]),
            Hex(K8890.DownloadLighting(2, 0, 3, endsKeyRepeat: false)));
    }

    // The 884x records, as the cross-platform app writes and reads them.

    [Fact]
    public void StandardEntriesTakeAModifierEach()
    {
        var action = new KeyAction.Keys(new KeyMacro(
        [
            new KeyStroke(Modifiers.Shift | Modifiers.Super, 0x1D).WithDelay(300),
            KeyStroke.OfKey(0x28).WithDelay(50),
        ]));
        var record = Records.Standard(action, 4, 2);
        Assert.Equal(64, record.Length);
        Assert.Equal(Bytes(0xFD, 4, 3, 1, 0, 4), record[0..6]);
        Assert.Equal(Bytes(0x01, 0x2C, 0xF2, 0x00, 0x00, 0xF4, 0x00, 0x00, 0x1D, 0x00, 0x32, 0x28), record[6..18]);
        Assert.Equal(action, Records.Decode(record, RecordFormat.Standard));
    }

    [Fact]
    public void StandardMediaAndMouse()
    {
        var media = Records.Standard(new KeyAction.Media(MediaKey.WebBack), 1, 0);
        Assert.True(media[3] == 2 && media[5] == 2 && media[8] == 0x24 && media[11] == 0x02);
        Assert.Equal(new KeyAction.Media(MediaKey.WebBack), Records.Decode(media, RecordFormat.Standard));
        var mouse = Records.Standard(new KeyAction.Mouse(MouseAction.ControlWheelUp), 1, 0);
        Assert.True(mouse[3] == 3 && mouse[4] == 1 && mouse[5] == 4 && mouse[8] == 0xF1 && mouse[11] == 0 && mouse[20] == 0x01);
        Assert.Equal(new KeyAction.Mouse(MouseAction.ControlWheelUp), Records.Decode(mouse, RecordFormat.Standard));
        byte[] gesture = [.. mouse];
        gesture[4] = 4;
        Assert.Null(Records.Decode(gesture, RecordFormat.Standard));
    }

    [Fact]
    public void LegacyPairsAndTheWait()
    {
        var action = new KeyAction.Keys(
            new KeyMacro([new KeyStroke(Modifiers.Control, 0x06), KeyStroke.OfKey(0x19)], 500));
        var record = Records.Encode(action, 16, 0, RecordFormat.Legacy);
        Assert.Equal(50, record.Length);
        Assert.Equal(Bytes(0xFD, 16, 1, 5, 0xF4, 0x01, 0, 0, 0, 2, 0x01, 0x06, 0x00, 0x19), record[0..14]);
        Assert.Equal(action, Records.Decode(record, RecordFormat.Legacy));
        var plain = Records.Encode(Keys(KeyStroke.OfKey(0x04)), 1, 0, RecordFormat.Legacy);
        Assert.Equal(1, plain[3]);
    }

    [Fact]
    public void LegacyMediaAndMouse()
    {
        var media = Records.Encode(new KeyAction.Media(MediaKey.Mute), 1, 0, RecordFormat.Legacy);
        Assert.True(media[3] == 2 && media[9] == 2 && media[10] == 0xE2 && media[11] == 0);
        Assert.Equal(new KeyAction.Media(MediaKey.Mute), Records.Decode(media, RecordFormat.Legacy));
        var wheel = Records.Encode(new KeyAction.Mouse(MouseAction.AltWheelDown), 1, 0, RecordFormat.Legacy);
        Assert.True(wheel[3] == 3 && wheel[9] == 1 && wheel[10] == 0x04 && wheel[11] == 0 && wheel[14] == 0xFF);
        Assert.Equal(new KeyAction.Mouse(MouseAction.AltWheelDown), Records.Decode(wheel, RecordFormat.Legacy));
    }

    [Fact]
    public void ModifiersAloneSitAfterTheLastPair()
    {
        var alone = Keys(new KeyStroke(Modifiers.Alt, 0));
        var record = Records.Encode(alone, 1, 0, RecordFormat.Legacy);
        Assert.True(record[9] == 0 && record[10] == 0x04 && record[11] == 0);
        Assert.Equal(alone, Records.Decode(record, RecordFormat.Legacy));
        var standard = Records.Standard(alone, 1, 0);
        Assert.True(standard[5] == 1 && standard[8] == 0xF3);
        Assert.Equal(alone, Records.Decode(standard, RecordFormat.Standard));
    }

    /// <summary>
    /// The form keyboards of the 1189:8840 kind take: as the legacy pairs, marked <c>FE</c>, a media key with a count
    /// of 0 and a wheel move with 3.
    /// </summary>
    [Fact]
    public void UnidentifiedRecords()
    {
        var key = Records.Unidentified(Keys(new KeyStroke(Modifiers.Super, 0x06)), 17, 0);
        Assert.Equal(Bytes(0xFE, 17, 1, 1, 0, 0, 0, 0, 0, 1, 0x08, 0x06), key[0..12]);
        var media = Records.Unidentified(new KeyAction.Media(MediaKey.VolumeUp), 1, 1);
        Assert.Equal(Bytes(0xFE, 1, 2, 2, 0, 0, 0, 0, 0, 0, 0xE9, 0x00), media[0..12]);
        var wheel = Records.Unidentified(new KeyAction.Mouse(MouseAction.WheelUp), 1, 0);
        Assert.Equal(Bytes(0xFE, 1, 1, 3, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0x01), wheel[0..15]);
        var click = Records.Unidentified(new KeyAction.Mouse(MouseAction.LeftClick), 1, 0);
        Assert.Equal(Bytes(0xFE, 1, 1, 3, 0, 0, 0, 0, 0, 1, 0, 0x01), click[0..12]);
    }

    [Fact]
    public void Cleared()
    {
        var record = Records.Standard(KeyAction.Unassigned, 2, 0);
        Assert.Equal(Bytes(0xFD, 2, 1, 1, 0, 0), record[0..6]);
        Assert.Equal(KeyAction.Unassigned, Records.Decode(record, RecordFormat.Standard));
        Assert.Equal(KeyAction.Unassigned, Records.Decode([0xFD, 2, 1, 0], RecordFormat.Legacy));
        Assert.Null(Records.Decode([0xFD, 2, 1, 9], RecordFormat.Legacy));
    }

    [Fact]
    public void LegacyLighting()
    {
        var record = Records.LegacyLighting(LightColor.Blue, 3, 2, Records.LegacyLength);
        Assert.Equal(Bytes(0xFE, 0xB0, 3, 8, 0, 0, 0, 0, 0, 1, 0, 0x63), record[0..12]);
    }

    [Fact]
    public void Answers()
    {
        Assert.Equal(new KeyboardIdentity(6, 1, 10), K884x.Identity([0x03, 0xFB, 6, 1, 10]));
        Assert.Null(K884x.Identity([0x03, 0xFB]));
        byte[] reply = [0x03, 0xB0, 4, .. Enumerable.Repeat((byte)9, 48)];
        Assert.Equal(
            new LayerLighting.PerKey(4, [.. Enumerable.Repeat(new KeyColor(9, 9, 9), 16)]),
            K884x.LightingReply(reply));
        Assert.Equal(Bytes(0xFA, 25, 0, 1), K884x.ReadRecords(0, RecordFormat.Standard));
        Assert.Equal(Bytes(0xFA, 0x0F, 3, 3), K884x.ReadRecords(2, RecordFormat.Legacy));
        Assert.Equal(25, K884x.ReadReplyCount(RecordFormat.Standard));
        Assert.Equal(24, K884x.ReadReplyCount(RecordFormat.Legacy));
        Assert.Null(K884x.ReadRecords(0, RecordFormat.Unidentified));
    }

    [Fact]
    public void Framing()
    {
        Assert.Equal(Bytes(3, 0xAA, 0xAA, 0, 0, 0, 0, 0, 0), new ReportFraming(3, 9).Frame([0xAA, 0xAA]));
        Assert.Equal(Bytes(0xAA, 0, 0, 0, 0, 0, 0, 0), new ReportFraming(0, 8).Frame([0xAA]));
        Assert.Equal(65, new ReportFraming(3, 65).Frame(K884x.Identify).Length);
    }

    // The order a save writes in.

    private static KeyboardLayout SixAndOne => KeyboardLayout.Named("6add1")!;

    private static KeyboardConfiguration Configuration(Action<KeyboardConfiguration> edit)
    {
        var configuration = new KeyboardConfiguration(3);
        edit(configuration);
        return configuration;
    }

    private static string[] Prefixes(WritePlan plan) => Hex(plan.Steps.Select(step => step.Payload[..3]));

    private static string[] Payloads(WritePlan plan) => Hex(plan.Steps.Select(step => step.Payload));

    /// <summary>
    /// Every changed record, then all three layers' lights, then one commit (<c>Widget::HID_write</c>).
    /// </summary>
    [Fact]
    public void StandardPlan()
    {
        var configuration = Configuration(configuration =>
        {
            configuration.Set(new KeyAction.Media(MediaKey.Mute), ControlId.Key(2), 1);
            configuration.Set(KeyAction.Unassigned, ControlId.Knob(1, KnobPart.Press), 0);
            for (var layer = 0; layer < 3; layer++)
                configuration.ApplyReadLighting(new LayerLighting.PerKey(0, KeyColor.DefaultKeyColors), layer);
            configuration.SetLighting(new LayerLighting.PerKey(2, KeyColor.DefaultKeyColors), 2);
        });
        var plan = new WritePlan(configuration, new CommandSet.Records(RecordFormat.Standard), SixAndOne);
        Assert.Equal(
            Hex([0xFD, 17, 1], [0xFD, 2, 2], [0xFE, 0xB0, 0], [0xFE, 0xB0, 1], [0xFE, 0xB0, 2], [0xFD, 0xFE, 0xFF]),
            Prefixes(plan));
        Assert.Equal(TimeSpan.FromMilliseconds(200), plan.Steps[^1].PauseAfter);
        Assert.Equal(2, plan.Steps[4].Payload[3]);
    }

    /// <summary>Lights alone are written without a commit, as the app does.</summary>
    [Fact]
    public void StandardLightsAlone()
    {
        var configuration = Configuration(configuration =>
            configuration.SetLighting(new LayerLighting.PerKey(1, KeyColor.DefaultKeyColors), 0));
        var plan = new WritePlan(configuration, new CommandSet.Records(RecordFormat.Standard), SixAndOne);
        Assert.Equal(Hex([0xFE, 0xB0, 0]), Prefixes(plan));
    }

    /// <summary>Each layer's records and lights, then that layer's commit.</summary>
    [Fact]
    public void LegacyPlan()
    {
        var configuration = Configuration(configuration =>
        {
            configuration.Set(new KeyAction.Media(MediaKey.Mute), ControlId.Key(1), 0);
            configuration.SetLighting(new LayerLighting.ColorMode(LightColor.Green, 1), 0);
            configuration.Set(new KeyAction.Media(MediaKey.Mute), ControlId.Key(6), 2);
        });
        var plan = new WritePlan(configuration, new CommandSet.Records(RecordFormat.Legacy), SixAndOne);
        Assert.Equal(
            Hex([0xFD, 1, 1], [0xFE, 0xB0, 1], [0xFD, 0xFE, 0xFF], [0xFD, 6, 3], [0xFD, 0xFE, 0xFF]),
            Prefixes(plan));
    }

    [Fact]
    public void UnidentifiedPlan()
    {
        var configuration = Configuration(configuration =>
        {
            configuration.Set(new KeyAction.Media(MediaKey.Mute), ControlId.Knob(1, KnobPart.RotateRight), 0);
            configuration.SetLighting(new LayerLighting.ColorMode(LightColor.Red, 2), 1);
        });
        var plan = new WritePlan(configuration, new CommandSet.Records(RecordFormat.Unidentified), SixAndOne);
        Assert.Equal(
            Hex(
                Records.Unidentified(new KeyAction.Media(MediaKey.Mute), 18, 0),
                [0xAA, 0xAA],
                [0xFD, 0xFE, 0xFF],
                [0xAA, 0xAA],
                [0xFB, 0xFB, 0xFB, 0x01],
                [0xFA, 0x0F, 0x03, 1, 0x7F],
                [0xFA, 0x0F, 0x03, 2, 0x7F],
                [0xFA, 0x0F, 0x03, 3, 0x7F],
                [0xFE, 0xB0, 2, 8, 0, 0, 0, 0, 0, 1, 0, 0x12],
                [0xFD, 0xFE, 0xFF]),
            Payloads(plan));
    }

    /// <summary>One key at a time on the 8890, knobs numbered from 13 — the lights first.</summary>
    [Fact]
    public void K8890Plan()
    {
        var configuration = Configuration(configuration =>
        {
            configuration.Set(Keys(KeyStroke.OfKey(0x04)), ControlId.Knob(1, KnobPart.RotateRight), 0);
            configuration.SetLighting(new LayerLighting.ModeOnly(1), 0);
        });
        var plan = new WritePlan(configuration, new CommandSet.Packets(3), SixAndOne);
        Assert.Equal(
            Hex(
                [0xA1, 1, 0, 0, 0, 0, 0, 0],
                [0xB0, 0x18, 1, 0, 0, 0, 0, 0],
                [0xAA, 0xA1, 0, 0, 0, 0, 0, 0],
                [0xA1, 1, 0, 0, 0, 0, 0, 0],
                [0xAA, 0xAA, 0, 0, 0, 0, 0, 0],
                [0xA1, 1, 0, 0, 0, 0, 0, 0],
                [15, 0x11, 1, 0, 0, 0, 0, 0],
                [15, 0x11, 1, 1, 0, 0x04, 0, 0],
                [0xAA, 0xAA, 0, 0, 0, 0, 0, 0]),
            Payloads(plan));
    }

    /// <summary>What the app doesn't know is never written.</summary>
    [Fact]
    public void OnlyChanges()
    {
        var configuration = Configuration(configuration => configuration.ApplyRead(
            new Dictionary<ControlId, KeyAction> { [ControlId.Key(1)] = new KeyAction.Media(MediaKey.Mute) }, 0));
        Assert.Empty(new WritePlan(configuration, new CommandSet.Records(RecordFormat.Standard), SixAndOne).Steps);
    }

    // Where each control is stored: the 8890's numbers, and the records of the cross-platform app's layouts.

    [Fact]
    public void K8890Numbers()
    {
        Assert.Equal((byte)1, Numbers.K8890Number(ControlId.Key(1)));
        Assert.Equal((byte)12, Numbers.K8890Number(ControlId.Key(12)));
        Assert.Null(Numbers.K8890Number(ControlId.Key(13)));
        Assert.Equal((byte)13, Numbers.K8890Number(ControlId.Knob(1, KnobPart.RotateLeft)));
        Assert.Equal((byte)14, Numbers.K8890Number(ControlId.Knob(1, KnobPart.Press)));
        Assert.Equal((byte)18, Numbers.K8890Number(ControlId.Knob(2, KnobPart.RotateRight)));
        Assert.Null(Numbers.K8890Number(ControlId.Knob(3, KnobPart.Press)));
        Assert.Null(Numbers.K8890Number(ControlId.Knob(1, KnobPart.PressRotateLeft)));
    }

    /// <summary>Whatever the domain lets the 8890 take has a number.</summary>
    [Fact]
    public void EveryAddressableControlHasANumber()
    {
        var capabilities = new KeyboardCapabilities(new CommandSet.Packets(3));
        var controls = Enumerable.Range(1, 16).Select(number => ControlId.Key((byte)number))
            .Concat(Enumerable.Range(1, 3).SelectMany(knob =>
                KnobParts.All.Select(part => ControlId.Knob((byte)knob, part))));
        Assert.All(controls, control =>
            Assert.Equal(capabilities.CanAddress(control), Numbers.K8890Number(control) is not null));
    }

    /// <summary>Every control of every layout has a record of its own, from 1 to 25.</summary>
    [Fact]
    public void EveryControlHasARecord()
    {
        Assert.All(KeyboardLayout.All, layout =>
        {
            var records = layout.Controls.Select(control => Numbers.Record(control, layout)).OfType<byte>().ToList();
            Assert.Equal(layout.Controls.Count, records.Count);
            Assert.Equal(records.Count, records.Distinct().Count());
            Assert.All(records, record => Assert.InRange(record, (byte)1, (byte)25));
            Assert.All(layout.Controls, control =>
                Assert.Equal(control, Numbers.ControlInRecord(Numbers.Record(control, layout)!.Value, layout)));
        });
        var dial = KeyboardLayout.Named("0add1")!;
        Assert.Equal((byte)19, Numbers.Record(ControlId.Knob(1, KnobPart.PressRotateLeft), dial));
        Assert.Equal((byte)21, Numbers.Record(ControlId.Knob(1, KnobPart.PressRotateRight), dial));
        Assert.Equal((byte)14, Numbers.Record(ControlId.Knob(4, KnobPart.Press), KeyboardLayout.Named("12add4")!));
        Assert.Null(Numbers.Record(ControlId.Key(7), SixAndOne));
    }
}
