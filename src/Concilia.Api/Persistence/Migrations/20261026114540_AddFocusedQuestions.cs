using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concilia.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFocusedQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 112: only adds. The questions already stored get an empty
            // history and no proposal; the jobs, no focus (all the iterations).
            migrationBuilder.AddColumn<string>(
                name: "ContradictsQuestion",
                table: "Questions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContradictsRequirement",
                table: "Questions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "History",
                table: "Questions",
                type: "text",
                nullable: false,
                // An empty JSON list, as the conversion of AppDbContext reads it.
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ProposedAction",
                table: "Questions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedReason",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "ProposedSources",
                table: "Questions",
                type: "text[]",
                nullable: false,
                // Without a default, PostgreSQL rejects the new column on existing rows.
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<List<int>>(
                name: "Focus",
                table: "Jobs",
                type: "integer[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContradictsQuestion",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ContradictsRequirement",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "History",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ProposedAction",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ProposedReason",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ProposedSources",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Focus",
                table: "Jobs");
        }
    }
}
