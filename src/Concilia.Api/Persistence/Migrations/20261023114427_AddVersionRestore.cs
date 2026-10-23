using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concilia.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionRestore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Outdated",
                table: "DeliverableVersions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RestoredFrom",
                table: "DeliverableVersions",
                type: "integer",
                nullable: true);

            // The versions recorded before this column (step 101) are all from
            // before the current deliverable: if it is outdated, so are they.
            migrationBuilder.Sql("""
                UPDATE "DeliverableVersions" AS v SET "Outdated" = TRUE
                FROM "Deliverables" AS d
                WHERE d."ProjectId" = v."ProjectId" AND d."Kind" = v."Kind" AND d."Outdated";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Outdated",
                table: "DeliverableVersions");

            migrationBuilder.DropColumn(
                name: "RestoredFrom",
                table: "DeliverableVersions");
        }
    }
}
