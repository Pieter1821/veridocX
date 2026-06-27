using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeridocX.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSaIdResultAndFingerprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fingerprint",
                table: "jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_valid",
                table: "jobs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "result_json",
                table: "jobs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject",
                table: "jobs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_jobs_fingerprint",
                table: "jobs",
                column: "fingerprint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_jobs_fingerprint",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "fingerprint",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "is_valid",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "result_json",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "subject",
                table: "jobs");
        }
    }
}
