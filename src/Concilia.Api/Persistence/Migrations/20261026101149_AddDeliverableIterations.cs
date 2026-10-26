using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concilia.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliverableIterations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 111: only adds. The iterations of the deliverables and
            // versions that already exist are given at start-up
            // (ProjectIterations.RecordExistingCoverageAsync).
            migrationBuilder.AddColumn<List<int>>(
                name: "Iterations",
                table: "DeliverableVersions",
                type: "integer[]",
                nullable: true);

            migrationBuilder.AddColumn<List<int>>(
                name: "Iterations",
                table: "Deliverables",
                type: "integer[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Iterations",
                table: "DeliverableVersions");

            migrationBuilder.DropColumn(
                name: "Iterations",
                table: "Deliverables");
        }
    }
}
