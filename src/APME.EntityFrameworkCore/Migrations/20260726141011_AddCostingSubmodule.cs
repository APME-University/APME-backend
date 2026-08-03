using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APME.Migrations
{
    /// <inheritdoc />
    public partial class AddCostingSubmodule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppCostComponentEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentType = table.Column<int>(type: "integer", nullable: false),
                    Plane = table.Column<int>(type: "integer", nullable: false),
                    ValueType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_AppCostComponentEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppCostingPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultMethod = table.Column<int>(type: "integer", nullable: false),
                    MarginBasis = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    MarketplaceFeePct = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    PaymentFeePct = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    PaymentFixedPerUnit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    FulfillmentPerUnit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    StoragePerUnitMonth = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ReturnsReservePct = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    AdPerUnit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
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
                    table.PrimaryKey("PK_AppCostingPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCostingPolicies_AppShops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "AppShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppProductCostProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    CurrentLandedUnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QtyOnHand = table.Column<int>(type: "integer", nullable: false),
                    LastCostedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
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
                    table.PrimaryKey("PK_AppProductCostProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppProductCostProfiles_AppProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AppProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppProductCostProfiles_AppShops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "AppShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppCostComponentEntries_ProductId",
                table: "AppCostComponentEntries",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCostComponentEntries_ProductId_Plane_ValueType",
                table: "AppCostComponentEntries",
                columns: new[] { "ProductId", "Plane", "ValueType" });

            migrationBuilder.CreateIndex(
                name: "IX_AppCostComponentEntries_TenantId",
                table: "AppCostComponentEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCostingPolicies_ShopId",
                table: "AppCostingPolicies",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCostingPolicies_TenantId",
                table: "AppCostingPolicies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCostingPolicies_TenantId_ShopId",
                table: "AppCostingPolicies",
                columns: new[] { "TenantId", "ShopId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppProductCostProfiles_ProductId",
                table: "AppProductCostProfiles",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductCostProfiles_ShopId",
                table: "AppProductCostProfiles",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductCostProfiles_TenantId",
                table: "AppProductCostProfiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductCostProfiles_TenantId_ProductId",
                table: "AppProductCostProfiles",
                columns: new[] { "TenantId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppCostComponentEntries");

            migrationBuilder.DropTable(
                name: "AppCostingPolicies");

            migrationBuilder.DropTable(
                name: "AppProductCostProfiles");
        }
    }
}
