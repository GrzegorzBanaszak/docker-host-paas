using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dockerizer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeploymentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContainerId",
                table: "jobs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContainerName",
                table: "jobs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContainerPort",
                table: "jobs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeployedAtUtc",
                table: "jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeploymentUrl",
                table: "jobs",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublishedPort",
                table: "jobs",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContainerId",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "ContainerName",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "ContainerPort",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "DeployedAtUtc",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "DeploymentUrl",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "PublishedPort",
                table: "jobs");
        }
    }
}
