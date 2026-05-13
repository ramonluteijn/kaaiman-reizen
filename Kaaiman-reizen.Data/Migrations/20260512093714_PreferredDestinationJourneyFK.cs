using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class PreferredDestinationJourneyFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Destination",
                table: "PreferredDestinations");

            migrationBuilder.AddColumn<int>(
                name: "JourneyId",
                table: "PreferredDestinations",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 1,
                column: "JourneyId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 2,
                column: "JourneyId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 3,
                column: "JourneyId",
                value: 5);

            migrationBuilder.UpdateData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 4,
                column: "JourneyId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 5,
                column: "JourneyId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 6,
                column: "JourneyId",
                value: 4);

            migrationBuilder.CreateIndex(
                name: "IX_PreferredDestinations_JourneyId",
                table: "PreferredDestinations",
                column: "JourneyId");

            migrationBuilder.AddForeignKey(
                name: "FK_PreferredDestinations_Journey_JourneyId",
                table: "PreferredDestinations",
                column: "JourneyId",
                principalTable: "Journey",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreferredDestinations_Journey_JourneyId",
                table: "PreferredDestinations");

            migrationBuilder.DropIndex(
                name: "IX_PreferredDestinations_JourneyId",
                table: "PreferredDestinations");

            migrationBuilder.DropColumn(
                name: "JourneyId",
                table: "PreferredDestinations");

            migrationBuilder.AddColumn<string>(
                name: "Destination",
                table: "PreferredDestinations",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 1,
                column: "Destination",
                value: "Italië");

            migrationBuilder.UpdateData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 2,
                column: "Destination",
                value: "Griekenland");

            migrationBuilder.UpdateData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 3,
                column: "Destination",
                value: "Kroatië");

            migrationBuilder.UpdateData(
                table: "PreferredDestinations",
                keyColumn: "Id",
                keyValue: 4,
                column: "Destination",
                value: "Spanje");

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
    }
}
