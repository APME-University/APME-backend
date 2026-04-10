using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APME.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryIdToProductEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "AppProductEmbeddings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductEmbeddings_CategoryId",
                table: "AppProductEmbeddings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductEmbeddings_IsActive_CategoryId",
                table: "AppProductEmbeddings",
                columns: new[] { "IsActive", "CategoryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductEmbeddings_CategoryId",
                table: "AppProductEmbeddings");

            migrationBuilder.DropIndex(
                name: "IX_ProductEmbeddings_IsActive_CategoryId",
                table: "AppProductEmbeddings");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "AppProductEmbeddings");
        }
    }
}
