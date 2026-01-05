using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace APME.Migrations
{
    /// <inheritdoc />
    public partial class AddAIEmbeddingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppCartItems_AppShops_ShopId",
                table: "AppCartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_AppOrderItems_AppShops_ShopId",
                table: "AppOrderItems");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<string>(
                name: "CanonicalDocument",
                table: "AppProducts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CanonicalDocumentUpdatedAt",
                table: "AppProducts",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CanonicalDocumentVersion",
                table: "AppProducts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EmbeddingGenerated",
                table: "AppProducts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EmbeddingPriority",
                table: "AppProductAttributes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeInEmbedding",
                table: "AppProductAttributes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SemanticLabel",
                table: "AppProductAttributes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppProductEmbeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ChunkText = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(768)", nullable: false),
                    EmbeddingVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    EmbeddingModel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    CanonicalDocumentVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppProductEmbeddings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppProducts_IsActive_IsPublished_EmbeddingGenerated",
                table: "AppProducts",
                columns: new[] { "IsActive", "IsPublished", "EmbeddingGenerated" });

            migrationBuilder.CreateIndex(
                name: "IX_AppProductAttributes_ShopId_IncludeInEmbedding",
                table: "AppProductAttributes",
                columns: new[] { "ShopId", "IncludeInEmbedding" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEmbeddings_Embedding_HNSW",
                table: "AppProductEmbeddings",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEmbeddings_IsActive",
                table: "AppProductEmbeddings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProductEmbeddings_IsActive_TenantId",
                table: "AppProductEmbeddings",
                columns: new[] { "IsActive", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEmbeddings_ProductId_ChunkIndex",
                table: "AppProductEmbeddings",
                columns: new[] { "ProductId", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductEmbeddings_ShopId",
                table: "AppProductEmbeddings",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductEmbeddings_TenantId",
                table: "AppProductEmbeddings",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppProductEmbeddings");

            migrationBuilder.DropIndex(
                name: "IX_AppProducts_IsActive_IsPublished_EmbeddingGenerated",
                table: "AppProducts");

            migrationBuilder.DropIndex(
                name: "IX_AppProductAttributes_ShopId_IncludeInEmbedding",
                table: "AppProductAttributes");

            migrationBuilder.DropColumn(
                name: "CanonicalDocument",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "CanonicalDocumentUpdatedAt",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "CanonicalDocumentVersion",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "EmbeddingGenerated",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "EmbeddingPriority",
                table: "AppProductAttributes");

            migrationBuilder.DropColumn(
                name: "IncludeInEmbedding",
                table: "AppProductAttributes");

            migrationBuilder.DropColumn(
                name: "SemanticLabel",
                table: "AppProductAttributes");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddForeignKey(
                name: "FK_AppCartItems_AppShops_ShopId",
                table: "AppCartItems",
                column: "ShopId",
                principalTable: "AppShops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppOrderItems_AppShops_ShopId",
                table: "AppOrderItems",
                column: "ShopId",
                principalTable: "AppShops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
