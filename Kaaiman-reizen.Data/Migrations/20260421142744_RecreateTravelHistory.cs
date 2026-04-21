using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class RecreateTravelHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JourneyTravelLeader");

            migrationBuilder.CreateTable(
                name: "JourneyTravelLeaders",
                columns: table => new
                {
                    JourneysId = table.Column<int>(type: "int", nullable: false),
                    TravelLeadersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JourneyTravelLeaders", x => new { x.JourneysId, x.TravelLeadersId });
                    table.ForeignKey(
                        name: "FK_JourneyTravelLeaders_Journey_JourneysId",
                        column: x => x.JourneysId,
                        principalTable: "Journey",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JourneyTravelLeaders_TravelLeader_TravelLeadersId",
                        column: x => x.TravelLeadersId,
                        principalTable: "TravelLeader",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "JourneyTravelLeaders",
                columns: new[] { "JourneysId", "TravelLeadersId" },
                values: new object[,]
                {
                    { 2, 1 },
                    { 3, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_JourneyTravelLeaders_TravelLeadersId",
                table: "JourneyTravelLeaders",
                column: "TravelLeadersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JourneyTravelLeaders");

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

            migrationBuilder.CreateIndex(
                name: "IX_JourneyTravelLeader_TravelLeadersId",
                table: "JourneyTravelLeader",
                column: "TravelLeadersId");
        }
    }
}
