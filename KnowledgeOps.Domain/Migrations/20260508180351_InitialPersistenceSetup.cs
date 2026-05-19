using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeOps.Domain.Migrations
{
    /// <inheritdoc />
    public partial class InitialPersistenceSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Department = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedBy = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    BusinessJustification = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Impact = table.Column<int>(type: "INTEGER", nullable: false),
                    Urgency = table.Column<string>(type: "TEXT", nullable: false),
                    SubmittedOnUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RequiredByUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AssignedTo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopilotConversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    ContextType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ContextId = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeDocuments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Department = table.Column<string>(type: "TEXT", nullable: false),
                    Owner = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    LastReviewedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortalDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StoredFilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ProcessingStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ExtractedTextPreview = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessingError = table.Column<string>(type: "TEXT", nullable: true),
                    UploadedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopilotMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConversationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    SequenceNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CopilotMessages_CopilotConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "CopilotConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotConversations_UserId_ContextType_ContextId",
                table: "CopilotConversations",
                columns: new[] { "UserId", "ContextType", "ContextId" });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotMessages_ConversationId_SequenceNumber",
                table: "CopilotMessages",
                columns: new[] { "ConversationId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_PortalDocuments_UserId_ProcessingStatus",
                table: "PortalDocuments",
                columns: new[] { "UserId", "ProcessingStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessRequests");

            migrationBuilder.DropTable(
                name: "CopilotMessages");

            migrationBuilder.DropTable(
                name: "KnowledgeDocuments");

            migrationBuilder.DropTable(
                name: "PortalDocuments");

            migrationBuilder.DropTable(
                name: "CopilotConversations");
        }
    }
}
