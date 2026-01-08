using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APME.Migrations
{
    /// <inheritdoc />
    public partial class AddChatDomains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppChatSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppChatSessions_AppCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AppCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppChatMessages_AppChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AppChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppChatMessages_CreationTime",
                table: "AppChatMessages",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatMessages_IsArchived",
                table: "AppChatMessages",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatMessages_IsArchived_CreationTime",
                table: "AppChatMessages",
                columns: new[] { "IsArchived", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AppChatMessages_SessionId",
                table: "AppChatMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatMessages_SessionId_CreationTime",
                table: "AppChatMessages",
                columns: new[] { "SessionId", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionId_SequenceNumber",
                table: "AppChatMessages",
                columns: new[] { "SessionId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppChatSessions_CustomerId",
                table: "AppChatSessions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatSessions_CustomerId_LastActivityAt",
                table: "AppChatSessions",
                columns: new[] { "CustomerId", "LastActivityAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppChatSessions_CustomerId_Status",
                table: "AppChatSessions",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppChatSessions_LastActivityAt",
                table: "AppChatSessions",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatSessions_Status",
                table: "AppChatSessions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppChatMessages");

            migrationBuilder.DropTable(
                name: "AppChatSessions");
        }
    }
}
