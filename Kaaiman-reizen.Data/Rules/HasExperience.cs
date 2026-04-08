namespace Kaaiman_reizen.Data.Rules;

public static class HasExperience
{
    public static bool Check(int requiredExperience, string destination, int? experience = 0)
    {
        // If destination is in Europe, always allowed
        if (Enum.TryParse<Countries>(destination, true, out _))
        {
            return true;
        }
        return experience >= requiredExperience;
    }
}

// List of European countries
public enum Countries
{
    Albanie,
    Andorra,
    Armenie,
    Azerbeidzjan,
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
    Zweden,
    Zwitserland
}