using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCustomerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIndustryFaq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IndustryCode",
                table: "tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAt",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IndustryFaqs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndustryCode = table.Column<string>(type: "text", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    Keywords = table.Column<string[]>(type: "text[]", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndustryFaqs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndustryFaqs");

            migrationBuilder.DropColumn(
                name: "IndustryCode",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "tenants");
        }
    }
}
