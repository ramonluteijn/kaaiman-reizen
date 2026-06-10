using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJourneyReminderSettingsRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Rule",
                columns: new[] { "Id", "Description", "IsActive", "Key", "Value", "Weight" },
                values: new object[,]
                {
                    { 6, "Versturen van reisnotificaties voor aankomende reizen.", true, "JourneyReminderEnabled", "true", 1 },
                    { 7, "Aantal dagen voor vertrek waarop reisnotificaties worden verstuurd (komma-gescheiden).", true, "JourneyReminderDays", "7,3", 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Rule",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Rule",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
