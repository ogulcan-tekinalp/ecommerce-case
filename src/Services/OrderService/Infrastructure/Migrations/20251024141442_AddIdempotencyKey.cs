using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyKey : Migration
    {
        /// <inheritdoc />
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<string>(
        name: "IdempotencyKey",
        table: "orders",
        type: "character varying(100)",
        maxLength: 100,
        nullable: true);

    migrationBuilder.CreateIndex(
        name: "IX_orders_IdempotencyKey",
        table: "orders",
        column: "IdempotencyKey",
        unique: true,
        filter: "\"IdempotencyKey\" IS NOT NULL");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropIndex(
        name: "IX_orders_IdempotencyKey",
        table: "orders");

    migrationBuilder.DropColumn(
        name: "IdempotencyKey",
        table: "orders");
}
    }
}
