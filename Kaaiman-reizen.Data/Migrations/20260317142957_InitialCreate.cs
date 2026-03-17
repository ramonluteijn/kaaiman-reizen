using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TravelLeader",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AmountOfTrips = table.Column<int>(type: "int", nullable: false),
                    WhenAvailable = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelLeader", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PreferredDestinations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TravelLeaderId = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    Destination = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferredDestinations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreferredDestinations_TravelLeader_TravelLeaderId",
                        column: x => x.TravelLeaderId,
                        principalTable: "TravelLeader",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "TravelLeader",
                columns: new[] { "Id", "AmountOfTrips", "Name", "PhoneNumber", "WhenAvailable" },
                values: new object[,]
                {
                    { 1, 8, "Jan de Vries", "06-12345678", "April–juni 2025" },
                    { 2, 12, "Maria Jansen", "06-87654321", "Hele jaar" }
                });

            migrationBuilder.InsertData(
                table: "PreferredDestinations",
                columns: new[] { "Id", "Destination", "Rank", "TravelLeaderId" },
                values: new object[,]
                {
                    { 1, "Italië", 1, 1 },
                    { 2, "Griekenland", 2, 1 },
                    { 3, "Kroatië", 3, 1 },
                    { 4, "Spanje", 1, 2 },
                    { 5, "Portugal", 2, 2 },
                    { 6, "Marokko", 3, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreferredDestinations_TravelLeaderId",
                table: "PreferredDestinations",
                column: "TravelLeaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreferredDestinations");

            migrationBuilder.DropTable(
                name: "TravelLeader");
        }
    }
}
