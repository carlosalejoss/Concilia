using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReqArbiter.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRunCatalogFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaudeCodeVersion",
                table: "Runs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LotNumber",
                table: "Runs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LotTotal",
                table: "Runs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptSha256",
                table: "Runs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptVersion",
                table: "Runs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaudeCodeVersion",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "LotTotal",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "PromptSha256",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                table: "Runs");
        }
    }
}
