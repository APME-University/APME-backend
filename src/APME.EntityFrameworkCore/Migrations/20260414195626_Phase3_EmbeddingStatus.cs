using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APME.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_EmbeddingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "AppProductEmbeddings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptAt",
                table: "AppProductEmbeddings",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "AppProductEmbeddings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SearchableTextHash",
                table: "AppProductEmbeddings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AppProductEmbeddings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProductEmbeddings_Model_Status",
                table: "AppProductEmbeddings",
                columns: new[] { "EmbeddingModel", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEmbeddings_Status",
                table: "AppProductEmbeddings",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductEmbeddings_Model_Status",
                table: "AppProductEmbeddings");

            migrationBuilder.DropIndex(
                name: "IX_ProductEmbeddings_Status",
                table: "AppProductEmbeddings");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "AppProductEmbeddings");

            migrationBuilder.DropColumn(
                name: "LastAttemptAt",
                table: "AppProductEmbeddings");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "AppProductEmbeddings");

            migrationBuilder.DropColumn(
                name: "SearchableTextHash",
                table: "AppProductEmbeddings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AppProductEmbeddings");
        }
    }
}
