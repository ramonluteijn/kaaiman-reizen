using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Weight",
                table: "Rule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Rule",
                keyColumn: "Id",
                keyValue: 1,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Rule",
                keyColumn: "Id",
                keyValue: 2,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Rule",
                keyColumn: "Id",
                keyValue: 3,
                column: "Weight",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Rule",
                keyColumn: "Id",
                keyValue: 4,
                column: "Weight",
                value: 1);
            
            migrationBuilder.UpdateData(
                table: "Rule",
                keyColumn: "Id",
                keyValue: 5,
                column: "Weight",
                value: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Rule");
        }
    }
}
