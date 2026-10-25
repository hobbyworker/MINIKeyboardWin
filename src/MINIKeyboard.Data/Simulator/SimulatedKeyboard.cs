using MINIKeyboard.Data.Protocols;
using MINIKeyboard.Domain;

namespace MINIKeyboard.Data;

// Keyboards that exist only in the app, for trying every keyboard without owning it. They take the same reports a real
// one does, through the same codecs, and answer as their firmware would.

/// <summary>
/// A keyboard that exists only in the app. A 884x keyboard that answers says who it is and reads back what was written
/// to it.
/// </summary>
public sealed class SimulatedKeyboard : IReportChannel, IChannelProvider
{
    private readonly SimulatedModel model;
    private readonly Lock gate = new();
    private readonly Queue<byte[]> replies = new();

    /// <summary>Records by layer (from 1) and record number, as written.</summary>
    private readonly Dictionary<byte, Dictionary<byte, byte[]>> records = [];

    /// <summary>Standard-format lights by layer (from 0): the mode and 16 colours, as written.</summary>
    private readonly Dictionary<byte, byte[]> lighting = [];

    /// <summary>Every payload written, in order.</summary>
    private readonly List<byte[]> written = [];

    public SimulatedKeyboard(SimulatedModel model)
    {
        this.model = model;
        Framing = model.Kind is SimulatedKind.K8890 { ReportId: var reportId }
            ? new ReportFraming(reportId, reportId == 0 ? 64 : 65)
            : new ReportFraming(K884x.ReportId, K884x.ReportLength);
    }

    public ReportFraming Framing { get; }

    public ConnectedKeyboard Keyboard()
    {
        var family = model.Family;
        return new ConnectedKeyboard(
            model.Usb, family, family.ConfigurationInterface(), Channel.Simulated, Framing.ReportId, Framing.Length,
            Details: null);
    }

    /// <summary>Every payload written, in order.</summary>
    public IReadOnlyList<byte[]> Written()
    {
        lock (gate)
            return [.. written.Select(payload => payload.ToArray())];
    }

    /// <summary>
    /// The record kept in <paramref name="record"/> of <paramref name="layer"/> (from 1), as written.
    /// </summary>
    public byte[]? Record(byte layer, byte record)
    {
        lock (gate)
        {
            return records.TryGetValue(layer, out var layerRecords) && layerRecords.TryGetValue(record, out var bytes)
                ? bytes.ToArray()
                : null;
        }
    }

    public Task WriteAsync(byte[] report, CancellationToken cancellation = default)
    {
        lock (gate)
            Receive(report);
        return Task.CompletedTask;
    }

    public void DiscardPendingReplies()
    {
        lock (gate)
            replies.Clear();
    }

    /// <summary>The firmware answers as soon as it is asked, so a reply that isn't there won't come.</summary>
    public Task<byte[]?> NextReplyAsync(TimeSpan timeout, CancellationToken cancellation = default)
    {
        lock (gate)
            return Task.FromResult(replies.TryDequeue(out var reply) ? reply : null);
    }

    /// <summary>The one simulated keyboard, as the device repository's channels.</summary>
    public IReportChannel? OpenChannel() => this;

    private void Receive(byte[] report)
    {
        var payload = Framing.ReportId == 0 ? report : report.Length > 0 ? report[1..] : [];
        written.Add(payload);
        KeyboardIdentity identity;
        switch (model.Kind)
        {
            case SimulatedKind.Identified { Identity: var known }:
                identity = known;
                break;
            case SimulatedKind.Unidentified:
                if (payload is [0xFE, var number, var recordLayer, ..])
                    Store(recordLayer, number, payload);
                return;
            default:
                return;
        }
        byte At(int index) => index < payload.Length ? payload[index] : (byte)0;
        switch (At(0), At(1))
        {
            case (0xFB, _):
                Reply([0xFB, identity.KeyCount, identity.KnobCount, identity.Kind]);
                break;
            case (0xFA, 0xB0):
                Reply([0xB0, .. (lighting.GetValueOrDefault(At(2)) ?? InitialLighting())]);
                break;
            case (0xFA, _):
                {
                    var layer = At(3);
                    var count = K884x.ReadReplyCount(identity.RecordFormat);
                    for (byte id = 1; id <= count; id++)
                        Reply(Stored(layer, id) ?? InitialRecord(id, layer));
                    break;
                }
            case (0xFD, var record) when record != 0xFE:
                Store(At(2), record, payload);
                break;
            case (0xFE, 0xB0) when identity.RecordFormat == RecordFormat.Standard:
                {
                    var end = 4 + 3 * LightingSupport.ColorKeyCount;
                    lighting[At(2)] = payload.Length >= end ? payload[3..end] : [];
                    break;
                }
            case (0xFE, 0xB0):
                Store(At(2), 0, payload);
                break;
        }
    }

    private void Store(byte layer, byte record, byte[] payload)
    {
        if (!records.TryGetValue(layer, out var layerRecords))
            records[layer] = layerRecords = [];
        layerRecords[record] = payload;
    }

    private byte[]? Stored(byte layer, byte record) =>
        records.TryGetValue(layer, out var layerRecords) ? layerRecords.GetValueOrDefault(record) : null;

    private void Reply(ReadOnlySpan<byte> payload) => replies.Enqueue(Framing.Frame(payload));

    /// <summary>What the cross-platform app starts a record as: <c>FD &lt;record&gt; &lt;layer&gt; 01</c>.</summary>
    private static byte[] InitialRecord(byte id, byte layer)
    {
        var record = new byte[64];
        record[0] = 0xFD;
        record[1] = id;
        record[2] = layer;
        record[3] = 0x01;
        return record;
    }

    private static byte[] InitialLighting() =>
        [0, .. KeyColor.DefaultKeyColors.SelectMany(color => new[] { color.Red, color.Green, color.Blue })];
}
