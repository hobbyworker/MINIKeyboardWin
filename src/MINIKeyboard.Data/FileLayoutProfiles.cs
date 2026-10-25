using System.Text.Json;
using System.Text.Json.Serialization;
using MINIKeyboard.Domain;

namespace MINIKeyboard.Data;

/// <summary>
/// The layouts users chose, by keyboard, in a JSON file: <c>{"1189:8890": {"layout": "grid-6-3-1", "rotation": 2}}</c>
/// — the same file as the other editions write. A file that can't be read is treated as empty; a write that fails is
/// dropped — the user chooses again.
/// </summary>
public sealed class FileLayoutProfiles(string path) : ILayoutProfileRepository
{
    public LayoutProfile? Profile(UsbIdentity keyboard) =>
        Load().TryGetValue(keyboard.ToString(), out var stored)
            ? new LayoutProfile(stored.Layout, Rotations.FromQuarterTurns(stored.Rotation))
            : null;

    public void Save(LayoutProfile profile, UsbIdentity keyboard)
    {
        var all = new SortedDictionary<string, StoredProfile>(Load(), StringComparer.Ordinal)
        {
            [keyboard.ToString()] = new() { Layout = profile.LayoutId, Rotation = profile.Rotation.QuarterTurns() },
        };
        var json = JsonSerializer.SerializeToUtf8Bytes(all, ProfilesJson.Default.SortedDictionaryStringStoredProfile);
        // Written beside and moved over, so a crash never leaves half a file.
        var partial = Path.ChangeExtension(path, ".json.partial");
        try
        {
            if (Path.GetDirectoryName(path) is { Length: > 0 } directory)
                Directory.CreateDirectory(directory);
            File.WriteAllBytes(partial, json);
            File.Move(partial, path, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private Dictionary<string, StoredProfile> Load()
    {
        try
        {
            using var file = File.OpenRead(path);
            return JsonSerializer.Deserialize(file, ProfilesJson.Default.DictionaryStringStoredProfile) ?? [];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }
}

internal sealed class StoredProfile
{
    [JsonPropertyName("layout")]
    public required string Layout { get; init; }

    /// <summary>Quarter turns clockwise.</summary>
    [JsonPropertyName("rotation")]
    public required byte Rotation { get; init; }
}

/// <summary>Pretty-printed, keys in order, as the other editions write the file.</summary>
[JsonSourceGenerationOptions(WriteIndented = true, NewLine = "\n", RespectNullableAnnotations = true)]
[JsonSerializable(typeof(Dictionary<string, StoredProfile>))]
[JsonSerializable(typeof(SortedDictionary<string, StoredProfile>))]
internal sealed partial class ProfilesJson : JsonSerializerContext;
