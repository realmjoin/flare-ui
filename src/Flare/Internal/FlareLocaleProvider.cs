using System.Text.Json;

namespace Flare.Internal;

/// <summary>
/// Reads locale JSON files embedded as static web assets and provides typed string lookups for prerender.
/// </summary>
internal sealed class FlareLocaleProvider
{
    private const string Fallback = "en-us";

    private readonly Dictionary<string, Dictionary<string, string>> _sections;
    private readonly Dictionary<string, Dictionary<string, string>> _fallback;

    public FlareLocaleProvider(string locale)
    {
        var assembly = typeof(FlareLocaleProvider).Assembly;
        var tag = locale.ToLowerInvariant();

        // Resolution order: exact tag → language default region (e.g. "de"/"fr-ca" → "de-de"/"fr-fr") → en-us
        var candidates = new List<string> { tag };
        var lang = tag.Contains('-') ? tag[..tag.IndexOf('-')] : tag;
        if (!candidates.Contains($"{lang}-{lang}")) candidates.Add($"{lang}-{lang}");
        if (tag != Fallback && !candidates.Contains(Fallback)) candidates.Add(Fallback);

        using (var fallbackStream = assembly.GetManifestResourceStream($"Flare.wwwroot.locales.{Fallback}.json"))
        {
            _fallback = fallbackStream is not null ? Parse(fallbackStream) : new(StringComparer.OrdinalIgnoreCase);
        }

        foreach (var candidate in candidates)
        {
            var resourceName = $"Flare.wwwroot.locales.{candidate}.json";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                _sections = Parse(stream);
                return;
            }
        }

        throw new FileNotFoundException(
            $"Locale resource not found in assembly (tried {string.Join(", ", candidates)}).");
    }

    public IReadOnlyDictionary<string, string> Get(string section) =>
        _sections.TryGetValue(section, out var s) ? s : throw new KeyNotFoundException($"Locale section '{section}' not found.");

    /// <summary>
    /// Returns a built-in UI string (labels, status texts). Falls back to en-us per key,
    /// so a locale file that lacks a newer key never breaks rendering.
    /// </summary>
    public string Ui(string key)
    {
        if (_sections.TryGetValue("ui", out var s) && s.TryGetValue(key, out var value) && value.Length > 0)
            return value;
        if (_fallback.TryGetValue("ui", out var f) && f.TryGetValue(key, out var fallback))
            return fallback;
        return key;
    }

    /// <summary>Formats a built-in UI string that contains <c>{0}</c>-style placeholders.</summary>
    public string Ui(string key, params object?[] args) => string.Format(Ui(key), args);

    private static Dictionary<string, Dictionary<string, string>> Parse(Stream stream)
    {
        using var doc = JsonDocument.Parse(stream);
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var section in doc.RootElement.EnumerateObject())
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in section.Value.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
            result[section.Name] = dict;
        }

        return result;
    }
}
