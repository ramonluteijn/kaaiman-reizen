using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMinMaxTrips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxTrips",
                table: "TravelLeader",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinTrips",
                table: "TravelLeader",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "TravelLeader",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "MaxTrips", "MinTrips" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "TravelLeader",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "MaxTrips", "MinTrips" },
                values: new object[] { 0, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxTrips",
                table: "TravelLeader");

            migrationBuilder.DropColumn(
                name: "MinTrips",
                table: "TravelLeader");
        }
    }
}
