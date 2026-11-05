using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.UI.Xaml;

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
        ["ko", "ja", "zh-Hans", "zh-Hant", "es", "es-419", "pt-BR", "pt-PT", "fr", "de", "it", "pl", "ru", "id", "vi", "th", "ar"];

    /// <summary>The regions whose Spanish is Latin America's: Latin America itself (419), its countries and the US.</summary>
    private static readonly HashSet<string> LatinAmericanSpanish =
    [
        "419", "AR", "BO", "BR", "BZ", "CL", "CO", "CR", "CU", "DO", "EC", "GT", "HN", "MX", "NI", "PA", "PE", "PR", "PY", "SV",
        "US", "UY", "VE",
    ];

    private static IReadOnlyDictionary<string, string> table = new Dictionary<string, string>();

    /// <summary>The language asked for at launch ("ko", "pt-BR"), or null to follow the system.</summary>
    public static string? Language { get; private set; }

    /// <summary>What the interface reads in: "en", or one of the translated languages.</summary>
    public static string Current { get; private set; } = "en";

    /// <summary>
    /// <see cref="Current"/> as Windows names its languages, for the words the Windows App SDK draws itself — a
    /// switch's On and Off come from its own file for each language (ko-KR\Microsoft.ui.xaml.dll.mui …).
    /// </summary>
    public static string WindowsLanguage => Current switch
    {
        "en" => "en-US",
        "ko" => "ko-KR",
        "ja" => "ja-JP",
        "zh-Hans" => "zh-CN",
        "zh-Hant" => "zh-TW",
        "es" => "es-ES",
        "es-419" => "es-MX",
        "fr" => "fr-FR",
        "de" => "de-DE",
        "it" => "it-IT",
        "pl" => "pl-PL",
        "ru" => "ru-RU",
        "id" => "id-ID",
        "vi" => "vi-VN",
        "th" => "th-TH",
        "ar" => "ar-SA",
        var language => language, // pt-BR, pt-PT
    };

    /// <summary>
    /// Which way <see cref="Current"/> is written — Arabic right to left, every other language left to right. WinUI doesn't
    /// take it from the language; the windows set it on their content.
    /// </summary>
    public static FlowDirection Flow => Current is "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    /// <summary>Called once at launch, before the first word is looked up.</summary>
    public static void Initialize(string? language)
    {
        Language = string.IsNullOrWhiteSpace(language) ? null : language;
        Current = Match(Language ?? CultureInfo.CurrentUICulture.Name);
        table = Load(Current);
    }

    /// <summary>A message in the user's language.</summary>
    public static string T(string message) => Translate(message);

    /// <summary>
    /// A message in the user's language, with its values put in — in a right-to-left language each set apart from the
    /// words around it, so "8890-3" doesn't read "3-8890".
    /// </summary>
    public static string T(string message, params object[] values) =>
        Fill(Translate(message), Flow == FlowDirection.RightToLeft ? [.. values.Select(Isolate)] : values);

    /// <summary>
    /// <paramref name="value"/> set apart from the words around it in a right-to-left language: its own direction inside,
    /// in its place outside. Unchanged in the other languages.
    /// </summary>
    /// <remarks>
    /// An embedding (LRE or RLE … PDF) in the direction of its first letter, as a first-strong isolate would be — XAML's
    /// text doesn't keep the isolates of Unicode 6.3 (FSI … PDI): a "8890-3" in them still read "3-8890".
    /// </remarks>
    public static string Isolate(object value)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        if (Flow != FlowDirection.RightToLeft)
            return text;
        return $"{(StartsRightToLeft(text) ? '\u202B' : '\u202A')}{text}\u202C";
    }

    /// <summary>
    /// Key names and presses as the keys print them, left to right — in Arabic a lone "(" would turn round and "Shift+("
    /// read "(+Shift". Unchanged in the other languages. An embedding (LRE … PDF), for the reason <see cref="Isolate"/> gives.
    /// </summary>
    public static string LeftToRight(string text) =>
        Flow == FlowDirection.RightToLeft ? $"\u202A{text}\u202C" : text;

    /// <summary>Whether the first letter of <paramref name="text"/> is written right to left — Hebrew or Arabic.</summary>
    private static bool StartsRightToLeft(string text)
    {
        foreach (var c in text)
        {
            if (c is >= '\u0590' and <= '\u08FF' or >= '\uFB1D' and <= '\uFDFF' or >= '\uFE70' and <= '\uFEFF')
                return true;
            if (char.IsLetter(c))
                return false;
        }
        return false;
    }

    private static string Translate(string message) => table.TryGetValue(message, out var text) ? text : message;

    /// <summary>
    /// The table for a language tag as Windows or the user gives it ("ko-KR", "zh-TW", "pt-AO", "zh-Hans", "es-MX") —
    /// Chinese by its script or region, Portuguese as Brazil's in Brazil and Portugal's anywhere else, Spanish as Latin
    /// America's there and in the US and Spain's anywhere else, any other by its language; "en" when there is none.
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
            return parts.Skip(1).Any(part => part.Equals("BR", StringComparison.OrdinalIgnoreCase)) ? "pt-BR" : "pt-PT";
        if (language == "es")
            return parts.Skip(1).Any(part => LatinAmericanSpanish.Contains(part.ToUpperInvariant())) ? "es-419" : "es";
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
