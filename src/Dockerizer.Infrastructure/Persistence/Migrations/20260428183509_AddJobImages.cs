using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dockerizer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentImageId",
                table: "jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "job_images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DetectedStack = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ImageTag = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ImageId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SourceCommitSha = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ContainerPort = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    BuiltAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_images_jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "image_artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobImageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_image_artifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_image_artifacts_job_images_JobImageId",
                        column: x => x.JobImageId,
                        principalTable: "job_images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_jobs_CurrentImageId",
                table: "jobs",
                column: "CurrentImageId");

            migrationBuilder.CreateIndex(
                name: "IX_image_artifacts_JobImageId",
                table: "image_artifacts",
                column: "JobImageId");

            migrationBuilder.CreateIndex(
                name: "IX_image_artifacts_JobImageId_Kind_Name",
                table: "image_artifacts",
                columns: new[] { "JobImageId", "Kind", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_images_CreatedAtUtc",
                table: "job_images",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_job_images_JobId",
                table: "job_images",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_job_images_JobId_CreatedAtUtc",
                table: "job_images",
                columns: new[] { "JobId", "CreatedAtUtc" });

            migrationBuilder.Sql(
                """
                INSERT INTO job_images (
                    "Id",
                    "JobId",
                    "Status",
                    "DetectedStack",
                    "ImageTag",
                    "ImageId",
                    "ContainerPort",
                    "ErrorMessage",
                    "CreatedAtUtc",
                    "StartedAtUtc",
                    "BuiltAtUtc",
                    "CompletedAtUtc")
                SELECT
                    j."Id",
                    j."Id",
                    j."Status",
                    j."DetectedStack",
                    j."GeneratedImageTag",
                    j."ImageId",
                    j."ContainerPort",
                    j."ErrorMessage",
                    j."CreatedAtUtc",
                    j."StartedAtUtc",
                    CASE
                        WHEN j."GeneratedImageTag" IS NOT NULL OR j."ImageId" IS NOT NULL
                            THEN COALESCE(j."DeployedAtUtc", j."CompletedAtUtc", j."StartedAtUtc", j."CreatedAtUtc")
                        ELSE NULL
                    END,
                    j."CompletedAtUtc"
                FROM jobs j
                WHERE
                    j."GeneratedImageTag" IS NOT NULL
                    OR j."ImageId" IS NOT NULL
                    OR j."DetectedStack" IS NOT NULL
                    OR EXISTS (
                        SELECT 1
                        FROM job_artifacts ja
                        WHERE ja."JobId" = j."Id");
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO image_artifacts (
                    "Id",
                    "JobImageId",
                    "Kind",
                    "Name",
                    "Content",
                    "UpdatedAtUtc")
                SELECT
                    ja."Id",
                    ja."JobId",
                    ja."Kind",
                    ja."Name",
                    ja."Content",
                    ja."UpdatedAtUtc"
                FROM job_artifacts ja
                WHERE EXISTS (
                    SELECT 1
                    FROM job_images ji
                    WHERE ji."Id" = ja."JobId");
                """);

            migrationBuilder.Sql(
                """
                UPDATE jobs
                SET "CurrentImageId" = "Id"
                WHERE "GeneratedImageTag" IS NOT NULL OR "ImageId" IS NOT NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_jobs_job_images_CurrentImageId",
                table: "jobs",
                column: "CurrentImageId",
                principalTable: "job_images",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_jobs_job_images_CurrentImageId",
                table: "jobs");

            migrationBuilder.DropTable(
                name: "image_artifacts");

            migrationBuilder.DropTable(
                name: "job_images");

            migrationBuilder.DropIndex(
                name: "IX_jobs_CurrentImageId",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "CurrentImageId",
                table: "jobs");
        }
    }
}
