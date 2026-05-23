using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHardcodedSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AvailabilityPeriods",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "AvailabilityPeriods",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "JourneyTravelLeaders",
                keyColumns: new[] { "JourneysId", "TravelLeadersId" },
                keyValues: new object[] { 2, 1 });

            migrationBuilder.DeleteData(
                table: "JourneyTravelLeaders",
                keyColumns: new[] { "JourneysId", "TravelLeadersId" },
                keyValues: new object[] { 3, 1 });

            migrationBuilder.DeleteData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TravelLeader",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TravelLeader",
                keyColumn: "Id",
                keyValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Journey",
                columns: new[] { "Id", "BookingStatus", "Busses", "End", "Name", "RequiredLeaders", "Start", "Travelers" },
                values: new object[,]
                {
                    { 1, 0, 1, new DateOnly(2026, 7, 14), "Italië", 1, new DateOnly(2026, 7, 1), 10 },
                    { 2, 0, 2, new DateOnly(2026, 3, 20), "Spanje", 1, new DateOnly(2026, 3, 10), 15 },
                    { 3, 1, 1, new DateOnly(2026, 4, 3), "Oostenrijk", 1, new DateOnly(2026, 3, 25), 8 },
                    { 4, 2, 3, new DateOnly(2026, 4, 15), "Griekenland", 2, new DateOnly(2026, 4, 5), 25 },
                    { 5, 2, 2, new DateOnly(2026, 5, 10), "Kroatië", 1, new DateOnly(2026, 4, 28), 12 }
                });

            migrationBuilder.InsertData(
                table: "TravelLeader",
                columns: new[] { "Id", "AmountOfTrips", "IsActive", "MaxTrips", "MinTrips", "Name", "Note", "PhoneNumber" },
                values: new object[,]
                {
                    { 1, 8, true, 10, 2, "Jan de Vries", "", "06-12345678" },
                    { 2, 12, true, 15, 3, "Maria Jansen", "", "06-87654321" }
                });

            migrationBuilder.InsertData(
                table: "AvailabilityPeriods",
                columns: new[] { "Id", "End", "Start", "TravelLeaderId" },
                values: new object[,]
                {
                    { 1, new DateOnly(2026, 7, 31), new DateOnly(2026, 4, 1), 1 },
                    { 2, new DateOnly(2026, 5, 31), new DateOnly(2026, 3, 1), 2 }
                });

            migrationBuilder.InsertData(
                table: "JourneyTravelLeaders",
                columns: new[] { "JourneysId", "TravelLeadersId" },
                values: new object[,]
                {
                    { 2, 1 },
                    { 3, 1 }
                });

            migrationBuilder.InsertData(
                table: "PreferredDestinations",
                columns: new[] { "Id", "Destination", "Rank", "TravelLeaderId" },
                values: new object[,]
                {
                    { 1, "Italië", 1, 1 },
                    { 2, "Griekenland", 2, 1 },
                    { 3, "Kroatië", 3, 1 },
                    { 4, "Spanje", 1, 2 },
                    { 5, "Oostenrijk", 2, 2 },
                    { 6, "Griekenland", 3, 2 }
                });
        }
    }
}
