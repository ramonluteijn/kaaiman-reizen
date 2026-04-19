using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kaaiman_reizen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Key = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rule", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Rule",
                columns: new[] { "Id", "Description", "IsActive", "Key", "Value" },
                values: new object[,]
                {
                    { 1, "Reisleider mag geen overlappende reizen hebben.", true, "NoOverlap", null },
                    { 2, "Minimaal aantal dagen tussen twee reizen.", true, "MinimumGapDays", "3" },
                    { 3, "Minimaal aantal reizen ervaring voor niet-standaard bestemmingen.", true, "RequiredExperience", "3" },
                    { 4, "Controle op minimum/maximum aantal reizen per reisleider.", true, "MinMaxJourneys", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rule_Key",
                table: "Rule",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rule");
        }
    }
}
