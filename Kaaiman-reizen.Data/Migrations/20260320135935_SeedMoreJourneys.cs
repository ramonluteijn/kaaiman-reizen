using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedMoreJourneys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Journey",
                columns: new[] { "Id", "Busses", "Country", "End", "Start", "Travelers" },
                values: new object[,]
                {
                    { 2, 2, "Spanje", new DateTime(2026, 3, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 15 },
                    { 3, 1, "Oostenrijk", new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 8 },
                    { 4, 3, "Griekenland", new DateTime(2026, 4, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 25 },
                    { 5, 2, "Kroatië", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), 12 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
