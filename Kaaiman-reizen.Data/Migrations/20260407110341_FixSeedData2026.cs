using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixSeedData2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AvailabilityPeriods",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "AvailabilityPeriods",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "End", "Start" },
                values: new object[] { new DateOnly(2026, 7, 31), new DateOnly(2026, 4, 1) });

            migrationBuilder.UpdateData(
                table: "AvailabilityPeriods",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "End", "Start", "TravelLeaderId" },
                values: new object[] { new DateOnly(2026, 5, 31), new DateOnly(2026, 3, 1), 2 });

            migrationBuilder.UpdateData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 5,
                column: "Destination",
                value: "Oostenrijk");

            migrationBuilder.UpdateData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 6,
                column: "Destination",
                value: "Griekenland");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AvailabilityPeriods",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "End", "Start" },
                values: new object[] { new DateOnly(2025, 5, 3), new DateOnly(2025, 4, 29) });

            migrationBuilder.UpdateData(
                table: "AvailabilityPeriods",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "End", "Start", "TravelLeaderId" },
                values: new object[] { new DateOnly(2025, 6, 14), new DateOnly(2025, 5, 30), 1 });

            migrationBuilder.InsertData(
                table: "AvailabilityPeriods",
                columns: new[] { "Id", "End", "Start", "TravelLeaderId" },
                values: new object[] { 3, new DateOnly(2025, 12, 31), new DateOnly(2025, 1, 1), 2 });

            migrationBuilder.UpdateData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 5,
                column: "Destination",
                value: "Portugal");

            migrationBuilder.UpdateData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 6,
                column: "Destination",
                value: "Marokko");
        }
    }
}
