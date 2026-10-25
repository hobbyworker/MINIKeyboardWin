using System.Runtime.CompilerServices;
using MINIKeyboard.Domain;
using MINIKeyboard.Domain.UseCases;
using Channels = System.Threading.Channels;

namespace MINIKeyboard.Presentation.Tests;

public class DeviceLogTests
{
    private sealed class FakeLog : IDeviceLogRepository
    {
        public Channels.Channel<IReadOnlyList<DeviceLogEntry>> Updates { get; } =
            Channels.Channel.CreateUnbounded<IReadOnlyList<DeviceLogEntry>>();

        public int Cleared { get; private set; }

        public async IAsyncEnumerable<IReadOnlyList<DeviceLogEntry>> Entries(
            [EnumeratorCancellation] CancellationToken cancellation = default)
        {
            while (true)
                yield return await Updates.Reader.ReadAsync(cancellation);
        }

        public void Clear() => Cleared++;
    }

    /// <summary>The window's lines follow the log, each kind of entry as the window shows it, until it stops.</summary>
    [Fact]
    public async Task TheLinesFollowTheLog()
    {
        var log = new FakeLog();
        var model = new DeviceLogModel(new DeviceLogUseCase(log));
        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        model.Changed += (_, _) => changed.TrySetResult();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var run = model.RunAsync(stop.Token);

        var time = DateTimeOffset.UnixEpoch;
        byte[] report = [0x03, 0xFB, 0xFB, 0xFB, 0, 0];
        log.Updates.Writer.TryWrite(
        [
            new DeviceLogEntry(1, time, new LogContent.Event(new DeviceLogEvent.Connected(KeyboardFamilies.K8890Usb, 1))),
            new DeviceLogEntry(2, time, new LogContent.Output([.. report])),
            new DeviceLogEntry(3, time, new LogContent.Input([0x01])),
            new DeviceLogEntry(4, time, new LogContent.Event(new DeviceLogEvent.Simulated("8890-3"))),
        ]);
        await changed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        var lines = model.Lines;
        Assert.Equal([1UL, 2UL, 3UL, 4UL], lines.Select(line => line.Id));
        Assert.Equal(
            [LogDirection.Event, LogDirection.Output, LogDirection.Input, LogDirection.Event],
            lines.Select(line => line.Direction));
        Assert.Equal(new LogEvent.Connected("1189:8890", 1), lines[0].Event);
        Assert.Empty(lines[0].Bytes);
        Assert.Equal("03 FB FB FB", lines[1].Summary());
        Assert.True(lines[1].IsShortened);
        Assert.Equal("03 FB FB FB 00 00", lines[1].Hex());
        Assert.Equal(["0000  03 FB FB FB 00 00"], lines[1].Rows());
        Assert.False(lines[2].IsShortened);
        Assert.Equal(new LogEvent.Simulated("8890-3"), lines[3].Event);

        model.Clear();
        Assert.Equal(1, log.Cleared);
        await stop.CancelAsync();
        await run;
    }
}
