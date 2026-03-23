using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateJourneys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Country",
                table: "Journey",
                newName: "Name");

            migrationBuilder.AddColumn<int>(
                name: "BookingStatus",
                table: "Journey",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 1,
                column: "BookingStatus",
                value: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingStatus",
                table: "Journey");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Journey",
                newName: "Country");
        }
    }
}
