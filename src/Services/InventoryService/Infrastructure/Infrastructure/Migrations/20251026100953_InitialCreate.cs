using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Infrastructure.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AvailableQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "flash_sale_products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxQuantityPerCustomer = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flash_sale_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_flash_sale_products_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsReleased = table.Column<bool>(type: "boolean", nullable: false),
                    ReleasedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleaseReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_reservations_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlashSaleProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    PurchaseDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_purchases_flash_sale_products_FlashSaleProductId",
                        column: x => x.FlashSaleProductId,
                        principalTable: "flash_sale_products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_purchases_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchases_CustomerId",
                table: "customer_purchases",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchases_CustomerId_FlashSaleProductId",
                table: "customer_purchases",
                columns: new[] { "CustomerId", "FlashSaleProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchases_CustomerId_ProductId",
                table: "customer_purchases",
                columns: new[] { "CustomerId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchases_FlashSaleProductId",
                table: "customer_purchases",
                column: "FlashSaleProductId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchases_ProductId",
                table: "customer_purchases",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_flash_sale_products_IsActive_StartTimeUtc_EndTimeUtc",
                table: "flash_sale_products",
                columns: new[] { "IsActive", "StartTimeUtc", "EndTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_flash_sale_products_ProductId",
                table: "flash_sale_products",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_ExpiresAtUtc_IsReleased",
                table: "stock_reservations",
                columns: new[] { "ExpiresAtUtc", "IsReleased" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_OrderId",
                table: "stock_reservations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_ProductId",
                table: "stock_reservations",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_purchases");

            migrationBuilder.DropTable(
                name: "stock_reservations");

            migrationBuilder.DropTable(
                name: "flash_sale_products");

            migrationBuilder.DropTable(
                name: "products");
        }
    }
}
