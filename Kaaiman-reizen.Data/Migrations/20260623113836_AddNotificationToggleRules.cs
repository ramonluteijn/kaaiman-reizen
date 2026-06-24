using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationToggleRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Rule",
                columns: new[] { "Id", "Description", "IsActive", "Key", "Value", "Weight" },
                values: new object[,]
                {
                    { 8, "Versturen van een welkomstmail met tijdelijk wachtwoord aan een nieuwe reisleider.", true, "WelcomeEmailEnabled", "true", 1 },
                    { 9, "Versturen van een notificatie wanneer een nieuwe planning wordt gepubliceerd.", true, "PlanningPublishedEnabled", "true", 1 },
                    { 10, "Versturen van een notificatie aan betrokken reisleiders wanneer een gepubliceerde planning wijzigt.", true, "PlanningChangedEnabled", "true", 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Rule",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Rule",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Rule",
                keyColumn: "Id",
                keyValue: 10);
        }
    }
}
