using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveMinMaxNoteToParticipation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxTrips",
                table: "PlanningRoundParticipations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinTrips",
                table: "PlanningRoundParticipations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "PlanningRoundParticipations",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxTrips",
                table: "PlanningRoundParticipations");

            migrationBuilder.DropColumn(
                name: "MinTrips",
                table: "PlanningRoundParticipations");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "PlanningRoundParticipations");
        }
    }
}
