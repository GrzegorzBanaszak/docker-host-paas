using Dockerizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dockerizer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DockerizerDbContext))]
    [Migration("20260503110000_AddPublicAccessEnabled")]
    public partial class AddPublicAccessEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PublicAccessEnabled",
                table: "jobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE jobs
                SET "PublicAccessEnabled" = TRUE
                WHERE "PublicHostname" IS NOT NULL
                   OR "RouteStatus" = 'reverse-proxy-configured'
                """);

            migrationBuilder.CreateIndex(
                name: "IX_jobs_PublicAccessEnabled",
                table: "jobs",
                column: "PublicAccessEnabled");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_jobs_PublicAccessEnabled",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "PublicAccessEnabled",
                table: "jobs");
        }
    }
}
