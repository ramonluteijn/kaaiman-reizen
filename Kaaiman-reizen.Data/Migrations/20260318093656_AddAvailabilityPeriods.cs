using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilityPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhenAvailable",
                table: "TravelLeader");

            migrationBuilder.CreateTable(
                name: "AvailabilityPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TravelLeaderId = table.Column<int>(type: "int", nullable: false),
                    Start = table.Column<DateOnly>(type: "date", nullable: false),
                    End = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvailabilityPeriods_TravelLeader_TravelLeaderId",
                        column: x => x.TravelLeaderId,
                        principalTable: "TravelLeader",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "AvailabilityPeriods",
                columns: new[] { "Id", "End", "Start", "TravelLeaderId" },
                values: new object[,]
                {
                    { 1, new DateOnly(2025, 5, 3), new DateOnly(2025, 4, 29), 1 },
                    { 2, new DateOnly(2025, 6, 14), new DateOnly(2025, 5, 30), 1 },
                    { 3, new DateOnly(2025, 12, 31), new DateOnly(2025, 1, 1), 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityPeriods_TravelLeaderId",
                table: "AvailabilityPeriods",
                column: "TravelLeaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailabilityPeriods");

            migrationBuilder.AddColumn<string>(
                name: "WhenAvailable",
                table: "TravelLeader",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "TravelLeader",
                keyColumn: "Id",
                keyValue: 1,
                column: "WhenAvailable",
                value: "April–juni 2025");

            migrationBuilder.UpdateData(
                table: "TravelLeader",
                keyColumn: "Id",
                keyValue: 2,
                column: "WhenAvailable",
                value: "Hele jaar");
        }
    }
}
