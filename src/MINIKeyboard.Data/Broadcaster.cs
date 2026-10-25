using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace MINIKeyboard.Data;

/// <summary>
/// Keeps every subscriber of a value up to date, from any thread: the current value first, then each change.
/// </summary>
internal sealed class Broadcaster<T>(T initial)
{
    private readonly Lock gate = new();
    private readonly List<ChannelWriter<T>> subscribers = [];
    private T current = initial;

    public T Value
    {
        get
        {
            lock (gate)
                return current;
        }
    }

    /// <summary>Sends a changed value to every subscriber; the same value again sends nothing.</summary>
    public void Set(T value)
    {
        lock (gate)
        {
            if (EqualityComparer<T>.Default.Equals(current, value))
                return;
            current = value;
            foreach (var subscriber in subscribers)
                subscriber.TryWrite(value);
        }
    }

    /// <summary>The current value, then each change, until the caller stops.</summary>
    public async IAsyncEnumerable<T> Subscribe([EnumeratorCancellation] CancellationToken cancellation = default)
    {
        var updates = System.Threading.Channels.Channel.CreateUnbounded<T>(
            new UnboundedChannelOptions { SingleReader = true });
        lock (gate)
        {
            updates.Writer.TryWrite(current);
            subscribers.Add(updates.Writer);
        }
        try
        {
            await foreach (var value in updates.Reader.ReadAllAsync(cancellation))
                yield return value;
        }
        finally
        {
            lock (gate)
                subscribers.Remove(updates.Writer);
        }
    }
}
