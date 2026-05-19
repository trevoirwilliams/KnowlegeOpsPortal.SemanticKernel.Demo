using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeOps.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddedSummaryToConversationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SummarizedThroughSequenceNumber",
                table: "CopilotConversations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SummarizedUtc",
                table: "CopilotConversations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "CopilotConversations",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SummarizedThroughSequenceNumber",
                table: "CopilotConversations");

            migrationBuilder.DropColumn(
                name: "SummarizedUtc",
                table: "CopilotConversations");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "CopilotConversations");
        }
    }
}
