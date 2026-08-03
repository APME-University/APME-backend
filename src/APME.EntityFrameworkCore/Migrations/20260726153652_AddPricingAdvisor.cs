using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APME.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingAdvisor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppCompetitorPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    Competitor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    Url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MatchConfidence = table.Column<double>(type: "double precision", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_AppCompetitorPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPriceChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NewPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    ChangedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
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
                    table.PrimaryKey("PK_AppPriceChangeLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPricingAttentionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scenario = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedOpportunity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReasonSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
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
                    table.PrimaryKey("PK_AppPricingAttentionItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPricingPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinMarginPct = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    MaxDiscountPct = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    MaxIncreasePct = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    CandidateGridJson = table.Column<string>(type: "jsonb", nullable: false),
                    AutoApplyEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_AppPricingPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPricingPolicies_AppShops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "AppShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppPricingRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scenario = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RecommendedPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ExpectedDemand = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExpectedRevenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExpectedProfit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExpectedMargin = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    Confidence = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReasonCodes = table.Column<string>(type: "jsonb", nullable: true),
                    Explanation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DecidedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DecisionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_AppPricingRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPricingRecommendations_AppProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AppProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppPricingRecommendations_AppShops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "AppShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppRecommendationOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecommendationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualUnits = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ActualRevenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ActualProfit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    WindowEnd = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MeasuredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
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
                    table.PrimaryKey("PK_AppRecommendationOutcomes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPriceCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecommendationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PctChange = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    PredictedDemand = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExpectedRevenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExpectedProfit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Margin = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    GuardrailStatus = table.Column<int>(type: "integer", nullable: false),
                    GuardrailsJson = table.Column<string>(type: "jsonb", nullable: true),
                    IsRecommended = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPriceCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPriceCandidates_AppPricingRecommendations_Recommendation~",
                        column: x => x.RecommendationId,
                        principalTable: "AppPricingRecommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppCompetitorPrices_ShopId_ProductId",
                table: "AppCompetitorPrices",
                columns: new[] { "ShopId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCompetitorPrices_TenantId",
                table: "AppCompetitorPrices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPriceCandidates_RecommendationId",
                table: "AppPriceCandidates",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPriceChangeLogs_ShopId_ProductId",
                table: "AppPriceChangeLogs",
                columns: new[] { "ShopId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppPriceChangeLogs_TenantId",
                table: "AppPriceChangeLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPricingAttentionItems_ShopId_IsResolved_Priority",
                table: "AppPricingAttentionItems",
                columns: new[] { "ShopId", "IsResolved", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_AppPricingAttentionItems_ShopId_ProductId",
                table: "AppPricingAttentionItems",
                columns: new[] { "ShopId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppPricingAttentionItems_TenantId",
                table: "AppPricingAttentionItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPricingPolicies_ShopId",
                table: "AppPricingPolicies",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPricingPolicies_TenantId",
                table: "AppPricingPolicies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPricingPolicies_TenantId_ShopId",
                table: "AppPricingPolicies",
                columns: new[] { "TenantId", "ShopId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppPricingRecommendations_ProductId",
                table: "AppPricingRecommendations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPricingRecommendations_ShopId_ProductId",
                table: "AppPricingRecommendations",
                columns: new[] { "ShopId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppPricingRecommendations_Status",
                table: "AppPricingRecommendations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppPricingRecommendations_TenantId",
                table: "AppPricingRecommendations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppRecommendationOutcomes_RecommendationId",
                table: "AppRecommendationOutcomes",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppRecommendationOutcomes_TenantId",
                table: "AppRecommendationOutcomes",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppCompetitorPrices");

            migrationBuilder.DropTable(
                name: "AppPriceCandidates");

            migrationBuilder.DropTable(
                name: "AppPriceChangeLogs");

            migrationBuilder.DropTable(
                name: "AppPricingAttentionItems");

            migrationBuilder.DropTable(
                name: "AppPricingPolicies");

            migrationBuilder.DropTable(
                name: "AppRecommendationOutcomes");

            migrationBuilder.DropTable(
                name: "AppPricingRecommendations");
        }
    }
}
