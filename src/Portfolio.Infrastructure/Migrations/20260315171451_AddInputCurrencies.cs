using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInputCurrencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SpotPriceInUsd",
                table: "Transaction",
                newName: "SpotPriceUSD");

            migrationBuilder.AlterColumn<decimal>(
                name: "SpotPriceUSD",
                table: "Transaction",
                type: "decimal(36,18)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(36,18)",
                oldNullable: false);

            migrationBuilder.RenameColumn(
                name: "SpotPriceInEur",
                table: "Transaction",
                newName: "SpotPriceEUR");

            migrationBuilder.RenameColumn(
                name: "FeeSpotPriceInUsd",
                table: "Transaction",
                newName: "FeePriceUSD");

            migrationBuilder.RenameColumn(
                name: "FeeSpotPriceInEur",
                table: "Transaction",
                newName: "FeePriceEUR");

            migrationBuilder.AddColumn<string>(
                name: "SpotPriceInputCurrency",
                table: "Transaction",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE [Transaction]
                SET SpotPriceInputCurrency = 'eur'");

            migrationBuilder.AlterColumn<string>(
                name: "SpotPriceInputCurrency",
                table: "Transaction",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeePriceInputCurrency",
                table: "Transaction",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE [Transaction]
                SET FeePriceInputCurrency = 'eur'
                WHERE FeePriceEUR IS NOT NULL AND FeePriceEUR > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeePriceInputCurrency",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "SpotPriceInputCurrency",
                table: "Transaction");

            migrationBuilder.RenameColumn(
                name: "SpotPriceUSD",
                table: "Transaction",
                newName: "SpotPriceInUsd");

            migrationBuilder.Sql(@"
                UPDATE [Transaction]
                SET SpotPriceInUsd = 0
                WHERE SpotPriceInUsd IS NULL");

            migrationBuilder.AlterColumn<decimal>(
                name: "SpotPriceInUsd",
                table: "Transaction",
                type: "decimal(36,18)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(36,18)",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "SpotPriceEUR",
                table: "Transaction",
                newName: "SpotPriceInEur");

            migrationBuilder.RenameColumn(
                name: "FeePriceUSD",
                table: "Transaction",
                newName: "FeeSpotPriceInUsd");

            migrationBuilder.RenameColumn(
                name: "FeePriceEUR",
                table: "Transaction",
                newName: "FeeSpotPriceInEur");
        }
    }
}
