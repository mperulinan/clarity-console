using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorTransactionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FromAssetPriceInUsd",
                table: "Transaction",
                newName: "SpotPriceInUsd");

            migrationBuilder.RenameColumn(
                name: "FromAssetPriceInEur",
                table: "Transaction",
                newName: "SpotPriceInEur");

            migrationBuilder.RenameColumn(
                name: "FeeAssetPriceInUsd",
                table: "Transaction",
                newName: "FeeSpotPriceInUsd");

            migrationBuilder.RenameColumn(
                name: "FeeAssetPriceInEur",
                table: "Transaction",
                newName: "FeeSpotPriceInEur");

            migrationBuilder.AlterColumn<Guid>(
                name: "ToAssetId",
                table: "Transaction",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "FromAssetId",
                table: "Transaction",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.Sql(@"
                UPDATE [Transaction]
                SET TransactionType = 'DEPOSIT'
                WHERE TransactionType = 'TRANSFER_IN'");

            migrationBuilder.Sql(@"
                UPDATE [Transaction]
                SET FromAssetId = NULL
                WHERE TransactionType = 'REWARD' OR TransactionType = 'DEPOSIT'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE [Transaction]
                SET TransactionType = 'TRANSFER_IN'
                WHERE TransactionType = 'DEPOSIT'");

            migrationBuilder.Sql(@"
                UPDATE [Transaction]
                SET FromAssetId = '00000000-0000-0000-0000-000000000000'
                WHERE TransactionType = 'REWARD' OR TransactionType = 'DEPOSIT'");

            migrationBuilder.RenameColumn(
                name: "SpotPriceInUsd",
                table: "Transaction",
                newName: "FromAssetPriceInUsd");

            migrationBuilder.RenameColumn(
                name: "SpotPriceInEur",
                table: "Transaction",
                newName: "FromAssetPriceInEur");

            migrationBuilder.RenameColumn(
                name: "FeeSpotPriceInUsd",
                table: "Transaction",
                newName: "FeeAssetPriceInUsd");

            migrationBuilder.RenameColumn(
                name: "FeeSpotPriceInEur",
                table: "Transaction",
                newName: "FeeAssetPriceInEur");

            migrationBuilder.AlterColumn<Guid>(
                name: "ToAssetId",
                table: "Transaction",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FromAssetId",
                table: "Transaction",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
