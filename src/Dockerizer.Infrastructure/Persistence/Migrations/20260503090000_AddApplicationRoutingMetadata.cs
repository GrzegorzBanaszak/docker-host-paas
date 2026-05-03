using Dockerizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dockerizer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DockerizerDbContext))]
    [Migration("20260503090000_AddApplicationRoutingMetadata")]
    public partial class AddApplicationRoutingMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DnsRecordId",
                table: "jobs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicHostname",
                table: "jobs",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteStatus",
                table: "jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_jobs_PublicHostname",
                table: "jobs",
                column: "PublicHostname");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_jobs_PublicHostname",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "DnsRecordId",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "PublicHostname",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "RouteStatus",
                table: "jobs");
        }
    }
}
