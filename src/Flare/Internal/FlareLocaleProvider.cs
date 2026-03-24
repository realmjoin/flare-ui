using System.Text.Json;

namespace Flare.Internal;

/// <summary>
/// Reads locale JSON files embedded as static web assets and provides typed string lookups for prerender.
/// </summary>
internal sealed class FlareLocaleProvider
{
    private const string Fallback = "en-us";

    private readonly Dictionary<string, Dictionary<string, string>> _sections;

    public FlareLocaleProvider(string locale)
    {
        var assembly = typeof(FlareLocaleProvider).Assembly;
        var tag = locale.ToLowerInvariant();

        // Resolution order: exact tag → base-base (e.g. "de" → "de-de") → en-us
        var candidates = new List<string> { tag };
        if (!tag.Contains('-')) candidates.Add($"{tag}-{tag}");
        if (tag != Fallback && !candidates.Contains(Fallback)) candidates.Add(Fallback);

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
