using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelLeaderNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "TravelLeader",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "TravelLeader",
                keyColumn: "Id",
                keyValue: 1,
                column: "Note",
                value: "");

            migrationBuilder.UpdateData(
                table: "TravelLeader",
                keyColumn: "Id",
                keyValue: 2,
                column: "Note",
                value: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "TravelLeader");
        }
    }
}
