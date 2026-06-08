using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningRound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanningRoundId",
                table: "PlanningVersions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlanningRounds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreferenceDeadline = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PublicationDeadline = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningRounds", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlanningRoundParticipations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlanningRoundId = table.Column<int>(type: "int", nullable: false),
                    TravelLeaderId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningRoundParticipations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningRoundParticipations_PlanningRounds_PlanningRoundId",
                        column: x => x.PlanningRoundId,
                        principalTable: "PlanningRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanningRoundParticipations_TravelLeader_TravelLeaderId",
                        column: x => x.TravelLeaderId,
                        principalTable: "TravelLeader",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlanningRoundPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlanningRoundParticipationId = table.Column<int>(type: "int", nullable: false),
                    JourneyId = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningRoundPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningRoundPreferences_Journey_JourneyId",
                        column: x => x.JourneyId,
                        principalTable: "Journey",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanningRoundPreferences_PlanningRoundParticipations_Plannin~",
                        column: x => x.PlanningRoundParticipationId,
                        principalTable: "PlanningRoundParticipations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningVersions_PlanningRoundId",
                table: "PlanningVersions",
                column: "PlanningRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningRoundParticipations_PlanningRoundId_TravelLeaderId",
                table: "PlanningRoundParticipations",
                columns: new[] { "PlanningRoundId", "TravelLeaderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanningRoundParticipations_TravelLeaderId",
                table: "PlanningRoundParticipations",
                column: "TravelLeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningRoundPreferences_JourneyId",
                table: "PlanningRoundPreferences",
                column: "JourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningRoundPreferences_PlanningRoundParticipationId_Journe~",
                table: "PlanningRoundPreferences",
                columns: new[] { "PlanningRoundParticipationId", "JourneyId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanningVersions_PlanningRounds_PlanningRoundId",
                table: "PlanningVersions",
                column: "PlanningRoundId",
                principalTable: "PlanningRounds",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanningVersions_PlanningRounds_PlanningRoundId",
                table: "PlanningVersions");

            migrationBuilder.DropTable(
                name: "PlanningRoundPreferences");

            migrationBuilder.DropTable(
                name: "PlanningRoundParticipations");

            migrationBuilder.DropTable(
                name: "PlanningRounds");

            migrationBuilder.DropIndex(
                name: "IX_PlanningVersions_PlanningRoundId",
                table: "PlanningVersions");

            migrationBuilder.DropColumn(
                name: "PlanningRoundId",
                table: "PlanningVersions");
        }
    }
}
