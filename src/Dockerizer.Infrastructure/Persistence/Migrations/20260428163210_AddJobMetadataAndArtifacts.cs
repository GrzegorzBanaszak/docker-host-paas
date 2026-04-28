using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dockerizer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobMetadataAndArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageId",
                table: "jobs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "jobs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "job_artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_artifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_artifacts_jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_artifacts_JobId",
                table: "job_artifacts",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_job_artifacts_JobId_Kind_Name",
                table: "job_artifacts",
                columns: new[] { "JobId", "Kind", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_artifacts");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "jobs");
        }
    }
}
