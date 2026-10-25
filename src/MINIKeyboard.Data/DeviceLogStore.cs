using System.Runtime.CompilerServices;
using System.Threading.Channels;
using MINIKeyboard.Domain;

namespace MINIKeyboard.Data;

/// <summary>
/// The device log, kept in memory: what the connection and the device write, the log window reads. Lines can be added
/// from any thread.
/// </summary>
public sealed class DeviceLogStore : IDeviceLogRepository
{
    private const int Limit = 2000;

    private readonly Lock gate = new();
    private readonly Queue<DeviceLogEntry> items = new();
    private readonly List<ChannelWriter<IReadOnlyList<DeviceLogEntry>>> subscribers = [];
    private ulong nextId;

    public void Append(LogContent content)
    {
        lock (gate)
        {
            nextId++;
            items.Enqueue(new DeviceLogEntry(nextId, DateTimeOffset.Now, content));
            while (items.Count > Limit)
                items.Dequeue();
            Publish();
        }
    }

    /// <summary>Every line, oldest first.</summary>
    public IReadOnlyList<DeviceLogEntry> Snapshot()
    {
        lock (gate)
            return [.. items];
    }

    /// <summary>
    /// The current lines, then the whole list after changes. One that falls behind skips to the newest.
    /// </summary>
    public async IAsyncEnumerable<IReadOnlyList<DeviceLogEntry>> Entries(
        [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        var updates = System.Threading.Channels.Channel.CreateBounded<IReadOnlyList<DeviceLogEntry>>(
            new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });
        lock (gate)
        {
            updates.Writer.TryWrite([.. items]);
            subscribers.Add(updates.Writer);
        }
        try
        {
            await foreach (var entries in updates.Reader.ReadAllAsync(cancellation))
                yield return entries;
        }
        finally
        {
            lock (gate)
                subscribers.Remove(updates.Writer);
        }
    }

    public void Clear()
    {
        lock (gate)
        {
            items.Clear();
            Publish();
        }
    }

    /// <summary>Each subscriber holds only the newest list.</summary>
    private void Publish()
    {
        IReadOnlyList<DeviceLogEntry> list = [.. items];
        foreach (var subscriber in subscribers)
            subscriber.TryWrite(list);
    }
}
