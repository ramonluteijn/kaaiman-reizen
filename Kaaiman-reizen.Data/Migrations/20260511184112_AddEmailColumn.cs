using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "TravelLeader",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "TravelLeader",
                keyColumn: "Id",
                keyValue: 1,
                column: "Email",
                value: "j.devries@kaaiman.nl");

            migrationBuilder.UpdateData(
                table: "TravelLeader",
                keyColumn: "Id",
                keyValue: 2,
                column: "Email",
                value: "m.jansen@kaaiman.nl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "TravelLeader");
        }
    }
}
