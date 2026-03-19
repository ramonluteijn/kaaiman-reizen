using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeTripFieldsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "TravelLeader",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "MaxTrips", "MinTrips" },
                values: new object[] { 10, 2 });

            migrationBuilder.UpdateData(
                table: "TravelLeader",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "MaxTrips", "MinTrips" },
                values: new object[] { 15, 3 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
