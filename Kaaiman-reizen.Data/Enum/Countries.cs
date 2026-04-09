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
}
