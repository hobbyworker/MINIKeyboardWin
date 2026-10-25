using System.Collections.Concurrent;
using MINIKeyboard.Domain;

namespace MINIKeyboard.Data.Connection;

/// <summary>
/// An open configuration collection on a thread of its own: writes and reads block there, one after another in the
/// order they were asked for, and the caller awaits tasks.
/// </summary>
internal sealed class IoChannel : IReportChannel
{
    private readonly Lock gate = new();
    private readonly BlockingCollection<Action<IHidPort>> jobs = [];
    private bool closed;

    private IoChannel(ReportFraming framing) => Framing = framing;

    public ReportFraming Framing { get; }

    /// <summary>
    /// Starts the thread that owns <paramref name="port"/>; it closes the port when the channel closes.
    /// </summary>
    public static IoChannel Start(IHidPort port, ReportFraming framing)
    {
        var channel = new IoChannel(framing);
        new Thread(() => channel.Run(port)) { IsBackground = true, Name = "keyboard-io" }.Start();
        return channel;
    }

    /// <summary>No more jobs: what is queued still runs, then the port closes.</summary>
    public void Close()
    {
        lock (gate)
        {
            if (closed)
                return;
            closed = true;
            jobs.CompleteAdding();
        }
    }

    public async Task WriteAsync(byte[] report, CancellationToken cancellation = default)
    {
        var done = Submit(port => port.Write(report))
            ?? throw new KeyboardDeviceException(new KeyboardDeviceError.NotConnected());
        var code = await done.WaitAsync(cancellation);
        if (code != 0)
            throw new KeyboardDeviceException(new KeyboardDeviceError.WriteFailed(code));
    }

    public void DiscardPendingReplies() => Submit(port =>
    {
        port.Discard();
        return true;
    });

    public async Task<byte[]?> NextReplyAsync(TimeSpan timeout, CancellationToken cancellation = default)
    {
        if (Submit(port => port.Read(timeout)) is not { } done)
            return null;
        return await done.WaitAsync(cancellation);
    }

    /// <summary>Queues <paramref name="work"/> for the port's thread; null once the channel is closed.</summary>
    private Task<T>? Submit<T>(Func<IHidPort, T> work)
    {
        // Continuations run off the port's thread, so the next job never waits on the caller.
        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (gate)
        {
            if (closed)
                return null;
            jobs.Add(port =>
            {
                try
                {
                    done.SetResult(work(port));
                }
                catch (Exception exception)
                {
                    done.SetException(exception);
                }
            });
        }
        return done.Task;
    }

    private void Run(IHidPort port)
    {
        using (port)
        {
            foreach (var job in jobs.GetConsumingEnumerable())
                job(port);
        }
        jobs.Dispose();
    }
}
