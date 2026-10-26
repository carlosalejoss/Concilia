using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Concilia.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIterations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 109: only adds. The "Iteración 1" of the projects that
            // already exist is created at start-up (IterationEndpoints.CreateFirstForExistingAsync).
            migrationBuilder.AddColumn<int>(
                name: "IterationId",
                table: "ProjectFiles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Iterations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Iterations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Iterations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_IterationId",
                table: "ProjectFiles",
                column: "IterationId");

            migrationBuilder.CreateIndex(
                name: "IX_Iterations_ProjectId_Number",
                table: "Iterations",
                columns: new[] { "ProjectId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Iterations_ProjectId_Open",
                table: "Iterations",
                column: "ProjectId",
                unique: true,
                filter: "\"Status\" = 'Open'");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFiles_Iterations_IterationId",
                table: "ProjectFiles",
                column: "IterationId",
                principalTable: "Iterations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFiles_Iterations_IterationId",
                table: "ProjectFiles");

            migrationBuilder.DropTable(
                name: "Iterations");

            migrationBuilder.DropIndex(
                name: "IX_ProjectFiles_IterationId",
                table: "ProjectFiles");

            migrationBuilder.DropColumn(
                name: "IterationId",
                table: "ProjectFiles");
        }
    }
}
