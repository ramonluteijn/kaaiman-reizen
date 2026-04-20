using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data.Seeders;

public static class DevDataSeeder
{
    private const string DevSeedMarker = "[DEV]";

    public static async Task SeedAsync(MainContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (await context.TravelLeader.AnyAsync(t => t.Note.StartsWith(DevSeedMarker)))
            return;

        var leaders = CreateTravelLeaders();
        context.TravelLeader.AddRange(leaders);

        var journeys = CreateJourneys();
        context.Journey.AddRange(journeys);

        await context.SaveChangesAsync();
    }

    private static List<TravelLeader> CreateTravelLeaders() =>
    [
        new TravelLeader
        {
            Name = "Sophie van den Berg",
            PhoneNumber = "06-11223344",
            AmountOfTrips = 5,
            MinTrips = 1,
            MaxTrips = 6,
            IsActive = true,
            Note = $"{DevSeedMarker} weinig reizen, laag maximum",
            PreferredDestinations =
            [
                new PreferredDestination { Rank = 1, Destination = "Portugal" },
                new PreferredDestination { Rank = 2, Destination = "Spanje" },
                new PreferredDestination { Rank = 3, Destination = "Marokko" }
            ],
            AvailabilityPeriods =
            [
                new AvailabilityPeriod { Start = new DateOnly(2026, 1, 1), End = new DateOnly(2026, 6, 30) }
            ]
        },
        new TravelLeader
        {
            Name = "Pieter Bakker",
            PhoneNumber = "06-22334455",
            AmountOfTrips = 20,
            MinTrips = 4,
            MaxTrips = 20,
            IsActive = true,
            Note = $"{DevSeedMarker} ervaren reisleider, hoog maximum",
            PreferredDestinations =
            [
                new PreferredDestination { Rank = 1, Destination = "Turkije" },
                new PreferredDestination { Rank = 2, Destination = "Egypte" }
            ],
            AvailabilityPeriods =
            [
                new AvailabilityPeriod { Start = new DateOnly(2026, 3, 1), End = new DateOnly(2026, 12, 31) }
            ]
        },
        new TravelLeader
        {
            Name = "Anke Smit",
            PhoneNumber = "06-33445566",
            AmountOfTrips = 2,
            MinTrips = 2,
            MaxTrips = 4,
            IsActive = true,
            Note = $"{DevSeedMarker} bijna op minimum, nauwe marge",
            PreferredDestinations =
            [
                new PreferredDestination { Rank = 1, Destination = "Duitsland" },
                new PreferredDestination { Rank = 2, Destination = "Zwitserland" },
                new PreferredDestination { Rank = 3, Destination = "Oostenrijk" }
            ],
            // Beschikbaarheid loopt af vóór zomerreizen — test voor validatieconflicten
            AvailabilityPeriods =
            [
                new AvailabilityPeriod { Start = new DateOnly(2026, 2, 1), End = new DateOnly(2026, 4, 15) }
            ]
        },
        new TravelLeader
        {
            Name = "Dirk Visser",
            PhoneNumber = "06-44556677",
            AmountOfTrips = 9,
            MinTrips = 3,
            MaxTrips = 12,
            IsActive = false,
            Note = $"{DevSeedMarker} inactief, mag niet ingepland worden",
            PreferredDestinations =
            [
                new PreferredDestination { Rank = 1, Destination = "Italië" },
                new PreferredDestination { Rank = 2, Destination = "Griekenland" }
            ],
            // Heeft periode maar is inactief — test IsActive-validatie
            AvailabilityPeriods =
            [
                new AvailabilityPeriod { Start = new DateOnly(2026, 5, 1), End = new DateOnly(2026, 9, 30) }
            ]
        },
        new TravelLeader
        {
            Name = "Lotte Hendriks",
            PhoneNumber = "06-55667788",
            AmountOfTrips = 0,
            MinTrips = 1,
            MaxTrips = 3,
            IsActive = true,
            Note = $"{DevSeedMarker} geen beschikbaarheid opgegeven",
            PreferredDestinations =
            [
                new PreferredDestination { Rank = 1, Destination = "Noorwegen" },
                new PreferredDestination { Rank = 2, Destination = "IJsland" },
                new PreferredDestination { Rank = 3, Destination = "Schotland" }
            ]
            // Geen AvailabilityPeriods — test gedrag zonder beschikbaarheid
        }
    ];

    private static List<Journey> CreateJourneys() =>
    [
        new Journey
        {
            Name = "Portugal (DEV)",
            Start = new DateOnly(2026, 1, 15),
            End = new DateOnly(2026, 1, 25),
            Busses = 1,
            Travelers = 12,
            RequiredLeaders = 1,
            BookingStatus = BookingStatus.Geweest
        },
        new Journey
        {
            Name = "Marokko (DEV)",
            Start = new DateOnly(2026, 2, 10),
            End = new DateOnly(2026, 2, 22),
            Busses = 2,
            Travelers = 20,
            RequiredLeaders = 2,
            BookingStatus = BookingStatus.Geanuleerd
        },
        new Journey
        {
            Name = "Turkije (DEV)",
            Start = new DateOnly(2026, 4, 18),
            End = new DateOnly(2026, 4, 28),
            Busses = 1,
            Travelers = 8,
            RequiredLeaders = 1,
            BookingStatus = BookingStatus.Bezig
        },
        new Journey
        {
            Name = "Zwitserland (DEV)",
            Start = new DateOnly(2026, 5, 2),
            End = new DateOnly(2026, 5, 12),
            Busses = 2,
            Travelers = 18,
            RequiredLeaders = 2,
            BookingStatus = BookingStatus.Bezig
        },
        new Journey
        {
            Name = "Egypte (DEV)",
            Start = new DateOnly(2026, 6, 5),
            End = new DateOnly(2026, 6, 18),
            Busses = 3,
            Travelers = 30,
            RequiredLeaders = 3,
            BookingStatus = BookingStatus.Bezig
        },
        new Journey
        {
            Name = "Duitsland (DEV)",
            Start = new DateOnly(2026, 7, 20),
            End = new DateOnly(2026, 7, 30),
            Busses = 1,
            Travelers = 9,
            RequiredLeaders = 1,
            BookingStatus = BookingStatus.Geweest
        },
        new Journey
        {
            Name = "Noorwegen (DEV)",
            Start = new DateOnly(2026, 9, 1),
            End = new DateOnly(2026, 9, 14),
            Busses = 2,
            Travelers = 16,
            RequiredLeaders = 2,
            BookingStatus = BookingStatus.Bezig
        },
        new Journey
        {
            Name = "IJsland (DEV)",
            Start = new DateOnly(2026, 11, 3),
            End = new DateOnly(2026, 11, 13),
            Busses = 1,
            Travelers = 10,
            RequiredLeaders = 1,
            BookingStatus = BookingStatus.Geanuleerd
        }
    ];
}