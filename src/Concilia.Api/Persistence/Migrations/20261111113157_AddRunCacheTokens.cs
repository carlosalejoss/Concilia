using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concilia.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRunCacheTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CacheCreationTokens",
                table: "Runs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CacheReadTokens",
                table: "Runs",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CacheCreationTokens",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "CacheReadTokens",
                table: "Runs");
        }
    }
}
