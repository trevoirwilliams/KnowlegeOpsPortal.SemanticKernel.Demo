using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeOps.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddedPortalDocumentContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortalDocumentContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PortalDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RawText = table.Column<string>(type: "TEXT", nullable: false),
                    CharacterCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ExtractionEngine = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ExtractedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalDocumentContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortalDocumentContents_PortalDocuments_PortalDocumentId",
                        column: x => x.PortalDocumentId,
                        principalTable: "PortalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortalDocumentContents_PortalDocumentId",
                table: "PortalDocumentContents",
                column: "PortalDocumentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortalDocumentContents");
        }
    }
}
