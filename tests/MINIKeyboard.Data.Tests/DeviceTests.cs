using System.Buffers.Binary;
using MINIKeyboard.Data.Protocols;
using MINIKeyboard.Domain;
using MINIKeyboard.Domain.UseCases;

namespace MINIKeyboard.Data.Tests;

/// <summary>The device repository against simulated firmware: the same codecs as with a keyboard on USB.</summary>
public class DeviceTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    internal static async Task<T> First<T>(IAsyncEnumerable<T> updates)
    {
        await using var enumerator = updates.GetAsyncEnumerator(Cancellation);
        Assert.True(await enumerator.MoveNextAsync());
        return enumerator.Current;
    }

#if DEBUG
    private static SimulatedModel Model(string layout, RecordFormat format) =>
        SimulatedModel.IdentifiedAs(KeyboardLayout.Named(layout)!, format)!;

    private static KeyboardDevice Device(SimulatedKeyboard keyboard, DeviceLogStore log) =>
        new(keyboard, log, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(50), (_, _) => Task.CompletedTask);

    [Fact]
    public async Task AStandardKeyboardReadsBackWhatWasWritten()
    {
        var log = new DeviceLogStore();
        var keyboard = new SimulatedKeyboard(Model("6add1", RecordFormat.Standard));
        var device = Device(keyboard, log);
        Assert.Equal(new KeyboardIdentity(6, 1, 10), await device.AskIdentityAsync(Cancellation));

        var layout = KeyboardLayout.Named("6add1")!;
        var fresh = await device.ReadActionsAsync(0, RecordFormat.Standard, layout, Cancellation);
        Assert.Equal(layout.Controls.Count, fresh.Actions.Count);
        Assert.Empty(fresh.Unread);
        Assert.All(fresh.Actions.Values, action => Assert.Equal(KeyAction.Unassigned, action));

        var configuration = new KeyboardConfiguration(3);
        var presses = new KeyAction.Keys(new KeyMacro(
        [
            new KeyStroke(Modifiers.Super, 0x06).WithDelay(100),
            KeyStroke.OfKey(0x28).WithDelay(60),
        ]));
        configuration.Set(presses, ControlId.Key(3), 1);
        configuration.Set(new KeyAction.Mouse(MouseAction.MiddleClick), ControlId.Knob(1, KnobPart.Press), 1);
        EquatableArray<KeyColor> colors = [.. Enumerable.Range(0, 16).Select(red => new KeyColor((byte)red, 0, 0))];
        configuration.SetLighting(new LayerLighting.PerKey(5, colors), 1);
        await device.WriteAsync(configuration, new CommandSet.Records(RecordFormat.Standard), layout, Cancellation);

        var read = await device.ReadActionsAsync(1, RecordFormat.Standard, layout, Cancellation);
        Assert.Equal(presses, read.Actions[ControlId.Key(3)]);
        Assert.Equal(new KeyAction.Mouse(MouseAction.MiddleClick), read.Actions[ControlId.Knob(1, KnobPart.Press)]);
        Assert.Equal(KeyAction.Unassigned, read.Actions[ControlId.Key(1)]);
        Assert.Equal(new LayerLighting.PerKey(5, colors), await device.ReadLightingAsync(1, Cancellation));
        Assert.Equal(
            new LayerLighting.PerKey(0, KeyColor.DefaultKeyColors), await device.ReadLightingAsync(0, Cancellation));
    }

    [Fact]
    public async Task ALegacyKeyboardKeepsItsWait()
    {
        var log = new DeviceLogStore();
        var keyboard = new SimulatedKeyboard(Model("4add1", RecordFormat.Legacy));
        var device = Device(keyboard, log);
        Assert.Equal(new KeyboardIdentity(4, 1, 0), await device.AskIdentityAsync(Cancellation));
        var layout = KeyboardLayout.Named("4add1")!;
        var configuration = new KeyboardConfiguration(3);
        var presses = new KeyAction.Keys(new KeyMacro([new KeyStroke(Modifiers.Shift, 0x04)], 1500));
        configuration.Set(presses, ControlId.Key(4), 2);
        configuration.SetLighting(new LayerLighting.ColorMode(LightColor.Purple, 5), 2);
        await device.WriteAsync(configuration, new CommandSet.Records(RecordFormat.Legacy), layout, Cancellation);
        var read = await device.ReadActionsAsync(2, RecordFormat.Legacy, layout, Cancellation);
        Assert.Equal(presses, read.Actions[ControlId.Key(4)]);
        Assert.Equal(0x75, keyboard.Record(3, 0)![11]);
    }

    [Fact]
    public async Task AnUnidentifiedKeyboardAnswersNothing()
    {
        var log = new DeviceLogStore();
        var model = new SimulatedModel(new UsbIdentity(0x1189, 0x8840), new SimulatedKind.Unidentified());
        var keyboard = new SimulatedKeyboard(model);
        var device = Device(keyboard, log);
        Assert.Null(await device.AskIdentityAsync(Cancellation));
        var configuration = new KeyboardConfiguration(3);
        configuration.Set(new KeyAction.Media(MediaKey.Mute), ControlId.Key(1), 0);
        var layout = KeyboardLayout.Named("6add1")!;
        await device.WriteAsync(configuration, new CommandSet.Records(RecordFormat.Unidentified), layout, Cancellation);
        Assert.Equal(0xFE, keyboard.Record(1, 1)![0]);
    }

    /// <summary>
    /// Saved to a simulated keyboard that reads back, what was written reads back as written — standard, lights and
    /// all, and legacy.
    /// </summary>
    [Theory]
    [InlineData("12add2", RecordFormat.Standard)]
    [InlineData("4add1", RecordFormat.Legacy)]
    public async Task ASaveToASimulatedKeyboardReadsBack(string layoutId, RecordFormat format)
    {
        var keyboard = new SimulatedKeyboard(Model(layoutId, format));
        var device = Device(keyboard, new DeviceLogStore());
        var layout = KeyboardLayout.Named(layoutId)!;
        var capabilities = new KeyboardCapabilities(new CommandSet.Records(format));
        var configuration = new KeyboardConfiguration(3);
        configuration.Set(
            new KeyAction.Keys(new KeyMacro([new KeyStroke(Modifiers.Control, 0x06), KeyStroke.OfKey(0x19)])),
            ControlId.Key(1), 0);
        configuration.Set(new KeyAction.Media(MediaKey.VolumeUp), ControlId.Knob(1, KnobPart.RotateRight), 0);
        configuration.Set(new KeyAction.Mouse(MouseAction.ShiftWheelDown), ControlId.Key(2), 2);
        configuration.Set(new KeyAction.Keys(new KeyMacro([new KeyStroke(Modifiers.Alt, 0)])), ControlId.Key(3), 2);
        configuration.SetLighting(
            format == RecordFormat.Standard
                ? new LayerLighting.PerKey(2, KeyColor.DefaultKeyColors.SetItem(0, new KeyColor(1, 2, 3)))
                : new LayerLighting.ColorMode(LightColor.Blue, 3),
            1);

        var outcome = await new SaveKeyboardUseCase(device).ExecuteAsync(
            configuration, capabilities, layout, Cancellation);
        var saved = Assert.IsType<SaveOutcome.Saved>(outcome);
        Assert.Equal(SaveCheck.Matched, saved.Check);
        Assert.False(saved.Configuration.HasUnsavedChanges);
    }

    /// <summary>
    /// The legacy read asks for 24 records, so a 16-key, 3-knob keyboard's last knob turn, in record 25, is outside it —
    /// neither read nor unread — and a save that changes only it isn't compared.
    /// </summary>
    [Fact]
    public async Task ARecordPastTheReadIsOutsideIt()
    {
        var keyboard = new SimulatedKeyboard(Model("16add3", RecordFormat.Legacy));
        var device = Device(keyboard, new DeviceLogStore());
        var layout = KeyboardLayout.Named("16add3")!;
        var last = ControlId.Knob(3, KnobPart.RotateRight);
        var read = await device.ReadActionsAsync(0, RecordFormat.Legacy, layout, Cancellation);
        Assert.Equal([last], read.Outside);
        Assert.Empty(read.Unread);
        Assert.False(read.Actions.ContainsKey(last));
        Assert.Equal(layout.Controls.Count - 1, read.Actions.Count);

        var configuration = new KeyboardConfiguration(3);
        configuration.Set(new KeyAction.Media(MediaKey.Mute), last, 0);
        var saved = Assert.IsType<SaveOutcome.Saved>(await new SaveKeyboardUseCase(device).ExecuteAsync(
            configuration, new KeyboardCapabilities(new CommandSet.Records(RecordFormat.Legacy)), layout, Cancellation));
        Assert.Equal(SaveCheck.Matched, saved.Check);
        Assert.False(saved.Configuration.HasUnsavedChanges);
        Assert.Equal(25, keyboard.Record(1, 25)![1]);
    }

    /// <summary>The 8890's packets go out after its report ID, padded to its report's length.</summary>
    [Fact]
    public async Task The8890()
    {
        var log = new DeviceLogStore();
        var model = new SimulatedModel(KeyboardFamilies.K8890Usb, new SimulatedKind.K8890(3));
        var keyboard = new SimulatedKeyboard(model);
        var device = Device(keyboard, log);
        var configuration = new KeyboardConfiguration(3);
        configuration.Set(new KeyAction.Keys(new KeyMacro([KeyStroke.OfKey(0x04)])), ControlId.Key(1), 0);
        await device.WriteAsync(
            configuration, new CommandSet.Packets(3), KeyboardLayout.Named("6add1")!, Cancellation);
        var written = keyboard.Written();
        Assert.Equal(4, written.Count);
        Assert.All(written, payload => Assert.Equal(64, payload.Length));
        Assert.Equal(new byte[] { 1, 0x11, 1, 1, 0, 0x04, 0, 0 }, written[2][..8]);

        var entries = await First(log.Entries(Cancellation));
        var reports = entries.Select(entry => entry.Content).OfType<LogContent.Output>().ToList();
        Assert.Equal(4, reports.Count);
        var first = new byte[65];
        first[0] = 0x03;
        first[1] = 0xA1;
        first[2] = 0x01;
        Assert.Equal(first, reports[0].Bytes.ToArray());
    }
#endif

    private sealed class Nothing : IChannelProvider
    {
        public IReportChannel? OpenChannel() => null;
    }

    /// <summary>A keyboard that answers every request with the same records, then nothing.</summary>
    private sealed class Answering(params byte[][] records) : IReportChannel, IChannelProvider
    {
        private readonly Queue<byte[]> replies = new();

        public ReportFraming Framing { get; } = new(0x03, 65);

        public IReportChannel? OpenChannel() => this;

        public Task WriteAsync(byte[] report, CancellationToken cancellation = default)
        {
            foreach (var record in records)
                replies.Enqueue(Framing.Frame(record));
            return Task.CompletedTask;
        }

        public void DiscardPendingReplies() => replies.Clear();

        public Task<byte[]?> NextReplyAsync(TimeSpan timeout, CancellationToken cancellation = default) =>
            Task.FromResult(replies.TryDequeue(out var reply) ? reply : null);
    }

    private static readonly KeyboardLayout SixAndOne = KeyboardLayout.Named("6add1")!;

    private static Task<LayerRead> ReadAsync(Answering keyboard, RecordFormat format = RecordFormat.Standard) =>
        new KeyboardDevice(keyboard, new DeviceLogStore()).ReadActionsAsync(0, format, SixAndOne, Cancellation);

    private static byte[] Muted(byte recordId) =>
        Records.Encode(new KeyAction.Media(MediaKey.Mute), recordId, 0, RecordFormat.Standard);

    /// <summary>
    /// Answers that stop after the first keep what came; the layout's controls in the records that didn't come are
    /// unread. A keyboard with no more controls than records that came has none unread.
    /// </summary>
    [Fact]
    public async Task AReadThatStopsEarlyKeepsWhatCame()
    {
        var read = await ReadAsync(new Answering(Muted(1), Muted(2), Muted(3)));
        Assert.Equal([ControlId.Key(1), ControlId.Key(2), ControlId.Key(3)], read.Actions.Keys.Order());
        Assert.All(read.Actions.Values, action => Assert.Equal(new KeyAction.Media(MediaKey.Mute), action));
        Assert.Equal(
            [
                ControlId.Key(4), ControlId.Key(5), ControlId.Key(6), ControlId.Knob(1, KnobPart.RotateLeft),
                ControlId.Knob(1, KnobPart.Press), ControlId.Knob(1, KnobPart.RotateRight),
            ],
            read.Unread);

        var all = await ReadAsync(new Answering([.. Enumerable.Range(1, 18).Select(id => Muted((byte)id))]));
        Assert.Equal(SixAndOne.Controls.Count, all.Actions.Count);
        Assert.Empty(all.Unread);
        Assert.Empty(all.Outside);
    }

    [Fact]
    public async Task NoAnswerIsNoReply()
    {
        var exception = await Assert.ThrowsAsync<KeyboardDeviceException>(() => ReadAsync(new Answering()));
        Assert.IsType<KeyboardDeviceError.NoReply>(exception.Error);
    }

    /// <summary>A record of a kind the app doesn't know leaves its control unread.</summary>
    [Fact]
    public async Task ARecordOfAnUnknownKindIsUnread()
    {
        var unknown = Muted(2);
        unknown[3] = 0x07;
        var records = Enumerable.Range(1, 25).Select(id => id == 2 ? unknown : Muted((byte)id));
        var read = await ReadAsync(new Answering([.. records]));
        Assert.Equal([ControlId.Key(2)], read.Unread);
        Assert.False(read.Actions.ContainsKey(ControlId.Key(2)));
        Assert.Equal(SixAndOne.Controls.Count - 1, read.Actions.Count);
    }

    /// <summary>Well-formed actions of every kind come back from each format's records as they went in.</summary>
    [Theory]
    [InlineData(RecordFormat.Standard)]
    [InlineData(RecordFormat.Legacy)]
    public void TheStoredFormOfWhatFits(RecordFormat format)
    {
        var device = new KeyboardDevice(new Nothing(), new DeviceLogStore());
        var standard = format == RecordFormat.Standard;
        KeyAction[] actions =
        [
            KeyAction.Unassigned,
            new KeyAction.Keys(new KeyMacro(
            [
                new KeyStroke(Modifiers.Control | Modifiers.RightShift, 0x06).WithDelay(standard ? 250u : 0),
                KeyStroke.OfKey(0x28).WithDelay(standard ? 60000u : 0),
            ], standard ? 0u : 1500)),
            new KeyAction.Keys(new KeyMacro([new KeyStroke(Modifiers.Super | Modifiers.Alt, 0)])),
            new KeyAction.Media(MediaKey.Calculator),
            new KeyAction.Mouse(MouseAction.RightClick),
            new KeyAction.Mouse(MouseAction.ControlWheelUp),
        ];
        Assert.All(actions, action => Assert.Equal(action, device.StoredForm(action, format)));
        var tooMany = new KeyAction.Keys(new KeyMacro([.. Enumerable.Repeat(KeyStroke.OfKey(0x04), 21)]));
        Assert.Null(device.StoredForm(tooMany, format));
    }

    [Fact]
    public async Task NothingOpen()
    {
        var device = new KeyboardDevice(new Nothing(), new DeviceLogStore());
        var exception = await Assert.ThrowsAsync<KeyboardDeviceException>(() => device.AskIdentityAsync(Cancellation));
        Assert.IsType<KeyboardDeviceError.NotConnected>(exception.Error);
    }

    [Fact]
    public async Task TheLogKeepsTheNewestLines()
    {
        var log = new DeviceLogStore();
        for (uint index = 0; index < 2100; index++)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, index);
            log.Append(new LogContent.Output([.. bytes]));
        }
        var lines = log.Snapshot();
        Assert.Equal(2000, lines.Count);
        Assert.Equal(101UL, lines[0].Id);
        log.Clear();
        Assert.Empty(await First(log.Entries(Cancellation)));
    }
}
