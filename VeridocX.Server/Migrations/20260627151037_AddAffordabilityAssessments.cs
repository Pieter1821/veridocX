using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeridocX.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddAffordabilityAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "affordability_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    gross_monthly_income = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discretionary_income = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    proposed_instalment = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_affordable = table.Column<bool>(type: "boolean", nullable: false),
                    declared_below_norm = table.Column<bool>(type: "boolean", nullable: false),
                    result_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_affordability_assessments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_affordability_assessments_created_at",
                table: "affordability_assessments",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_affordability_assessments_subject_fingerprint",
                table: "affordability_assessments",
                column: "subject_fingerprint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "affordability_assessments");
        }
    }
}
