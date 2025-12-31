using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APME.Migrations
{
    /// <inheritdoc />
    public partial class HandleCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppCustomers_TenantId_UserId",
                table: "AppCustomers");

            migrationBuilder.DropIndex(
                name: "IX_AppCustomers_UserId",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AppCustomers");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AppCustomers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "AppCustomers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "AppCustomers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "AppCustomers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LockoutEnabled",
                table: "AppCustomers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "AppCustomers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "AppCustomers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUserName",
                table: "AppCustomers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "AppCustomers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "AppCustomers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "AppCustomers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "AppCustomers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "AppCustomers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "AbpUserTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "AbpUserLogins",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "AbpUserClaims",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppCustomerUserRole",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCustomerUserRole", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AppCustomerUserRole_AbpRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AbpRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppCustomerUserRole_AppCustomers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomers_Email",
                table: "AppCustomers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomers_NormalizedEmail",
                table: "AppCustomers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomers_NormalizedUserName",
                table: "AppCustomers",
                column: "NormalizedUserName");

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomers_PhoneNumber",
                table: "AppCustomers",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomers_UserName",
                table: "AppCustomers",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserTokens_CustomerId",
                table: "AbpUserTokens",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserLogins_CustomerId",
                table: "AbpUserLogins",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserClaims_CustomerId",
                table: "AbpUserClaims",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomerUserRole_RoleId",
                table: "AppCustomerUserRole",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_AbpUserClaims_AppCustomers_CustomerId",
                table: "AbpUserClaims",
                column: "CustomerId",
                principalTable: "AppCustomers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AbpUserLogins_AppCustomers_CustomerId",
                table: "AbpUserLogins",
                column: "CustomerId",
                principalTable: "AppCustomers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AbpUserTokens_AppCustomers_CustomerId",
                table: "AbpUserTokens",
                column: "CustomerId",
                principalTable: "AppCustomers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbpUserClaims_AppCustomers_CustomerId",
                table: "AbpUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AbpUserLogins_AppCustomers_CustomerId",
                table: "AbpUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AbpUserTokens_AppCustomers_CustomerId",
                table: "AbpUserTokens");

            migrationBuilder.DropTable(
                name: "AppCustomerUserRole");

            migrationBuilder.DropIndex(
                name: "IX_AppCustomers_Email",
                table: "AppCustomers");

            migrationBuilder.DropIndex(
                name: "IX_AppCustomers_NormalizedEmail",
                table: "AppCustomers");

            migrationBuilder.DropIndex(
                name: "IX_AppCustomers_NormalizedUserName",
                table: "AppCustomers");

            migrationBuilder.DropIndex(
                name: "IX_AppCustomers_PhoneNumber",
                table: "AppCustomers");

            migrationBuilder.DropIndex(
                name: "IX_AppCustomers_UserName",
                table: "AppCustomers");

            migrationBuilder.DropIndex(
                name: "IX_AbpUserTokens_CustomerId",
                table: "AbpUserTokens");

            migrationBuilder.DropIndex(
                name: "IX_AbpUserLogins_CustomerId",
                table: "AbpUserLogins");

            migrationBuilder.DropIndex(
                name: "IX_AbpUserClaims_CustomerId",
                table: "AbpUserClaims");

            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "LockoutEnabled",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "NormalizedUserName",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmed",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "AbpUserTokens");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "AbpUserLogins");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "AbpUserClaims");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AppCustomers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "AppCustomers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomers_TenantId_UserId",
                table: "AppCustomers",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomers_UserId",
                table: "AppCustomers",
                column: "UserId");
        }
    }
}
