using System.Buffers.Binary;
using MINIKeyboard.Domain;

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
        Assert.Equal(layout.Controls.Count, fresh.Count);
        Assert.All(fresh.Values, action => Assert.Equal(KeyAction.Unassigned, action));

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
        Assert.Equal(presses, read[ControlId.Key(3)]);
        Assert.Equal(new KeyAction.Mouse(MouseAction.MiddleClick), read[ControlId.Knob(1, KnobPart.Press)]);
        Assert.Equal(KeyAction.Unassigned, read[ControlId.Key(1)]);
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
        Assert.Equal(presses, read[ControlId.Key(4)]);
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
