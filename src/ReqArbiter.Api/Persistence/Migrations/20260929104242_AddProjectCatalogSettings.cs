using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReqArbiter.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectCatalogSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Glossary",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Projects",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "es");

            migrationBuilder.AddColumn<string>(
                name: "SystemName",
                table: "Projects",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Existing projects get the same default as new ones: "el sistema <name>"
            // (written by hand; EF Core does not generate data changes).
            migrationBuilder.Sql("""UPDATE "Projects" SET "SystemName" = 'el sistema ' || "Name";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Glossary",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "SystemName",
                table: "Projects");
        }
    }
}
