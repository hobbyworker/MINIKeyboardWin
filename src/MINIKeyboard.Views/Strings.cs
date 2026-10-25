using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MINIKeyboard.Views;

/// <summary>
/// The interface's words. Every text a user reads goes through here, looked up by its English text — the same keys as
/// the other editions' string catalogs, <c>%lld</c> and <c>%@</c> and all, so their translations carry over. The tables
/// (<c>Translations/&lt;language&gt;.json</c>, built into the assembly) are made from the macOS edition's catalog; a
/// message a table lacks reads in English.
/// </summary>
public static class Strings
{
    /// <summary>The languages there is a table for; English is the source and needs none.</summary>
    private static readonly string[] Translated =
        ["ko", "ja", "zh-Hans", "zh-Hant", "es", "pt-BR", "fr", "de", "it", "pl", "id", "vi"];

    private static IReadOnlyDictionary<string, string> table = new Dictionary<string, string>();

    /// <summary>The language asked for at launch ("ko", "pt-BR"), or null to follow the system.</summary>
    public static string? Language { get; private set; }

    /// <summary>What the interface reads in: "en", or one of the translated languages.</summary>
    public static string Current { get; private set; } = "en";

    /// <summary>Called once at launch, before the first word is looked up.</summary>
    public static void Initialize(string? language)
    {
        Language = string.IsNullOrWhiteSpace(language) ? null : language;
        Current = Match(Language ?? CultureInfo.CurrentUICulture.Name);
        table = Load(Current);
    }

    /// <summary>A message in the user's language.</summary>
    public static string T(string message) => Translate(message);

    /// <summary>A message in the user's language, with its values put in.</summary>
    public static string T(string message, params object[] values) => Fill(Translate(message), values);

    private static string Translate(string message) => table.TryGetValue(message, out var text) ? text : message;

    /// <summary>
    /// The table for a language tag as Windows or the user gives it ("ko-KR", "zh-TW", "pt-PT", "zh-Hans") — Chinese by
    /// its script or region, Portuguese as Brazil's, any other by its language; "en" when there is none.
    /// </summary>
    private static string Match(string tag)
    {
        var parts = tag.Split('-', '_');
        var language = parts[0].ToLowerInvariant();
        if (language == "zh")
        {
            var traditional = parts.Skip(1).Any(part =>
                part.Equals("Hant", StringComparison.OrdinalIgnoreCase) || part.ToUpperInvariant() is "TW" or "HK" or "MO");
            return traditional ? "zh-Hant" : "zh-Hans";
        }
        if (language == "pt")
            return "pt-BR";
        return Translated.Contains(language) ? language : "en";
    }

    private static Dictionary<string, string> Load(string language)
    {
        using var stream = typeof(Strings).Assembly.GetManifestResourceStream($"Translations.{language}.json");
        return stream is null ? [] : JsonSerializer.Deserialize(stream, SettingsJson.Default.DictionaryStringString) ?? [];
    }

    /// <summary>
    /// Replaces <c>%lld</c>, <c>%ld</c>, <c>%d</c> and <c>%@</c> with <paramref name="values"/> in order, and
    /// <c>%1$lld</c>, <c>%2$@</c> with the value of that number; <c>%%</c> is a percent sign.
    /// </summary>
    public static string Fill(string template, IReadOnlyList<object> values)
    {
        var text = new StringBuilder(template.Length + 16);
        var next = 0;
        var index = 0;
        while (index < template.Length)
        {
            var start = template.IndexOf('%', index);
            if (start < 0)
            {
                text.Append(template, index, template.Length - index);
                break;
            }
            text.Append(template, index, start - index);
            var after = start + 1;
            if (after < template.Length && template[after] == '%')
            {
                text.Append('%');
                index = after + 1;
                continue;
            }
            var digits = 0;
            while (after + digits < template.Length && char.IsAsciiDigit(template[after + digits]))
                digits++;
            int? position = null;
            var spec = after;
            if (digits > 0 && after + digits < template.Length && template[after + digits] == '$')
            {
                position = int.Parse(template.AsSpan(after, digits), CultureInfo.InvariantCulture) - 1;
                spec = after + digits + 1;
            }
            var length = SpecifierLength(template, spec);
            if (length == 0)
            {
                text.Append('%');
                index = after;
                continue;
            }
            var value = position ?? next++;
            if (value >= 0 && value < values.Count)
                text.Append(Convert.ToString(values[value], CultureInfo.InvariantCulture));
            index = spec + length;
        }
        return text.ToString();
    }

    private static int SpecifierLength(string template, int at)
    {
        foreach (var kind in (ReadOnlySpan<string>)["lld", "ld", "d", "@"])
        {
            if (string.CompareOrdinal(template, at, kind, 0, kind.Length) == 0)
                return kind.Length;
        }
        return 0;
    }
}
