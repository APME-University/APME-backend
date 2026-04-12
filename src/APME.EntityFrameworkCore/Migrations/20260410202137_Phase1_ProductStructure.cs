using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace APME.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_ProductStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<Guid>(
                name: "BrandId",
                table: "AppProducts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "AppProducts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "AppProducts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "AppProducts",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchHints",
                table: "AppProducts",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchableText",
                table: "AppProducts",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "AppProducts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockStatus",
                table: "AppProducts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "AttributeGroupId",
                table: "AppProductAttributes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsComparable",
                table: "AppProductAttributes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFilterable",
                table: "AppProductAttributes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSearchable",
                table: "AppProductAttributes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "AppProductAttributes",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationRulesJson",
                table: "AppProductAttributes",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "AppChatSessions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntitiesJson",
                table: "AppChatMessages",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Intent",
                table: "AppChatMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "IntentConfidence",
                table: "AppChatMessages",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingTimeMs",
                table: "AppChatMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferencedProductIds",
                table: "AppChatMessages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppAttributeGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_AppAttributeGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppAttributeGroups_AppCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "AppCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppAttributeOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductAttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAttributeOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppAttributeOptions_AppProductAttributes_ProductAttributeId",
                        column: x => x.ProductAttributeId,
                        principalTable: "AppProductAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppBrands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_AppBrands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppConversationContexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContextJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppConversationContexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppConversationContexts_AppChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AppChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppImageEmbeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductImageId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(768)", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Dimensions = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppImageEmbeddings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppIntentClassificationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Intent = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    ModelUsed = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RawResponseJson = table.Column<string>(type: "jsonb", nullable: true),
                    ProcessingTimeMs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppIntentClassificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppIntentClassificationLogs_AppChatMessages_ChatMessageId",
                        column: x => x.ChatMessageId,
                        principalTable: "AppChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppProductAttributeValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductAttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextValue = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NumericValue = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    BoolValue = table.Column<bool>(type: "boolean", nullable: true),
                    DisplayValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppProductAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppProductAttributeValues_AppProductAttributes_ProductAttri~",
                        column: x => x.ProductAttributeId,
                        principalTable: "AppProductAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppProductAttributeValues_AppProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AppProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppProductTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tag = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppProductTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppProductTags_AppProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AppProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppProductVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SKU = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    SalePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    StockStatus = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    VariantAttributesJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppProductVariants_AppProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AppProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppSearchQueryLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Query = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    RewrittenQuery = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ClassifiedIntent = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TopResultIdsJson = table.Column<string>(type: "jsonb", nullable: true),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProcessingTimeMs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSearchQueryLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppProductImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AltText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ImageEmbeddingId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppProductImages_AppProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "AppProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppProductImages_AppProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AppProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppProducts_BrandId",
                table: "AppProducts",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProducts_IsFeatured",
                table: "AppProducts",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_AppProducts_SearchableText",
                table: "AppProducts",
                column: "SearchableText");

            migrationBuilder.CreateIndex(
                name: "IX_AppProducts_StockStatus",
                table: "AppProducts",
                column: "StockStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductAttributes_AttributeGroupId",
                table: "AppProductAttributes",
                column: "AttributeGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductAttributes_IsSearchable_IsFilterable",
                table: "AppProductAttributes",
                columns: new[] { "IsSearchable", "IsFilterable" });

            migrationBuilder.CreateIndex(
                name: "IX_AppChatMessages_Intent",
                table: "AppChatMessages",
                column: "Intent");

            migrationBuilder.CreateIndex(
                name: "IX_AppAttributeGroups_CategoryId",
                table: "AppAttributeGroups",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppAttributeGroups_TenantId",
                table: "AppAttributeGroups",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppAttributeOptions_ProductAttributeId",
                table: "AppAttributeOptions",
                column: "ProductAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBrands_Slug",
                table: "AppBrands",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_AppBrands_TenantId",
                table: "AppBrands",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBrands_TenantId_Slug",
                table: "AppBrands",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppConversationContexts_SessionId",
                table: "AppConversationContexts",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppImageEmbeddings_ImageUrl",
                table: "AppImageEmbeddings",
                column: "ImageUrl");

            migrationBuilder.CreateIndex(
                name: "IX_AppImageEmbeddings_ProductImageId",
                table: "AppImageEmbeddings",
                column: "ProductImageId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageEmbeddings_Embedding_HNSW",
                table: "AppImageEmbeddings",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_AppIntentClassificationLogs_ChatMessageId",
                table: "AppIntentClassificationLogs",
                column: "ChatMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppIntentClassificationLogs_CreatedAt",
                table: "AppIntentClassificationLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AppIntentClassificationLogs_Intent",
                table: "AppIntentClassificationLogs",
                column: "Intent");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductAttributeValues_ProductAttributeId",
                table: "AppProductAttributeValues",
                column: "ProductAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductAttributeValues_ProductId",
                table: "AppProductAttributeValues",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductAttributeValues_ProductId_ProductAttributeId",
                table: "AppProductAttributeValues",
                columns: new[] { "ProductId", "ProductAttributeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppProductImages_IsPrimary",
                table: "AppProductImages",
                column: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductImages_ProductId",
                table: "AppProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductImages_VariantId",
                table: "AppProductImages",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductTags_ProductId",
                table: "AppProductTags",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductTags_ProductId_Tag",
                table: "AppProductTags",
                columns: new[] { "ProductId", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppProductTags_Tag",
                table: "AppProductTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductVariants_ProductId",
                table: "AppProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductVariants_SKU",
                table: "AppProductVariants",
                column: "SKU");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductVariants_TenantId_SKU",
                table: "AppProductVariants",
                columns: new[] { "TenantId", "SKU" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSearchQueryLogs_ClassifiedIntent",
                table: "AppSearchQueryLogs",
                column: "ClassifiedIntent");

            migrationBuilder.CreateIndex(
                name: "IX_AppSearchQueryLogs_CreatedAt",
                table: "AppSearchQueryLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AppSearchQueryLogs_SessionId",
                table: "AppSearchQueryLogs",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppProductAttributes_AppAttributeGroups_AttributeGroupId",
                table: "AppProductAttributes",
                column: "AttributeGroupId",
                principalTable: "AppAttributeGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AppProducts_AppBrands_BrandId",
                table: "AppProducts",
                column: "BrandId",
                principalTable: "AppBrands",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppProductAttributes_AppAttributeGroups_AttributeGroupId",
                table: "AppProductAttributes");

            migrationBuilder.DropForeignKey(
                name: "FK_AppProducts_AppBrands_BrandId",
                table: "AppProducts");

            migrationBuilder.DropTable(
                name: "AppAttributeGroups");

            migrationBuilder.DropTable(
                name: "AppAttributeOptions");

            migrationBuilder.DropTable(
                name: "AppBrands");

            migrationBuilder.DropTable(
                name: "AppConversationContexts");

            migrationBuilder.DropTable(
                name: "AppImageEmbeddings");

            migrationBuilder.DropTable(
                name: "AppIntentClassificationLogs");

            migrationBuilder.DropTable(
                name: "AppProductAttributeValues");

            migrationBuilder.DropTable(
                name: "AppProductImages");

            migrationBuilder.DropTable(
                name: "AppProductTags");

            migrationBuilder.DropTable(
                name: "AppSearchQueryLogs");

            migrationBuilder.DropTable(
                name: "AppProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_AppProducts_BrandId",
                table: "AppProducts");

            migrationBuilder.DropIndex(
                name: "IX_AppProducts_IsFeatured",
                table: "AppProducts");

            migrationBuilder.DropIndex(
                name: "IX_AppProducts_SearchableText",
                table: "AppProducts");

            migrationBuilder.DropIndex(
                name: "IX_AppProducts_StockStatus",
                table: "AppProducts");

            migrationBuilder.DropIndex(
                name: "IX_AppProductAttributes_AttributeGroupId",
                table: "AppProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_AppProductAttributes_IsSearchable_IsFilterable",
                table: "AppProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_AppChatMessages_Intent",
                table: "AppChatMessages");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "SearchHints",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "SearchableText",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "StockStatus",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "AttributeGroupId",
                table: "AppProductAttributes");

            migrationBuilder.DropColumn(
                name: "IsComparable",
                table: "AppProductAttributes");

            migrationBuilder.DropColumn(
                name: "IsFilterable",
                table: "AppProductAttributes");

            migrationBuilder.DropColumn(
                name: "IsSearchable",
                table: "AppProductAttributes");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "AppProductAttributes");

            migrationBuilder.DropColumn(
                name: "ValidationRulesJson",
                table: "AppProductAttributes");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "AppChatSessions");

            migrationBuilder.DropColumn(
                name: "EntitiesJson",
                table: "AppChatMessages");

            migrationBuilder.DropColumn(
                name: "Intent",
                table: "AppChatMessages");

            migrationBuilder.DropColumn(
                name: "IntentConfidence",
                table: "AppChatMessages");

            migrationBuilder.DropColumn(
                name: "ProcessingTimeMs",
                table: "AppChatMessages");

            migrationBuilder.DropColumn(
                name: "ReferencedProductIds",
                table: "AppChatMessages");

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
    }
}
