using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dockerizer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedImageTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeneratedImageTag",
                table: "jobs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneratedImageTag",
                table: "jobs");
        }
    }
}
