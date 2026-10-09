using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReqArbiter.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnswerOrigin",
                table: "Questions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedAnswer",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SuggestedAt",
                table: "Questions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestionBasis",
                table: "Questions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestionNote",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "SuggestionSources",
                table: "Questions",
                type: "text[]",
                nullable: false,
                // The questions already stored get an empty list (without a
                // default, PostgreSQL rejects the new column on existing rows).
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnswerOrigin",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "SuggestedAnswer",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "SuggestedAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "SuggestionBasis",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "SuggestionNote",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "SuggestionSources",
                table: "Questions");
        }
    }
}
