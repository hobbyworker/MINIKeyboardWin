using System.Diagnostics.CodeAnalysis;
using MINIKeyboard.Data.Connection;
using MINIKeyboard.Domain;

namespace MINIKeyboard.Data.Tests;

/// <summary>
/// The Windows connection's decisions, from what the HID API would list: which collection takes the configuration, how
/// its reports are framed, and what the watcher does as keyboards come and go.
/// </summary>
public class ConnectionTests
{
    private const string InterfaceGuid = "{4d1e55b2-f16f-11cf-88cb-001111000030}";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static HidCollection Collection(
        UsbIdentity usb, string device, string function, ushort page, ushort usage, ushort input = 0,
        ushort output = 0, byte? reportId = null)
    {
        var path = $@"\\?\hid#vid_{usb.VendorId:x4}&pid_{usb.ProductId:x4}&{function}#{device}&0000#{InterfaceGuid}";
        var number = HidCollections.InterfaceNumber(path) ?? 0;
        return new HidCollection(
            path, $@"USB\VID_{usb.VendorId:X4}&PID_{usb.ProductId:X4}\{device}", usb, 0x0100, number, page, usage,
            input, output, reportId);
    }

    /// <summary>
    /// A 1189:8890 as Windows makes devices of it: a keyboard on interface 0 (its LEDs an output report), the vendor
    /// collection on interface 1, a keyboard and a consumer collection on interface 2, a mouse on interface 3.
    /// </summary>
    private static List<HidCollection> K8890(string device = "6&8890") =>
    [
        Collection(KeyboardFamilies.K8890Usb, device, "mi_00", 0x01, 0x06, input: 9, output: 2, reportId: 0),
        Collection(KeyboardFamilies.K8890Usb, device, "mi_01", 0xFF00, 0x01, input: 65, output: 65, reportId: 3),
        Collection(KeyboardFamilies.K8890Usb, device, "mi_02&col01", 0x01, 0x06, input: 9),
        Collection(KeyboardFamilies.K8890Usb, device, "mi_02&col02", 0x0C, 0x01, input: 3),
        Collection(KeyboardFamilies.K8890Usb, device, "mi_03", 0x01, 0x02, input: 5),
    ];

    /// <summary>A 884x keyboard whose interface 0 has a keyboard collection and a vendor one for report ID 3.</summary>
    private static List<HidCollection> K8850(string device = "6&8850") =>
    [
        Collection(new(0x514C, 0x8850), device, "mi_00&col01", 0x01, 0x06, input: 9, output: 2, reportId: 0),
        Collection(new(0x514C, 0x8850), device, "mi_00&col02", 0xFF60, 0x61, input: 65, output: 65, reportId: 3),
        Collection(new(0x514C, 0x8850), device, "mi_01", 0x0C, 0x01, input: 3),
    ];

    [Fact]
    public void InterfaceNumbersComeFromThePath()
    {
        var path = $@"\\?\hid#vid_1189&pid_8890&mi_01#7&2f3b5c4a&0&0000#{InterfaceGuid}";
        Assert.Equal((byte)1, HidCollections.InterfaceNumber(path));
        Assert.Equal((byte)0, HidCollections.InterfaceNumber(@"\\?\HID#VID_514C&PID_8850&MI_00&COL02#8&1d&0&1#{x}"));
        Assert.Equal((byte)0x0A, HidCollections.InterfaceNumber(@"\\?\hid#vid_1189&pid_8840&mi_0a#7&1&0&0000#{x}"));
        Assert.Null(HidCollections.InterfaceNumber(@"\\?\hid#vid_046d&pid_c52b#6&2b3c&0&0000#{x}"));
        Assert.Null(HidCollections.InterfaceNumber(@"\\?\hid#vid_1189&pid_8890&mi_zz#7&1&0&0000#{x}"));
        Assert.Null(HidCollections.InterfaceNumber("&mi_1"));
    }

    /// <summary>
    /// The 8890's configuration is written to interface 1, a 884x keyboard's to interface 0 — to the collection with an
    /// output report, a vendor-defined one first.
    /// </summary>
    [Fact]
    public void TheConfigurationCollection()
    {
        var k8890 = HidCollections.ConfigurationCollection(K8890(), KeyboardFamily.K8890)!;
        Assert.Equal((byte)1, k8890.InterfaceNumber);
        var k8850 = HidCollections.ConfigurationCollection(K8850(), KeyboardFamily.K884x)!;
        Assert.Equal(0xFF60, k8850.UsagePage);
        // Without a vendor collection, any with an output report; without one, none.
        var plain = K8850().Where(collection => collection.UsagePage < 0xFF00).ToList();
        Assert.Equal(0x01, HidCollections.ConfigurationCollection(plain, KeyboardFamily.K884x)!.UsagePage);
        Assert.Null(HidCollections.ConfigurationCollection(
            K8890().Where(collection => collection.InterfaceNumber != 1), KeyboardFamily.K8890));
    }

    /// <summary>
    /// The report ID is the output caps', and the length the longest output report's — less the byte Windows counts
    /// for a report ID when the reports aren't numbered.
    /// </summary>
    [Fact]
    public void FramingFromTheOutputCaps()
    {
        var numbered = Collection(KeyboardFamilies.K8890Usb, "d", "mi_01", 0xFF00, 1, output: 65, reportId: 3);
        Assert.Equal(new ReportFraming(3, 65), HidCollections.Framing(numbered));
        Assert.Equal(new ReportFraming(0, 64), HidCollections.Framing(numbered with { OutputReportId = 0 }));
        var short9 = numbered with { OutputReportId = 2, OutputReportByteLength = 9 };
        Assert.Equal(new ReportFraming(2, 9), HidCollections.Framing(short9));
        Assert.Equal(new ReportFraming(3, 65), HidCollections.Framing(numbered with { OutputReportId = null }));
    }

    /// <summary>Windows takes the report ID first even when it is 0, and every report as long as the longest.</summary>
    [Fact]
    public void WriteFileBuffers()
    {
        var numbered = new ReportFraming(3, 9);
        Assert.Equal(
            new byte[] { 3, 0xAA, 0xAA, 0, 0, 0, 0, 0, 0 },
            HidCollections.OutputBuffer(numbered.Frame([0xAA, 0xAA]), numbered, 9));
        var unnumbered = new ReportFraming(0, 8);
        Assert.Equal(
            new byte[] { 0, 0xAA, 0, 0, 0, 0, 0, 0, 0 },
            HidCollections.OutputBuffer(unnumbered.Frame([0xAA]), unnumbered, 9));
        Assert.Equal(new byte[] { 3, 1, 2, 0 }, HidCollections.OutputBuffer([3, 1, 2], new ReportFraming(3, 3), 4));
    }

    /// <summary>The 1189:8890 before the 884x keyboards, as the vendor apps look; one candidate per keyboard.</summary>
    [Fact]
    public void KeyboardsInTheOrderTheVendorAppsLook()
    {
        var other = Collection(new(0x046D, 0xC52B), "6&mouse", "mi_00", 0xFF00, 1, output: 20, reportId: 0x10);
        var candidates = HidCollections.Candidates([.. K8850(), other, .. K8890()]);
        Assert.Equal(2, candidates.Count);
        Assert.Equal(KeyboardFamily.K8890, candidates[0].Family);
        Assert.Equal(1, candidates[0].Collection.InterfaceNumber);
        Assert.Equal(5, candidates[0].Device.Count);
        Assert.Equal(KeyboardFamily.K884x, candidates[1].Family);
        Assert.Equal(0xFF60, candidates[1].Collection.UsagePage);
        // Two of the same keyboard are two candidates.
        Assert.Equal(2, HidCollections.Candidates([.. K8890("6&a"), .. K8890("6&b")]).Count);
    }

    /// <summary>The 8890 answers nothing; a 884x keyboard is read.</summary>
    [Fact]
    public void Answers()
    {
        var candidates = HidCollections.Candidates([.. K8890(), .. K8850()]);
        Assert.False(HidCollections.ReadsAnswers(candidates[0]));
        Assert.True(HidCollections.ReadsAnswers(candidates[1]));
    }

    /// <summary>
    /// What the HID API tells, and nothing it doesn't: an interface per mi_XX with its collections' usages.
    /// </summary>
    [Fact]
    public void Details()
    {
        var candidate = HidCollections.Candidates(K8890())[0];
        var details = HidCollections.Details(candidate, new DeviceStrings(null, "Keyboard", null));
        Assert.Equal(
            new DeviceDetails(
                UsbVersion: null,
                DeviceRelease: 0x0100,
                Speed: null,
                Manufacturer: null,
                ProductName: "Keyboard",
                SerialNumber: null,
                MaxPower: null,
                [
                    new DeviceInterface(0, 3, null, null, [new UsagePair(0x01, 0x06)]),
                    new DeviceInterface(1, 3, null, null, [new UsagePair(0xFF00, 0x01)]),
                    new DeviceInterface(2, 3, null, null, [new UsagePair(0x01, 0x06), new UsagePair(0x0C, 0x01)]),
                    new DeviceInterface(3, 3, null, null, [new UsagePair(0x01, 0x02)]),
                ]),
            details);
    }

    // The watcher, against a fake HID system.

    private sealed class FakePort : IHidPort
    {
        public List<byte[]> Written { get; } = [];

        public Queue<byte[]> Answers { get; } = new();

        public int WriteError { get; set; }

        public int Discards { get; private set; }

        public int Write(byte[] report)
        {
            Written.Add(report);
            return WriteError;
        }

        public byte[]? Read(TimeSpan timeout) => Answers.TryDequeue(out var answer) ? answer : null;

        public void Discard() => Discards++;

        public DeviceStrings Strings() => new(null, null, null);

        public void Dispose()
        {
        }
    }

    private sealed class FakeHid : IHidSystem
    {
        public List<HidCollection> Plugged { get; } = [];

        public List<(HidCollection Collection, bool ReadsAnswers)> Opened { get; } = [];

        public int? OpenError { get; set; }

        public IReadOnlyList<HidCollection> Collections() => [.. Plugged];

        public bool TryOpen(
            HidCollection collection, ReportFraming framing, bool readsAnswers,
            [NotNullWhen(true)] out IHidPort? port, out int error)
        {
            Opened.Add((collection, readsAnswers));
            if (OpenError is { } code)
            {
                port = null;
                error = code;
                return false;
            }
            port = new FakePort();
            error = 0;
            return true;
        }
    }

    private static (ConnectionWatcher Watcher, Broadcaster<ConnectionState> State, DeviceLogStore Log) Watcher(
        FakeHid hid)
    {
        var (watcher, state, _, log) = Watching(hid, null);
        return (watcher, state, log);
    }

    private static (ConnectionWatcher Watcher, Broadcaster<ConnectionState> State,
        Broadcaster<AttachedKeyboards> Attached, DeviceLogStore Log) Watching(FakeHid hid, string? selectedKeyboard)
    {
        var state = new Broadcaster<ConnectionState>(ConnectionState.Searching);
        var attached = new Broadcaster<AttachedKeyboards>(AttachedKeyboards.None);
        var log = new DeviceLogStore();
        return (new ConnectionWatcher(log, hid, state, attached, selectedKeyboard), state, attached, log);
    }

    private static string DeviceKey(UsbIdentity usb, string device) =>
        $@"USB\VID_{usb.VendorId:X4}&PID_{usb.ProductId:X4}\{device}";

    // Several keyboards plugged in.

    /// <summary>One entry per keyboard, in the order their places to write came; the same USB ID by port.</summary>
    [Fact]
    public void KeyboardsInOrderOfFamilyThenPort()
    {
        AttachedKeyboard Keyboard(string id, UsbIdentity usb, string location) => new(id, usb, location);
        var a = Keyboard("a", KeyboardFamilies.K8890Usb, "2");
        var b = Keyboard("b", KeyboardFamilies.K8890Usb, "1");
        var c = Keyboard("c", new(0x1189, 0x8840), "0");
        Assert.Equal([b, a, c], KeyboardChoice.Ordered([a, b, c, c]));
    }

    /// <summary>
    /// The one chosen this session; else the one chosen last time, by its USB ID and port; else the first that opens —
    /// and with none, the first, to show why it can't.
    /// </summary>
    [Fact]
    public void WhichKeyboardOpens()
    {
        AttachedKeyboard[] keyboards =
        [
            new("a", KeyboardFamilies.K8890Usb, "1"),
            new("b", new(0x1189, 0x8840), "2"),
        ];
        Assert.Equal(keyboards[1], KeyboardChoice.Target(keyboards, "b", "1189:8890@1", _ => true));
        Assert.Equal("1189:8840@2", KeyboardChoice.Key(keyboards[1]));
        Assert.Equal(keyboards[1], KeyboardChoice.Target(keyboards, null, "1189:8840@2", _ => true));
        Assert.Equal(keyboards[1], KeyboardChoice.Target(keyboards, "x", "1189:8890@9", keyboard => keyboard.Id == "b"));
        Assert.Equal(keyboards[0], KeyboardChoice.Target(keyboards, null, null, _ => false));
        Assert.Null(KeyboardChoice.Target([], null, null, _ => true));
    }

    /// <summary>
    /// With several plugged in, one is open — the first — and choosing another opens it and keeps it for next time.
    /// Unplugged, the first opens again; plugged in again, the app stays where it is.
    /// </summary>
    [Fact]
    public void OneOfSeveralKeyboardsIsOpen()
    {
        var remembered = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"minikb-{Guid.NewGuid():N}", "selected");
        var hid = new FakeHid();
        hid.Plugged.AddRange([.. K8850(), .. K8890("6&b"), .. K8890("6&a")]);
        var (watcher, state, attached, _) = Watching(hid, remembered);
        watcher.Refresh();
        var a = DeviceKey(KeyboardFamilies.K8890Usb, "6&a");
        var b = DeviceKey(KeyboardFamilies.K8890Usb, "6&b");
        var k8850 = DeviceKey(new(0x514C, 0x8850), "6&8850");
        Assert.Equal([a, b, k8850], attached.Value.Keyboards.Select(keyboard => keyboard.Id));
        Assert.Equal(a, attached.Value.Selected);
        Assert.Contains("6&a", Assert.Single(hid.Opened).Collection.Path);

        watcher.Select(k8850);
        Assert.Equal(KeyboardFamily.K884x, Assert.IsType<ConnectionState.Connected>(state.Value).Device.Family);
        Assert.Equal(k8850, attached.Value.Selected);
        Assert.Equal($"514C:8850@{k8850}", File.ReadAllText(remembered));
        Assert.Equal(2, hid.Opened.Count);

        // The same again opens nothing.
        watcher.Select(k8850);
        Assert.Equal(2, hid.Opened.Count);

        hid.Plugged.RemoveAll(collection => collection.Usb != KeyboardFamilies.K8890Usb);
        watcher.Refresh();
        Assert.Equal(a, attached.Value.Selected);
        Assert.Contains("6&a", hid.Opened[^1].Collection.Path);
        hid.Plugged.AddRange(K8850());
        watcher.Refresh();
        Assert.Equal(a, attached.Value.Selected);
        Assert.Equal(3, hid.Opened.Count);

        // Next time, the one chosen last opens first.
        var (next, _, nextAttached, _) = Watching(hid, remembered);
        next.Refresh();
        Assert.Equal(k8850, nextAttached.Value.Selected);
        Directory.Delete(System.IO.Path.GetDirectoryName(remembered)!, recursive: true);
    }

    [Fact]
    public void ConnectsToTheConfigurationCollection()
    {
        var hid = new FakeHid();
        hid.Plugged.AddRange(K8890());
        var (watcher, state, log) = Watcher(hid);
        watcher.Refresh();

        var keyboard = Assert.IsType<ConnectionState.Connected>(state.Value).Device;
        Assert.Equal(KeyboardFamilies.K8890Usb, keyboard.Usb);
        Assert.Equal(KeyboardFamily.K8890, keyboard.Family);
        Assert.Equal(1, keyboard.InterfaceNumber);
        Assert.Equal(Channel.Hid, keyboard.Channel);
        Assert.Equal(3, keyboard.ReportId);
        Assert.Equal(65, keyboard.ReportLength);
        Assert.Equal(4, keyboard.Details!.Interfaces.Count);
        var (opened, readsAnswers) = Assert.Single(hid.Opened);
        Assert.Contains("&mi_01#", opened.Path);
        Assert.False(readsAnswers);
        Assert.NotNull(watcher.OpenChannel());
        Assert.Equal(
            new DeviceLogEvent.Connected(KeyboardFamilies.K8890Usb, 1),
            Assert.IsType<LogContent.Event>(Assert.Single(log.Snapshot()).Content).What);

        // Still plugged in: nothing is opened again.
        watcher.Refresh();
        Assert.Single(hid.Opened);
    }

    [Fact]
    public void AnUnpluggedKeyboardIsLetGo()
    {
        var hid = new FakeHid();
        hid.Plugged.AddRange(K8850());
        var (watcher, state, _) = Watcher(hid);
        watcher.Refresh();
        Assert.True(Assert.Single(hid.Opened).ReadsAnswers);
        Assert.IsType<ConnectionState.Connected>(state.Value);

        hid.Plugged.Clear();
        watcher.Refresh();
        Assert.Equal(ConnectionState.Searching, state.Value);
        Assert.Null(watcher.OpenChannel());
    }

    /// <summary>
    /// Plugged into the same port again between two looks, a keyboard has the same path but a new arrival: the watcher
    /// lets go of the handle it had, which is dead, and opens the keyboard again. Where Windows doesn't tell the
    /// arrival, the path alone is the entry.
    /// </summary>
    [Fact]
    public async Task AKeyboardPluggedInAgainIsOpenedAgain()
    {
        static List<HidCollection> Arrived(long arrival) =>
            [.. K8890().Select(collection => collection with { Arrival = arrival })];
        var plain = HidCollections.Candidates(K8890())[0];
        Assert.Equal(plain.Collection.Path, plain.Entry);
        Assert.NotEqual(HidCollections.Candidates(Arrived(1))[0].Entry, HidCollections.Candidates(Arrived(2))[0].Entry);

        var hid = new FakeHid();
        hid.Plugged.AddRange(Arrived(1));
        var (watcher, state, log) = Watcher(hid);
        watcher.Refresh();
        var first = watcher.OpenChannel()!;
        watcher.Refresh();
        Assert.Single(hid.Opened);

        hid.Plugged.Clear();
        hid.Plugged.AddRange(Arrived(2));
        watcher.Refresh();
        Assert.Equal(2, hid.Opened.Count);
        Assert.NotSame(first, watcher.OpenChannel());
        Assert.IsType<ConnectionState.Connected>(state.Value);
        Assert.Equal(
            [typeof(DeviceLogEvent.Connected), typeof(DeviceLogEvent.Searching), typeof(DeviceLogEvent.Connected)],
            log.Snapshot().Select(entry => Assert.IsType<LogContent.Event>(entry.Content).What.GetType()));
        var closed = await Assert.ThrowsAsync<KeyboardDeviceException>(() => first.WriteAsync([3], Cancellation));
        Assert.IsType<KeyboardDeviceError.NotConnected>(closed.Error);
    }

    /// <summary>
    /// A keyboard that didn't open isn't tried again — its failure shown — until it is re-plugged or retried.
    /// </summary>
    [Fact]
    public void AFailureStaysWhileTheKeyboardIsPluggedIn()
    {
        var hid = new FakeHid { OpenError = 5 };
        hid.Plugged.AddRange(K8890());
        var (watcher, state, _) = Watcher(hid);
        watcher.Refresh();
        Assert.Equal(new ConnectionState.OpenFailed(5), state.Value);
        watcher.Refresh();
        Assert.Equal(new ConnectionState.OpenFailed(5), state.Value);
        Assert.Single(hid.Opened);

        hid.OpenError = null;
        watcher.Retry();
        Assert.Equal(2, hid.Opened.Count);
        Assert.IsType<ConnectionState.Connected>(state.Value);
    }

    [Fact]
    public void AnotherKeyboardIsTriedWhileOneFails()
    {
        var hid = new FakeHid { OpenError = 31 };
        hid.Plugged.AddRange(K8890());
        var (watcher, state, _) = Watcher(hid);
        watcher.Refresh();
        Assert.Equal(new ConnectionState.OpenFailed(31), state.Value);

        hid.OpenError = null;
        hid.Plugged.AddRange(K8850());
        watcher.Refresh();
        Assert.Equal(KeyboardFamily.K884x, Assert.IsType<ConnectionState.Connected>(state.Value).Device.Family);

        // Unplugged, the failed one is forgotten: plugged in again, it is tried again.
        hid.Plugged.Clear();
        watcher.Refresh();
        Assert.Equal(ConnectionState.Searching, state.Value);
        hid.Plugged.AddRange(K8890());
        watcher.Refresh();
        Assert.Equal(KeyboardFamily.K8890, Assert.IsType<ConnectionState.Connected>(state.Value).Device.Family);
    }

    // The channel's thread.

    [Fact]
    public async Task TheChannelWritesAndReadsThroughThePort()
    {
        var port = new FakePort();
        port.Answers.Enqueue([0x03, 0xFB, 6, 1, 10]);
        var channel = IoChannel.Start(port, new ReportFraming(3, 65));
        await channel.WriteAsync([3, 0xFB, 0xFB, 0xFB], Cancellation);
        channel.DiscardPendingReplies();
        Assert.Equal(new byte[] { 0x03, 0xFB, 6, 1, 10 }, await channel.NextReplyAsync(TimeSpan.Zero, Cancellation));
        Assert.Null(await channel.NextReplyAsync(TimeSpan.Zero, Cancellation));
        Assert.Equal(new byte[] { 3, 0xFB, 0xFB, 0xFB }, Assert.Single(port.Written));
        Assert.Equal(1, port.Discards);

        port.WriteError = 1167;
        var failed = await Assert.ThrowsAsync<KeyboardDeviceException>(() => channel.WriteAsync([3], Cancellation));
        Assert.Equal(new KeyboardDeviceError.WriteFailed(1167), failed.Error);

        channel.Close();
        var closed = await Assert.ThrowsAsync<KeyboardDeviceException>(() => channel.WriteAsync([3], Cancellation));
        Assert.IsType<KeyboardDeviceError.NotConnected>(closed.Error);
        Assert.Null(await channel.NextReplyAsync(TimeSpan.Zero, Cancellation));
    }
}
