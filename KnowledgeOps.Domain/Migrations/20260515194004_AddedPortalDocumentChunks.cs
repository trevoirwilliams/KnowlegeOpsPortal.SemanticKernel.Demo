using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeOps.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddedPortalDocumentChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortalDocumentChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PortalDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChunkIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterStart = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterEnd = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterCount = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedTokenCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    DocumentTags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalDocumentChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortalDocumentChunks_PortalDocuments_PortalDocumentId",
                        column: x => x.PortalDocumentId,
                        principalTable: "PortalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortalDocumentChunks_ContentHash",
                table: "PortalDocumentChunks",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_PortalDocumentChunks_PortalDocumentId_ChunkIndex",
                table: "PortalDocumentChunks",
                columns: new[] { "PortalDocumentId", "ChunkIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortalDocumentChunks");
        }
    }
}
