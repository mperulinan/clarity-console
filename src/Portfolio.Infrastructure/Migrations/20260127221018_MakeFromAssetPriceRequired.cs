using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeFromAssetPriceRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "FromAssetPriceInUsd",
                table: "Transaction",
                type: "decimal(36,18)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(36,18)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "FromAssetPriceInUsd",
                table: "Transaction",
                type: "decimal(36,18)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(36,18)");
        }
    }
}
