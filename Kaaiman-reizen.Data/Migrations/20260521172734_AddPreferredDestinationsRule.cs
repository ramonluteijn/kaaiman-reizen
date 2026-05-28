using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredDestinationsRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Rule",
                columns: new[] { "Id", "Description", "IsActive", "Key", "Value", "Weight" },
                values: new object[] { 5, "Reisleider krijgt voorkeur voor reizen naar zijn favoriete bestemmingen.", true, "PreferredDestinations", null, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Rule",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
