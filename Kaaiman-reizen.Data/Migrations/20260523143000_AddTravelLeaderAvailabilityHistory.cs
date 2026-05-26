using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelLeaderAvailabilityHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TravelLeaderAvailabilityHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TravelLeaderId = table.Column<int>(type: "int", nullable: false),
                    JourneyId = table.Column<int>(type: "int", nullable: true),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PlanningVersionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelLeaderAvailabilityHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelLeaderAvailabilityHistories_Journey_JourneyId",
                        column: x => x.JourneyId,
                        principalTable: "Journey",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TravelLeaderAvailabilityHistories_PlanningVersions_PlanningVersionId",
                        column: x => x.PlanningVersionId,
                        principalTable: "PlanningVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TravelLeaderAvailabilityHistories_TravelLeader_TravelLeaderId",
                        column: x => x.TravelLeaderId,
                        principalTable: "TravelLeader",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TravelLeaderAvailabilityHistories_JourneyId",
                table: "TravelLeaderAvailabilityHistories",
                column: "JourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelLeaderAvailabilityHistories_PlanningVersionId",
                table: "TravelLeaderAvailabilityHistories",
                column: "PlanningVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelLeaderAvailabilityHistories_TravelLeaderId",
                table: "TravelLeaderAvailabilityHistories",
                column: "TravelLeaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TravelLeaderAvailabilityHistories");
        }
    }
}
