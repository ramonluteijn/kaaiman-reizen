using System.Globalization;
using System.Text;

namespace Kaaiman_reizen.Data.Enum;

// List of European countries
public enum Countries
{
    Albanie,
    Andorra,
    Armenie,
    Azerbeidzjan,
    Belarus,
    Belgie,
    BosnieEnHerzegovina,
    Bulgarije,
    Cyprus,
    Denemarken,
    Duitsland,
    Estland,
    Finland,
    Frankrijk,
    Georgie,
    Griekenland,
    Hongarije,
    Ierland,
    IJsland,
    Italie,
    Kosovo,
    Kroatie,
    Letland,
    Liechtenstein,
    Litouwen,
    Luxemburg,
    Malta,
    Moldavie,
    Monaco,
    Montenegro,
    Nederland,
    NoordMacedonie,
    Noorwegen,
    Oekraine,
    Oostenrijk,
    Polen,
    Portugal,
    Roemenie,
    Rusland,
    SanMarino,
    Schotland,
    Servie,
    Slovenie,
    Slowakije,
    Spanje,
    Tsjechie,
    Turkije,
    Vaticaanstad,
    VerenigdKoninkrijk,
    Wit_Rusland,
    Zweden,
    Zwitserland
}

public static class CountryMappings
{
    public static readonly Dictionary<string, Countries> AlternativeCountryNames = new()
    {
        { "Wit-Rusland", Countries.Wit_Rusland }
    };

    public static string NormalizeCountryName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (character == '-' || character == '_' || char.IsWhiteSpace(character))
            {
                continue;
            }

            builder.Append(char.ToUpperInvariant(character));
        }

        return builder.ToString();
    }

    // check if journey title contains the country name
    public static bool DestinationContainsCountry(string normalizedDestination, string countryName)
    {
        var normalizedCountry = NormalizeCountryName(countryName);
        return !string.IsNullOrEmpty(normalizedCountry) &&
               normalizedDestination.Contains(normalizedCountry, StringComparison.OrdinalIgnoreCase);
    }
}
