using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCustomerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEvalReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvalReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DatasetName = table.Column<string>(type: "text", nullable: false),
                    TotalCases = table.Column<int>(type: "integer", nullable: false),
                    FaithfulnessAvg = table.Column<double>(type: "double precision", nullable: false),
                    AnswerRelevancyAvg = table.Column<double>(type: "double precision", nullable: false),
                    ContextPrecisionAvg = table.Column<double>(type: "double precision", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ItemsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvalReports", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvalReports");
        }
    }
}
