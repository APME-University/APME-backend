using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APME.Migrations
{
    /// <inheritdoc />
    public partial class ModifyCartAndOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppCarts_AppShops_ShopId",
                table: "AppCarts");

            migrationBuilder.DropForeignKey(
                name: "FK_AppOrders_AppShops_ShopId",
                table: "AppOrders");

            migrationBuilder.DropIndex(
                name: "IX_AppOrders_ShopId",
                table: "AppOrders");

            migrationBuilder.DropIndex(
                name: "IX_AppOrders_ShopId_Status",
                table: "AppOrders");

            migrationBuilder.DropIndex(
                name: "IX_AppOrders_TenantId",
                table: "AppOrders");

            migrationBuilder.DropIndex(
                name: "IX_AppCarts_CustomerId_ShopId_Status",
                table: "AppCarts");

            migrationBuilder.DropIndex(
                name: "IX_AppCarts_ShopId",
                table: "AppCarts");

            migrationBuilder.DropIndex(
                name: "IX_AppCarts_TenantId",
                table: "AppCarts");

            migrationBuilder.DropColumn(
                name: "ShopId",
                table: "AppOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AppOrders");

            migrationBuilder.DropColumn(
                name: "ShopId",
                table: "AppCarts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AppCarts");

            migrationBuilder.AddColumn<Guid>(
                name: "ShopId",
                table: "AppOrderItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ShopId",
                table: "AppCartItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_AppOrderItems_ShopId",
                table: "AppOrderItems",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCarts_CustomerId_Status",
                table: "AppCarts",
                columns: new[] { "CustomerId", "Status" },
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppCartItems_CartId_ShopId_ProductId",
                table: "AppCartItems",
                columns: new[] { "CartId", "ShopId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppCartItems_ShopId",
                table: "AppCartItems",
                column: "ShopId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppCartItems_AppShops_ShopId",
                table: "AppCartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_AppOrderItems_AppShops_ShopId",
                table: "AppOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_AppOrderItems_ShopId",
                table: "AppOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_AppCarts_CustomerId_Status",
                table: "AppCarts");

            migrationBuilder.DropIndex(
                name: "IX_AppCartItems_CartId_ShopId_ProductId",
                table: "AppCartItems");

            migrationBuilder.DropIndex(
                name: "IX_AppCartItems_ShopId",
                table: "AppCartItems");

            migrationBuilder.DropColumn(
                name: "ShopId",
                table: "AppOrderItems");

            migrationBuilder.DropColumn(
                name: "ShopId",
                table: "AppCartItems");

            migrationBuilder.AddColumn<Guid>(
                name: "ShopId",
                table: "AppOrders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AppOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ShopId",
                table: "AppCarts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AppCarts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppOrders_ShopId",
                table: "AppOrders",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_AppOrders_ShopId_Status",
                table: "AppOrders",
                columns: new[] { "ShopId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppOrders_TenantId",
                table: "AppOrders",
                column: "TenantId");

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
                name: "IX_AppCarts_TenantId",
                table: "AppCarts",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppCarts_AppShops_ShopId",
                table: "AppCarts",
                column: "ShopId",
                principalTable: "AppShops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppOrders_AppShops_ShopId",
                table: "AppOrders",
                column: "ShopId",
                principalTable: "AppShops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
