using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeOps.Domain.Migrations
{
    /// <inheritdoc />
    public partial class RemovedKnowledgeDocumentsAndPromotedPortalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeDocuments");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PortalDocuments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "PortalDocuments",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "PortalDocuments");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "PortalDocuments",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.CreateTable(
                name: "KnowledgeDocuments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Department = table.Column<string>(type: "TEXT", nullable: false),
                    LastReviewedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Owner = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDocuments", x => x.Id);
                });
        }
    }
}
