using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJourneyPlanningRoundFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanningRoundId",
                table: "Journey",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Journey_PlanningRoundId",
                table: "Journey",
                column: "PlanningRoundId");

            migrationBuilder.AddForeignKey(
                name: "FK_Journey_PlanningRounds_PlanningRoundId",
                table: "Journey",
                column: "PlanningRoundId",
                principalTable: "PlanningRounds",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Journey_PlanningRounds_PlanningRoundId",
                table: "Journey");

            migrationBuilder.DropIndex(
                name: "IX_Journey_PlanningRoundId",
                table: "Journey");

            migrationBuilder.DropColumn(
                name: "PlanningRoundId",
                table: "Journey");
        }
    }
}
