using Bogus;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kaaiman_reizen.Data.Seeders;

/// <summary>
/// Genereert realistische, deterministische testdata voor de ontwikkelomgeving.
/// Bevat 16 actieve reisleiders met bewuste edge cases voor het planningsalgoritme,
/// historische reizen (2024–2025), 2026-reizen, en één planningsronde (Periode 1 2026)
/// met volledig ingediende voorkeuren — klaar om een draft te genereren en te publiceren.
/// </summary>
public class DatabaseSeeder(MainContext context, ILogger<DatabaseSeeder> logger)
{
    private const string SeedMarker = "[DEV-SEED]";

    public async Task SeedAsync()
    {
        bool leadersSeeded = await context.TravelLeader.AnyAsync(t => t.Note.Contains(SeedMarker));
        bool journeysSeeded = await context.Journey.AnyAsync(j => j.Start.Year == 2024);

        if (leadersSeeded || journeysSeeded)
        {
            logger.LogInformation("Dev seed data al aanwezig — overgeslagen.");
            return;
        }

        Randomizer.Seed = new Random(1337);
        var faker = new Faker("nl");

        logger.LogInformation("Development data seeden gestart...");

        var leaders = BuildLeaders(faker);
        context.TravelLeader.AddRange(leaders.Values);
        await context.SaveChangesAsync();

        var (journeys2024, journeys2025) = BuildHistoricalJourneys();
        context.Journey.AddRange(journeys2024.Values);
        context.Journey.AddRange(journeys2025.Values);
        await context.SaveChangesAsync();

        await SeedHistoricalAssignmentsAsync(leaders, journeys2024, journeys2025);

        var journeys2026 = BuildCurrentJourneys();
        context.Journey.AddRange(journeys2026);
        await context.SaveChangesAsync();

        await SeedPlanningRoundAsync(leaders);

        logger.LogInformation(
            "Dev seed voltooid — {Leaders} reisleiders, {Hist} historische reizen, {Current} reizen 2026, 1 planningsronde.",
            leaders.Count,
            journeys2024.Count + journeys2025.Count,
            journeys2026.Count);
    }

    // ─── REISLEIDERS ────────────────────────────────────────────────────────────

    private Dictionary<string, TravelLeader> BuildLeaders(Faker faker)
    {
        string Phone() => $"06-{faker.Random.Number(10000000, 99999999)}";
        string Mark(string note) => $"{SeedMarker} {note}";

        return new Dictionary<string, TravelLeader>
        {
            // ═══════════════════════════════════════════════════════════════════
            // GROEP 1 — KERNGROEP (5)
            // Breed beschikbaar heel 2026, ervaren, vormen de ruggengraat van
            // de planning.
            // ═══════════════════════════════════════════════════════════════════

            ["jan"] = new()
            {
                Name = "Jan de Vries",
                PhoneNumber = "06-12345678",
                AmountOfTrips = 8,
                MinTrips = 1,
                MaxTrips = 4,
                IsActive = true,
                Note = Mark("Kernlid — breed beschikbaar"),
                AvailabilityPeriods = [new() { Start = new(2026, 1, 1), End = new(2026, 12, 31) }]
            },
            ["maria"] = new()
            {
                Name = "Maria Jansen",
                PhoneNumber = "06-87654321",
                AmountOfTrips = 12,
                MinTrips = 1,
                MaxTrips = 4,
                IsActive = true,
                Note = Mark("Kernlid — ervaren, breed beschikbaar"),
                AvailabilityPeriods = [new() { Start = new(2026, 1, 1), End = new(2026, 12, 31) }]
            },
            ["thomas"] = new()
            {
                Name = "Thomas de Groot",
                PhoneNumber = Phone(),
                AmountOfTrips = 18,
                MinTrips = 1,
                MaxTrips = 4,
                IsActive = true,
                Note = Mark("Kernlid — meest ervaren"),
                AvailabilityPeriods = [new() { Start = new(2026, 2, 1), End = new(2026, 12, 31) }]
            },
            ["daan"] = new()
            {
                Name = "Daan Peters",
                PhoneNumber = Phone(),
                AmountOfTrips = 16,
                MinTrips = 1,
                MaxTrips = 4,
                IsActive = true,
                Note = Mark("Kernlid — gespecialiseerd in Scandinavië"),
                AvailabilityPeriods = [new() { Start = new(2026, 1, 1), End = new(2026, 12, 31) }]
            },
            ["fleur"] = new()
            {
                Name = "Fleur Visser",
                PhoneNumber = Phone(),
                AmountOfTrips = 7,
                MinTrips = 1,
                MaxTrips = 3,
                IsActive = true,
                Note = Mark("Kernlid — niet in winter beschikbaar"),
                AvailabilityPeriods = [new() { Start = new(2026, 3, 1), End = new(2026, 11, 30) }]
            },

            // ═══════════════════════════════════════════════════════════════════
            // GROEP 2 — LENTEGASTEN (3)
            // Alleen beschikbaar maart–juni. Kunnen niet voor zomer-/herstreizen
            // worden ingepland.
            // ═══════════════════════════════════════════════════════════════════

            ["anna"] = new()
            {
                Name = "Anna Smits",
                PhoneNumber = Phone(),
                AmountOfTrips = 4,
                MinTrips = 0,
                MaxTrips = 2,
                IsActive = true,
                Note = Mark("Lentegast — beschikbaar mrt-jun"),
                AvailabilityPeriods = [new() { Start = new(2026, 3, 1), End = new(2026, 6, 30) }]
            },
            ["clara"] = new()
            {
                Name = "Clara Kuijpers",
                PhoneNumber = Phone(),
                AmountOfTrips = 3,
                MinTrips = 0,
                MaxTrips = 2,
                IsActive = true,
                Note = Mark("Lentegast — beschikbaar mrt-mei"),
                AvailabilityPeriods = [new() { Start = new(2026, 3, 1), End = new(2026, 5, 31) }]
            },
            ["david"] = new()
            {
                Name = "David Mulder",
                PhoneNumber = Phone(),
                AmountOfTrips = 8,
                MinTrips = 0,
                MaxTrips = 2,
                IsActive = true,
                Note = Mark("Lentegast — veel ervaring"),
                AvailabilityPeriods = [new() { Start = new(2026, 3, 1), End = new(2026, 6, 30) }]
            },

            // ═══════════════════════════════════════════════════════════════════
            // GROEP 3 — ZOMERSPECIALISTEN (3)
            // Alleen beschikbaar juni–september.
            // ═══════════════════════════════════════════════════════════════════

            ["frank"] = new()
            {
                Name = "Frank Willems",
                PhoneNumber = Phone(),
                AmountOfTrips = 9,
                MinTrips = 0,
                MaxTrips = 3,
                IsActive = true,
                Note = Mark("Zomers — beschikbaar jun-sep"),
                AvailabilityPeriods = [new() { Start = new(2026, 6, 1), End = new(2026, 9, 30) }]
            },
            ["gerda"] = new()
            {
                Name = "Gerda van Dijk",
                PhoneNumber = Phone(),
                AmountOfTrips = 7,
                MinTrips = 1,
                MaxTrips = 3,
                IsActive = true,
                Note = Mark("Zomers — beschikbaar jul-sep"),
                AvailabilityPeriods = [new() { Start = new(2026, 7, 1), End = new(2026, 9, 30) }]
            },
            ["hans"] = new()
            {
                Name = "Hans Bosman",
                PhoneNumber = Phone(),
                AmountOfTrips = 12,
                MinTrips = 0,
                MaxTrips = 3,
                IsActive = true,
                Note = Mark("Zomers — veel ervaring"),
                AvailabilityPeriods = [new() { Start = new(2026, 6, 1), End = new(2026, 9, 30) }]
            },

            // ═══════════════════════════════════════════════════════════════════
            // GROEP 4 — SMALLE BESCHIKBAARHEID (2)
            // ═══════════════════════════════════════════════════════════════════

            ["karel"] = new()
            {
                Name = "Karel Linden",
                PhoneNumber = Phone(),
                AmountOfTrips = 3,
                MinTrips = 0,
                MaxTrips = 2,
                IsActive = true,
                Note = Mark("Smal venster — alleen beschikbaar 5-25 apr"),
                AvailabilityPeriods = [new() { Start = new(2026, 4, 5), End = new(2026, 4, 25) }]
            },
            ["laura"] = new()
            {
                Name = "Laura Straten",
                PhoneNumber = Phone(),
                AmountOfTrips = 5,
                MinTrips = 1,
                MaxTrips = 2,
                IsActive = true,
                Note = Mark("Smal venster — alleen beschikbaar 1-21 jul"),
                AvailabilityPeriods = [new() { Start = new(2026, 7, 1), End = new(2026, 7, 21) }]
            },

            // ═══════════════════════════════════════════════════════════════════
            // GROEP 5 — INACTIEF (2)
            // ═══════════════════════════════════════════════════════════════════

            ["omar"] = new()
            {
                Name = "Omar Hassan",
                PhoneNumber = Phone(),
                AmountOfTrips = 9,
                MinTrips = 0,
                MaxTrips = 3,
                IsActive = false,
                Note = Mark("INACTIEF — heeft beschikbaarheid maar mag niet worden ingepland"),
                AvailabilityPeriods = [new() { Start = new(2026, 1, 1), End = new(2026, 12, 31) }]
            },
            ["priya"] = new()
            {
                Name = "Priya Singh",
                PhoneNumber = Phone(),
                AmountOfTrips = 5,
                MinTrips = 0,
                MaxTrips = 2,
                IsActive = false,
                Note = Mark("INACTIEF — ook geen beschikbaarheidsperiodes opgegeven"),
            },

            // ═══════════════════════════════════════════════════════════════════
            // GROEP 6 — GEEN BESCHIKBAARHEID (1)
            // Actief maar heeft geen AvailabilityPeriods — nooit inplanbaar.
            // ═══════════════════════════════════════════════════════════════════

            ["quinten"] = new()
            {
                Name = "Quinten Vos",
                PhoneNumber = Phone(),
                AmountOfTrips = 4,
                MinTrips = 0,
                MaxTrips = 2,
                IsActive = true,
                Note = Mark("Geen beschikbaarheid — actief maar nooit inplanbaar"),
            },

            // ═══════════════════════════════════════════════════════════════════
            // GROEP 7 — BIJZONDERE CONSTRAINTS (2)
            // ═══════════════════════════════════════════════════════════════════

            ["sebastiaan"] = new()
            {
                Name = "Sebastiaan van Ooijen",
                PhoneNumber = Phone(),
                AmountOfTrips = 0,
                MinTrips = 1,
                MaxTrips = 1,
                IsActive = true,
                Note = Mark("Beginner — MaxTrips=1, kan maar 1 reis doen"),
                AvailabilityPeriods = [new() { Start = new(2026, 1, 1), End = new(2026, 12, 31) }]
            },
            ["vera"] = new()
            {
                Name = "Vera Molenkamp",
                PhoneNumber = Phone(),
                AmountOfTrips = 0,
                MinTrips = 2,
                MaxTrips = 3,
                IsActive = true,
                Note = Mark("AmountOfTrips=0 maar MinTrips=2 — ruim onder minimum"),
                AvailabilityPeriods = [new() { Start = new(2026, 1, 1), End = new(2026, 12, 31) }]
            },
        };
    }

    // ─── HISTORISCHE REIZEN (2024 + 2025) ───────────────────────────────────────

    private (Dictionary<string, Journey> journeys2024, Dictionary<string, Journey> journeys2025) BuildHistoricalJourneys()
    {
        Journey J(string name, int year, int mStart, int dStart, int mEnd, int dEnd,
            int busses, int travelers, int required) => new()
            {
                Name = name,
                Start = new(year, mStart, dStart),
                End = new(year, mEnd, dEnd),
                Busses = busses,
                Travelers = travelers,
                RequiredLeaders = required,
                BookingStatus = BookingStatus.Geweest
            };

        var j2024 = new Dictionary<string, Journey>
        {
            ["ital"] = J("Italië", 2024, 7, 1, 7, 14, 2, 20, 2),
            ["griek"] = J("Griekenland", 2024, 4, 10, 4, 22, 1, 10, 1),
            ["span"] = J("Spanje", 2024, 3, 15, 3, 25, 1, 12, 1),
            ["kro"] = J("Kroatië", 2024, 6, 2, 6, 14, 1, 9, 1),
            ["port"] = J("Portugal", 2024, 9, 8, 9, 18, 1, 11, 1),
            ["norw"] = J("Noorwegen", 2024, 10, 5, 10, 15, 2, 18, 2),
            ["turk"] = J("Turkije", 2024, 4, 28, 5, 8, 1, 8, 1),
            ["oost"] = J("Oostenrijk", 2024, 3, 20, 3, 30, 1, 9, 1),
        };

        var j2025 = new Dictionary<string, Journey>
        {
            ["ital"] = J("Italië", 2025, 7, 6, 7, 19, 2, 20, 2),
            ["span"] = J("Spanje", 2025, 3, 9, 3, 19, 1, 12, 1),
            ["griek"] = J("Griekenland", 2025, 4, 6, 4, 16, 2, 22, 2),
            ["port"] = J("Portugal", 2025, 3, 23, 4, 2, 1, 10, 1),
            ["kro"] = J("Kroatië", 2025, 5, 5, 5, 17, 1, 11, 1),
            ["turk"] = J("Turkije", 2025, 4, 21, 5, 1, 1, 9, 1),
            ["norw"] = J("Noorwegen", 2025, 10, 6, 10, 16, 2, 16, 2),
            ["ijsl"] = J("IJsland", 2025, 11, 3, 11, 13, 1, 8, 1),
            ["zwit"] = J("Zwitserland", 2025, 9, 8, 9, 18, 1, 10, 1),
            ["mar"] = J("Marokko", 2025, 2, 10, 2, 22, 1, 13, 1),
        };

        return (j2024, j2025);
    }

    // ─── HISTORISCHE KOPPELINGEN + GEPUBLICEERDE PLANNING 2025 ──────────────────

    private async Task SeedHistoricalAssignmentsAsync(
        Dictionary<string, TravelLeader> l,
        Dictionary<string, Journey> j2024,
        Dictionary<string, Journey> j2025)
    {
        var jtl2024 = new List<JourneyTravelLeader>
        {
            new() { JourneyId = j2024["ital"].Id,  TravelLeaderId = l["jan"].Id    },
            new() { JourneyId = j2024["ital"].Id,  TravelLeaderId = l["thomas"].Id },
            new() { JourneyId = j2024["griek"].Id, TravelLeaderId = l["maria"].Id  },
            new() { JourneyId = j2024["span"].Id,  TravelLeaderId = l["fleur"].Id  },
            new() { JourneyId = j2024["kro"].Id,   TravelLeaderId = l["frank"].Id  },
            new() { JourneyId = j2024["port"].Id,  TravelLeaderId = l["maria"].Id  },
            new() { JourneyId = j2024["norw"].Id,  TravelLeaderId = l["daan"].Id   },
            new() { JourneyId = j2024["norw"].Id,  TravelLeaderId = l["fleur"].Id  },
            new() { JourneyId = j2024["turk"].Id,  TravelLeaderId = l["thomas"].Id },
            new() { JourneyId = j2024["oost"].Id,  TravelLeaderId = l["clara"].Id  },
        };
        context.JourneyTravelLeaders.AddRange(jtl2024);
        await context.SaveChangesAsync();

        var version2025 = new PlanningVersion
        {
            Name = "Gepubliceerde Planning 2025",
            CreatedAt = new DateTime(2025, 1, 20, 9, 0, 0),
            IsPublished = true,
            PlanningYear = 2025
        };
        context.PlanningVersions.Add(version2025);
        await context.SaveChangesAsync();

        var assignments2025 = new (string journey, string[] leaders)[]
        {
            ("ital",  ["jan",   "frank"]),
            ("span",  ["maria"]),
            ("griek", ["thomas","anna"]),
            ("port",  ["fleur"]),
            ("kro",   ["anna"]),
            ("turk",  ["thomas"]),
            ("norw",  ["daan",  "jan"]),
            ("ijsl",  ["daan"]),
            ("zwit",  ["fleur"]),
            ("mar",   ["anna"]),
        };

        var planningAssignments = new List<PlanningAssignment>();
        var jtl2025 = new List<JourneyTravelLeader>();

        foreach (var (journeyKey, leaderKeys) in assignments2025)
        {
            foreach (var leaderKey in leaderKeys)
            {
                planningAssignments.Add(new()
                {
                    PlanningVersionId = version2025.Id,
                    JourneyId = j2025[journeyKey].Id,
                    TravelLeaderId = l[leaderKey].Id
                });
                jtl2025.Add(new() { JourneyId = j2025[journeyKey].Id, TravelLeaderId = l[leaderKey].Id });
            }
        }

        context.PlanningAssignments.AddRange(planningAssignments);
        context.JourneyTravelLeaders.AddRange(jtl2025);
        await context.SaveChangesAsync();
    }

    // ─── REIZEN 2026 ─────────────────────────────────────────────────────────────

    private List<Journey> BuildCurrentJourneys() =>
    [
        // ── Voorjaar / lente (Periode 1) ─────────────────────────────────────
        new() { Name = "Spanje Voorjaar",   Start = new(2026, 3,  8), End = new(2026, 3, 18), Busses = 1, Travelers = 12, RequiredLeaders = 1, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Portugal",          Start = new(2026, 3, 22), End = new(2026, 4,  1), Busses = 1, Travelers = 10, RequiredLeaders = 1, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Marokko",           Start = new(2026, 4,  5), End = new(2026, 4, 15), Busses = 1, Travelers = 13, RequiredLeaders = 1, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Italië Voorjaar",   Start = new(2026, 4, 19), End = new(2026, 4, 29), Busses = 2, Travelers = 18, RequiredLeaders = 2, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Griekenland Lente", Start = new(2026, 5,  4), End = new(2026, 5, 14), Busses = 1, Travelers = 11, RequiredLeaders = 1, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Kroatië Lente",     Start = new(2026, 5, 18), End = new(2026, 5, 28), Busses = 1, Travelers =  9, RequiredLeaders = 1, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Turkije",           Start = new(2026, 6,  7), End = new(2026, 6, 17), Busses = 1, Travelers =  9, RequiredLeaders = 1, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Spanje Zomer",      Start = new(2026, 6, 21), End = new(2026, 7,  1), Busses = 2, Travelers = 20, RequiredLeaders = 2, BookingStatus = BookingStatus.Huidig },

        // ── Zomer / herfst (Periode 2) ────────────────────────────────────────
        new() { Name = "Italië Zomer",      Start = new(2026, 7,  5), End = new(2026, 7, 18), Busses = 2, Travelers = 20, RequiredLeaders = 2, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Griekenland Zomer", Start = new(2026, 7, 20), End = new(2026, 7, 30), Busses = 2, Travelers = 22, RequiredLeaders = 2, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Kroatië Zomer",     Start = new(2026, 8,  3), End = new(2026, 8, 13), Busses = 1, Travelers = 10, RequiredLeaders = 1, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Montenegro",        Start = new(2026, 8, 17), End = new(2026, 8, 27), Busses = 1, Travelers =  8, RequiredLeaders = 1, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Zwitserland",       Start = new(2026, 9,  6), End = new(2026, 9, 16), Busses = 1, Travelers = 10, RequiredLeaders = 1, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Albanië",           Start = new(2026, 9, 20), End = new(2026, 9, 30), Busses = 1, Travelers =  8, RequiredLeaders = 1, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Noorwegen",         Start = new(2026,10,  4), End = new(2026,10, 14), Busses = 2, Travelers = 16, RequiredLeaders = 2, BookingStatus = BookingStatus.Huidig },
        new() { Name = "IJsland",           Start = new(2026,10, 18), End = new(2026,10, 28), Busses = 1, Travelers =  8, RequiredLeaders = 1, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Schotland",         Start = new(2026,11,  1), End = new(2026,11, 11), Busses = 1, Travelers =  9, RequiredLeaders = 1, BookingStatus = BookingStatus.Huidig },
        new() { Name = "Finland",           Start = new(2026,11, 15), End = new(2026,11, 25), Busses = 1, Travelers =  8, RequiredLeaders = 1, BookingStatus = BookingStatus.Huidig },

        // ── Edge cases ────────────────────────────────────────────────────────
        // Loopt parallel aan Italië Zomer + Griekenland Zomer, vereist 3 leiders.
        new() { Name = "Egypte Peak",       Start = new(2026, 7,  1), End = new(2026, 7, 28), Busses = 3, Travelers = 30, RequiredLeaders = 3, BookingStatus = BookingStatus.Huidig },
        // Eindigt 2 jan 2027 — geen leider heeft beschikbaarheid t/m die datum.
        new() { Name = "Noorwegen Kerst",   Start = new(2026,12, 22), End = new(2027, 1,  2), Busses = 2, Travelers = 14, RequiredLeaders = 2, BookingStatus = BookingStatus.Huidig },
    ];

    // ─── PLANNINGSRONDE PERIODE 1 2026 ───────────────────────────────────────────

    private async Task SeedPlanningRoundAsync(Dictionary<string, TravelLeader> leaders)
    {
        // De 8 reizen die binnen Periode 1 (1 jan – 30 jun 2026) vallen.
        var periodOneJourneys = await context.Journey
            .Where(j => j.Start.Year == 2026 && j.Start <= new DateOnly(2026, 6, 30))
            .ToListAsync();

        var journeyIds = periodOneJourneys.ToDictionary(j => j.Name, j => j.Id);

        // Shorthand: bouw een preference-lijst op basis van een naam→rank map.
        List<PlanningRoundPreference> Prefs(Dictionary<string, int> map) =>
            map
                .Where(kvp => journeyIds.ContainsKey(kvp.Key))
                .Select(kvp => new PlanningRoundPreference { JourneyId = journeyIds[kvp.Key], Rank = kvp.Value })
                .ToList();

        // Shorthand: bouw een participation aan.
        // Leaders zonder reizen in het datumvenster krijgen Status=Pending.
        PlanningRoundParticipation Part(string key, Dictionary<string, int> prefMap)
        {
            var prefs = Prefs(prefMap);
            var leader = leaders[key];
            return new PlanningRoundParticipation
            {
                TravelLeaderId = leader.Id,
                MinTrips = leader.MinTrips,
                MaxTrips = leader.MaxTrips,
                Note = leader.Note,
                Status = prefs.Count > 0 ? ParticipationStatus.Submitted : ParticipationStatus.Pending,
                SubmittedAt = prefs.Count > 0 ? new DateTime(2026, 2, 10, 10, 0, 0) : null,
                Preferences = prefs
            };
        }

        // ── Voorkeuren per reisleider ────────────────────────────────────────
        // Rank 1–3 = voorkeur (hoogste prioriteit), Rank 0 = beschikbaar zonder voorkeur.
        // Alleen reizen binnen het beschikbaarheidsvenster van de leider worden opgegeven.
        // Leaders die geen reizen hebben in Periode 1 (Gerda jul-sep, Laura jul, Quinten)
        // leveren een lege map in en krijgen automatisch Status=Pending.

        var participations = new List<PlanningRoundParticipation>
        {
            // ── Kerngroep — breed beschikbaar ───────────────────────────────────
            Part("jan", new()
            {
                ["Italië Voorjaar"]   = 1,
                ["Griekenland Lente"] = 2,
                ["Spanje Voorjaar"]   = 3,
                ["Portugal"]          = 0,
                ["Marokko"]           = 0,
                ["Kroatië Lente"]     = 0,
                ["Turkije"]           = 0,
                ["Spanje Zomer"]      = 0,
            }),
            Part("maria", new()
            {
                ["Italië Voorjaar"]   = 1,
                ["Portugal"]          = 2,
                ["Griekenland Lente"] = 3,
                ["Spanje Voorjaar"]   = 0,
                ["Marokko"]           = 0,
                ["Kroatië Lente"]     = 0,
                ["Spanje Zomer"]      = 0,
            }),
            Part("thomas", new()
            {
                ["Marokko"]           = 1,
                ["Italië Voorjaar"]   = 2,
                ["Turkije"]           = 3,
                ["Spanje Voorjaar"]   = 0,
                ["Portugal"]          = 0,
                ["Spanje Zomer"]      = 0,
            }),
            Part("daan", new()
            {
                ["Spanje Voorjaar"]   = 1,
                ["Kroatië Lente"]     = 2,
                ["Portugal"]          = 3,
                ["Marokko"]           = 0,
                ["Griekenland Lente"] = 0,
                ["Turkije"]           = 0,
                ["Spanje Zomer"]      = 0,
            }),
            Part("fleur", new()
            {
                ["Griekenland Lente"] = 1,
                ["Italië Voorjaar"]   = 2,
                ["Kroatië Lente"]     = 3,
                ["Spanje Voorjaar"]   = 0,
                ["Portugal"]          = 0,
                ["Marokko"]           = 0,
                ["Turkije"]           = 0,
            }),

            // ── Lentegasten ─────────────────────────────────────────────────────
            Part("anna", new()       // mrt–jun
            {
                ["Spanje Voorjaar"]   = 1,
                ["Portugal"]          = 2,
                ["Marokko"]           = 3,
                ["Italië Voorjaar"]   = 0,
                ["Turkije"]           = 0,
            }),
            Part("clara", new()      // mrt–mei
            {
                ["Portugal"]          = 1,
                ["Marokko"]           = 2,
                ["Spanje Voorjaar"]   = 3,
                ["Italië Voorjaar"]   = 0,
                ["Griekenland Lente"] = 0,
                ["Kroatië Lente"]     = 0,
            }),
            Part("david", new()      // mrt–jun, historisch op max
            {
                ["Marokko"]           = 1,
                ["Griekenland Lente"] = 2,
                ["Spanje Voorjaar"]   = 3,
                ["Portugal"]          = 0,
                ["Italië Voorjaar"]   = 0,
            }),

            // ── Zomerspecialisten ────────────────────────────────────────────────
            Part("frank", new()      // jun–sep
            {
                ["Turkije"]           = 1,
                ["Spanje Zomer"]      = 2,
            }),
            Part("gerda", new() {}), // jul–sep → geen reizen in Periode 1 → Pending
            Part("hans", new()       // jun–sep
            {
                ["Turkije"]           = 1,
                ["Spanje Zomer"]      = 2,
            }),

            // ── Smal venster ─────────────────────────────────────────────────────
            Part("karel", new()      // 5–25 apr → alleen Marokko (5-15 apr)
            {
                ["Marokko"]           = 1,
            }),
            Part("laura", new() {}), // 1–21 jul → geen reizen in Periode 1 → Pending

            // ── Bijzondere constraints ────────────────────────────────────────────
            Part("sebastiaan", new() // MaxTrips=1 → wil precies 1 reis
            {
                ["Italië Voorjaar"]   = 1,
            }),
            Part("vera", new()       // MinTrips=2, AmountOfTrips=0
            {
                ["Portugal"]          = 1,
                ["Griekenland Lente"] = 2,
                ["Spanje Voorjaar"]   = 3,
                ["Marokko"]           = 0,
                ["Italië Voorjaar"]   = 0,
            }),

            // ── Geen beschikbaarheid ─────────────────────────────────────────────
            Part("quinten", new() {}), // Actief maar geen AvailabilityPeriods → Pending
        };

        var round = new PlanningRound
        {
            Name = "Periode 1 2026",
            Year = 2026,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            PreferenceDeadline = new DateTime(2026, 2, 15, 23, 59, 59),
            PublicationDeadline = new DateTime(2026, 3, 1, 23, 59, 59),
            Participations = participations,
            Journeys = periodOneJourneys
        };

        context.PlanningRounds.Add(round);
        await context.SaveChangesAsync();

        var submitted = participations.Count(p => p.Status == ParticipationStatus.Submitted);
        logger.LogInformation(
            "Planningsronde '{Name}' aangemaakt — {Submitted}/{Total} voorkeuren ingediend, {Journeys} reizen.",
            round.Name, submitted, participations.Count, journeyIds.Count);
    }
}
