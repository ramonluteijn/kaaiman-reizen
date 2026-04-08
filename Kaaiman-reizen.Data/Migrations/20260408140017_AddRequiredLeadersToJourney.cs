using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequiredLeadersToJourney : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "Start",
                table: "Journey",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "End",
                table: "Journey",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AddColumn<int>(
                name: "RequiredLeaders",
                table: "Journey",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "End", "RequiredLeaders", "Start" },
                values: new object[] { new DateOnly(2026, 7, 14), 1, new DateOnly(2026, 7, 1) });

            migrationBuilder.UpdateData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "End", "RequiredLeaders", "Start" },
                values: new object[] { new DateOnly(2026, 3, 20), 1, new DateOnly(2026, 3, 10) });

            migrationBuilder.UpdateData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "End", "RequiredLeaders", "Start" },
                values: new object[] { new DateOnly(2026, 4, 3), 1, new DateOnly(2026, 3, 25) });

            migrationBuilder.UpdateData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "End", "RequiredLeaders", "Start" },
                values: new object[] { new DateOnly(2026, 4, 15), 2, new DateOnly(2026, 4, 5) });

            migrationBuilder.UpdateData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "End", "RequiredLeaders", "Start" },
                values: new object[] { new DateOnly(2026, 5, 10), 1, new DateOnly(2026, 4, 28) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredLeaders",
                table: "Journey");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Start",
                table: "Journey",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "End",
                table: "Journey",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.UpdateData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "End", "Start" },
                values: new object[] { new DateTime(2026, 7, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "End", "Start" },
                values: new object[] { new DateTime(2026, 3, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "End", "Start" },
                values: new object[] { new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "End", "Start" },
                values: new object[] { new DateTime(2026, 4, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Journey",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "End", "Start" },
                values: new object[] { new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Unspecified) });
        }
    }
}
