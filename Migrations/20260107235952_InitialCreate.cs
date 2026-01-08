using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SLA_API_AIChatBot_Poc.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConversationsContext",
                columns: table => new
                {
                    ConversationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequiresEscalation = table.Column<bool>(type: "bit", nullable: false),
                    MessagesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationsContext", x => x.ConversationId);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsHelpful = table.Column<bool>(type: "bit", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationsContext_StartedAt",
                table: "ConversationsContext",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationsContext_UserId",
                table: "ConversationsContext",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationsContext");

            migrationBuilder.DropTable(
                name: "Feedbacks");
        }
    }
}
