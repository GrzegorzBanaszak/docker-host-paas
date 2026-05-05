using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dockerizer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RepositoryUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    DefaultBranch = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DefaultProjectPath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CurrentJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentImageId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublicAccessEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PublicHostname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DeploymentUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RouteStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_projects_job_images_CurrentImageId",
                        column: x => x.CurrentImageId,
                        principalTable: "job_images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_projects_jobs_CurrentJobId",
                        column: x => x.CurrentJobId,
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO projects (
                    "Id",
                    "Name",
                    "RepositoryUrl",
                    "DefaultBranch",
                    "DefaultProjectPath",
                    "CurrentJobId",
                    "CurrentImageId",
                    "PublicAccessEnabled",
                    "PublicHostname",
                    "DeploymentUrl",
                    "RouteStatus",
                    "CreatedAtUtc",
                    "UpdatedAtUtc",
                    "ArchivedAtUtc")
                SELECT
                    "Id",
                    "Name",
                    "RepositoryUrl",
                    "Branch",
                    "ProjectPath",
                    CASE WHEN "Status" = 'Succeeded' THEN "Id" ELSE NULL END,
                    CASE WHEN "Status" = 'Succeeded' THEN "CurrentImageId" ELSE NULL END,
                    "PublicAccessEnabled",
                    "PublicHostname",
                    "DeploymentUrl",
                    "RouteStatus",
                    "CreatedAtUtc",
                    COALESCE("CompletedAtUtc", "StartedAtUtc", "CreatedAtUtc"),
                    NULL
                FROM jobs
                """);

            migrationBuilder.Sql("""
                UPDATE jobs
                SET "ProjectId" = "Id"
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "jobs",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_jobs_ProjectId",
                table: "jobs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_ArchivedAtUtc",
                table: "projects",
                column: "ArchivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_projects_CreatedAtUtc",
                table: "projects",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_projects_CurrentImageId",
                table: "projects",
                column: "CurrentImageId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_CurrentJobId",
                table: "projects",
                column: "CurrentJobId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_PublicAccessEnabled",
                table: "projects",
                column: "PublicAccessEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_projects_PublicHostname",
                table: "projects",
                column: "PublicHostname");

            migrationBuilder.CreateIndex(
                name: "IX_projects_RepositoryUrl_DefaultProjectPath",
                table: "projects",
                columns: new[] { "RepositoryUrl", "DefaultProjectPath" });

            migrationBuilder.AddForeignKey(
                name: "FK_jobs_projects_ProjectId",
                table: "jobs",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_jobs_projects_ProjectId",
                table: "jobs");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropIndex(
                name: "IX_jobs_ProjectId",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "jobs");
        }
    }
}
