using MINIKeyboard.Domain;

namespace MINIKeyboard.Data.Protocols;

/// <summary>One payload of a save, and how long to wait after it.</summary>
public sealed record WriteStep(byte[] Payload, TimeSpan PauseAfter);

/// <summary>
/// The payloads that write a configuration's changes, in the order the keyboard's vendor tool writes them.
/// </summary>
public sealed class WritePlan
{
    private readonly List<WriteStep> steps = [];

    /// <exception cref="KeyboardDeviceException">A change the keyboard has no command or room for.</exception>
    /// <param name="preventLightingKeyRepeat">
    /// Whether the 1189:8890's lighting save ends with the save that stops its key repeat (K8890.DownloadLighting) —
    /// a setting, on unless the user turns it off.
    /// </param>
    public WritePlan(
        KeyboardConfiguration configuration, CommandSet commands, KeyboardLayout layout,
        bool preventLightingKeyRepeat = true)
    {
        var layers = configuration.LayerCount;
        switch (commands)
        {
            case CommandSet.Packets { ReportId: var reportId }:
                // The lights, then one key at a time, each saved on its own (Download_Click).
                for (var layer = 0; layer < layers; layer++)
                {
                    if (configuration.Layers[layer].LightingChanged
                        && configuration.Lighting(layer) is LayerLighting.ModeOnly { Mode: var mode })
                    {
                        Append(K8890.DownloadLighting(mode, layer, reportId, preventLightingKeyRepeat));
                    }
                    foreach (var (control, action) in Changes(configuration, layer, layout))
                    {
                        if (Numbers.K8890Number(control) is { } number)
                            Append(K8890.Download(number, action, layer, reportId));
                    }
                }
                break;
            case CommandSet.Records { Format: RecordFormat.Standard }:
                {
                    // Every changed record, then — when a light changed — every layer's lights, then one commit if a
                    // record was written (Widget::HID_write).
                    var wroteRecords = false;
                    for (var layer = 0; layer < layers; layer++)
                    {
                        foreach (var (control, action) in Changes(configuration, layer, layout))
                        {
                            Append([Records.Standard(action, Record(control, layout), layer)]);
                            wroteRecords = true;
                        }
                    }
                    if (configuration.Layers.Any(layer => layer.LightingChanged))
                    {
                        for (var layer = 0; layer < layers; layer++)
                        {
                            if (configuration.Lighting(layer) is LayerLighting.PerKey(var mode, var colors))
                                Append([K884x.Lighting(layer, mode, colors)]);
                        }
                    }
                    if (wroteRecords)
                        Commit();
                    break;
                }
            case CommandSet.Records { Format: RecordFormat.Legacy }:
                // Each layer's changed records, its lights in record 0, and a commit.
                for (var layer = 0; layer < layers; layer++)
                {
                    var wroteRecords = false;
                    foreach (var (control, action) in Changes(configuration, layer, layout))
                    {
                        Append([Records.Encode(action, Record(control, layout), layer, RecordFormat.Legacy)]);
                        wroteRecords = true;
                    }
                    if (configuration.Layers[layer].LightingChanged
                        && configuration.Lighting(layer) is LayerLighting.ColorMode(var color, var mode))
                    {
                        Append([Records.LegacyLighting(color, mode, layer, Records.LegacyLength)]);
                        wroteRecords = true;
                    }
                    if (wroteRecords)
                        Commit();
                }
                break;
            case CommandSet.Records { Format: RecordFormat.Unidentified }:
                // One record at a time, each saved on its own.
                for (var layer = 0; layer < layers; layer++)
                {
                    foreach (var (control, action) in Changes(configuration, layer, layout))
                    {
                        Append([Records.Unidentified(action, Record(control, layout), layer)]);
                        Append(K884x.UnidentifiedSave());
                    }
                    if (configuration.Layers[layer].LightingChanged
                        && configuration.Lighting(layer) is LayerLighting.ColorMode(var color, var mode))
                    {
                        Append(K884x.UnidentifiedLighting(color, mode, layer));
                    }
                }
                break;
        }
    }

    public IReadOnlyList<WriteStep> Steps => steps;

    private void Append(IEnumerable<byte[]> payloads)
    {
        foreach (var payload in payloads)
            steps.Add(new WriteStep(payload, TimeSpan.Zero));
    }

    private void Commit() => steps.Add(new WriteStep(K884x.Commit.ToArray(), K884x.CommitPause));

    /// <summary>Changed controls of one layer that the layout has, in control order.</summary>
    private static List<(ControlId Control, KeyAction Action)> Changes(
        KeyboardConfiguration configuration, int layer, KeyboardLayout layout)
    {
        var changes = new List<(ControlId, KeyAction)>();
        foreach (var control in layout.Controls.Where(control => configuration.IsChanged(control, layer)).Order())
        {
            if (configuration.Action(control, layer) is { } action)
                changes.Add((control, action));
        }
        return changes;
    }

    /// <summary>The record a 884x keyboard keeps <paramref name="control"/> in.</summary>
    private static byte Record(ControlId control, KeyboardLayout layout) =>
        Numbers.Record(control, layout) ?? throw DeviceErrors.Unsupported();
}
