using System.Text.Json;
using System.Text.Json.Serialization;

namespace MINIKeyboard.Views;

/// <summary>Light, dark or the system's, for the app's windows. Switching is live.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Appearance>))]
public enum Appearance
{
    System,
    Light,
    Dark,
}

/// <summary>What the user chose for the interface itself.</summary>
/// <remarks>
/// The defaults are the constructor's: read from a file written before a choice existed, a missing value gets its
/// parameter's default — the JSON source generator would set an init property's to false or null instead.
/// </remarks>
/// <param name="Language">A language code ("ko", "pt-BR"), or null to follow the system.</param>
/// <param name="Appearance">Light, dark or the system's.</param>
/// <param name="KeyArrangement">"qwerty" or "abc", or null until the user picks one.</param>
/// <param name="PreventLightingKeyRepeat">
/// Whether the 1189:8890's lighting save ends with the save that stops its key repeat (the vendor's tool doesn't send
/// it). On unless the user turns it off.
/// </param>
/// <param name="CheckForUpdates">
/// Whether a build with updates looks for a new version by itself, at launch and once a day. On unless the user turns
/// it off.
/// </param>
public sealed record InterfaceChoices(
    string? Language = null,
    Appearance Appearance = Appearance.System,
    string? KeyArrangement = null,
    bool PreventLightingKeyRepeat = true,
    bool CheckForUpdates = true);

/// <summary>
/// The interface's choices — its language, light or dark, how the Keys page lays out the keys — in a small JSON file
/// beside the app's other settings. Read once; written on every change.
/// </summary>
public sealed class InterfaceSettings
{
    private readonly string path;

    private InterfaceSettings(string path, InterfaceChoices choices)
    {
        this.path = path;
        Choices = choices;
    }

    public InterfaceChoices Choices { get; private set; }

    /// <summary>
    /// The languages the app speaks, each in its own script — someone looking for theirs recognises it.
    /// </summary>
    public static IReadOnlyList<(string Code, string Name)> Languages { get; } =
    [
        ("en", "English"),
        ("ko", "한국어"),
        ("ja", "日本語"),
        ("zh-Hans", "简体中文"),
        ("zh-Hant", "繁體中文"),
        ("es", "Español"),
        ("pt-BR", "Português (Brasil)"),
        ("fr", "Français"),
        ("de", "Deutsch"),
        ("it", "Italiano"),
        ("pl", "Polski"),
        ("id", "Bahasa Indonesia"),
        ("vi", "Tiếng Việt"),
    ];

    /// <summary>A file that can't be read gives the defaults.</summary>
    public static InterfaceSettings Load(string path)
    {
        InterfaceChoices? choices = null;
        try
        {
            using var file = File.OpenRead(path);
            choices = JsonSerializer.Deserialize(file, SettingsJson.Default.InterfaceChoices);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
        }
        return new InterfaceSettings(path, choices ?? new InterfaceChoices());
    }

    /// <summary>Changes the choices and writes them; a write that fails is dropped.</summary>
    public void Update(Func<InterfaceChoices, InterfaceChoices> change)
    {
        Choices = change(Choices);
        var json = JsonSerializer.SerializeToUtf8Bytes(Choices, SettingsJson.Default.InterfaceChoices);
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
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(InterfaceChoices))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class SettingsJson : JsonSerializerContext;
