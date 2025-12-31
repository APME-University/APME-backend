using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APME.Migrations
{
    /// <inheritdoc />
    public partial class AddCartAndOrderCheckOut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LowStockThreshold",
                table: "AppProducts",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<string>(
                name: "StockConcurrencyStamp",
                table: "AppProducts",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AppCarts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_AppCarts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCarts_AppCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AppCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppCarts_AppShops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "AppShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ShippingFullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ShippingStreet = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ShippingStreet2 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ShippingCity = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ShippingState = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ShippingPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShippingCountry = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    ShippingPhone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    BillingFullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    BillingStreet = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    BillingStreet2 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    BillingCity = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BillingState = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BillingPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BillingCountry = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    BillingPhone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    PaymentTransactionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                    PaymentProcessedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PaymentCardLast4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    PaymentCardBrand = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PaymentFailureMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ShippingAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CustomerNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InternalNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShippingCarrier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_AppOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppOrders_AppCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AppCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppOrders_AppShops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "AppShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppCartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProductSku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProductImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCartItems_AppCarts_CartId",
                        column: x => x.CartId,
                        principalTable: "AppCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProductSku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProductImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppOrderItems_AppOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "AppOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppCartItems_CartId",
                table: "AppCartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCartItems_ProductId",
                table: "AppCartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCarts_CustomerId",
                table: "AppCarts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCarts_CustomerId_ShopId_Status",
                table: "AppCarts",
                columns: new[] { "CustomerId", "ShopId", "Status" },
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppCarts_ShopId",
                table: "AppCarts",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCarts_Status",
                table: "AppCarts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppCarts_TenantId",
                table: "AppCarts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppOrderItems_OrderId",
                table: "AppOrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AppOrderItems_ProductId",
                table: "AppOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppOrders_CreationTime",
                table: "AppOrders",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppOrders_CustomerId",
                table: "AppOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_AppOrders_CustomerId_CreationTime",
                table: "AppOrders",
                columns: new[] { "CustomerId", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AppOrders_OrderNumber",
                table: "AppOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppOrders_ShopId",
                table: "AppOrders",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_AppOrders_ShopId_Status",
                table: "AppOrders",
                columns: new[] { "ShopId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppOrders_Status",
                table: "AppOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppOrders_TenantId",
                table: "AppOrders",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppCartItems");

            migrationBuilder.DropTable(
                name: "AppOrderItems");

            migrationBuilder.DropTable(
                name: "AppCarts");

            migrationBuilder.DropTable(
                name: "AppOrders");

            migrationBuilder.DropColumn(
                name: "LowStockThreshold",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "StockConcurrencyStamp",
                table: "AppProducts");
        }
    }
}
