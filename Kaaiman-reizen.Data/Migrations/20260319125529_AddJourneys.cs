using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJourneys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Journey",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Country = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Start = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    End = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Busses = table.Column<int>(type: "int", nullable: false),
                    Travelers = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Journey", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JourneyTravelLeader",
                columns: table => new
                {
                    JourneysId = table.Column<int>(type: "int", nullable: false),
                    TravelLeadersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JourneyTravelLeader", x => new { x.JourneysId, x.TravelLeadersId });
                    table.ForeignKey(
                        name: "FK_JourneyTravelLeader_Journey_JourneysId",
                        column: x => x.JourneysId,
                        principalTable: "Journey",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JourneyTravelLeader_TravelLeader_TravelLeadersId",
                        column: x => x.TravelLeadersId,
                        principalTable: "TravelLeader",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Journey",
                columns: new[] { "Id", "Busses", "Country", "End", "Start", "Travelers" },
                values: new object[,]
                {
                    { 1, 1, "Italië", new DateTime(2026, 7, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 10 },
                    { 2, 2, "Spanje", new DateTime(2026, 3, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 15 },
                    { 3, 1, "Oostenrijk", new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 8 },
                    { 4, 3, "Griekenland", new DateTime(2026, 4, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 25 },
                    { 5, 2, "Kroatië", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), 12 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_JourneyTravelLeader_TravelLeadersId",
                table: "JourneyTravelLeader",
                column: "TravelLeadersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JourneyTravelLeader");

            migrationBuilder.DropTable(
                name: "Journey");
        }
    }
}
