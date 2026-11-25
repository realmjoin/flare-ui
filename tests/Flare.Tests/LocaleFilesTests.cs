using Xunit;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flare.Internal;

namespace Flare.Tests;

public class LocaleFilesTests
{
    private const string Prefix = "Flare.wwwroot.locales.";

    private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> LoadAll()
    {
        var assembly = typeof(FlareLocaleProvider).Assembly;
        var result = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith(Prefix)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var doc = JsonDocument.Parse(stream);
            result[name[Prefix.Length..^".json".Length]] = doc.RootElement.EnumerateObject().ToDictionary(
                section => section.Name,
                section => section.Value.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? ""));
        }

        return result;
    }

    private static string[] Placeholders(string text) =>
        Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Distinct().Order().ToArray();

    [Fact]
    public void EveryLocaleHasTheSameKeysAsEnglish()
    {
        var all = LoadAll();
        var english = all["en-us"];
        Assert.True(all.Count > 1);

        foreach (var (locale, sections) in all)
        {
            foreach (var (section, strings) in english)
            {
                Assert.True(sections.ContainsKey(section), $"{locale}: section '{section}' is missing");
                Assert.Equal(strings.Keys.Order(), sections[section].Keys.Order());
            }
        }
    }

    [Fact]
    public void UiStringsAreTranslatedWithTheSamePlaceholders()
    {
        var all = LoadAll();
        var english = all["en-us"]["ui"];

        foreach (var (locale, sections) in all)
        {
            foreach (var (key, value) in sections["ui"])
            {
                Assert.False(string.IsNullOrWhiteSpace(value), $"{locale}: ui.{key} is empty");
                Assert.Equal(Placeholders(english[key]), Placeholders(value));
            }
        }
    }

    [Fact]
    public void UiFallsBackToEnglishForUnknownLocale()
    {
        var provider = new FlareLocaleProvider("xx-yy");
        Assert.Equal("Close", provider.Ui("close"));
        Assert.Equal("Remove Alice", provider.Ui("removeFormat", "Alice"));
    }

    [Fact]
    public void UiUsesTheConfiguredLocale()
    {
        var provider = new FlareLocaleProvider("de");
        Assert.Equal("Schließen", provider.Ui("close"));
        Assert.Equal("3 ausgewählt / max. 5", provider.Ui("selectedMaxFormat", 3, 5));
    }
}
