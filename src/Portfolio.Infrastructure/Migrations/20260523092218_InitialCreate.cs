using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Asset",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asset", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FromAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AmountSpent = table.Column<decimal>(type: "decimal(36,18)", nullable: false),
                    AmountReceived = table.Column<decimal>(type: "decimal(36,18)", nullable: false),
                    SpotPriceUSD = table.Column<decimal>(type: "decimal(36,18)", nullable: true),
                    SpotPriceEUR = table.Column<decimal>(type: "decimal(36,18)", nullable: true),
                    Fee = table.Column<decimal>(type: "decimal(36,18)", nullable: false),
                    FeeAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FeePriceUSD = table.Column<decimal>(type: "decimal(36,18)", nullable: true),
                    FeePriceEUR = table.Column<decimal>(type: "decimal(36,18)", nullable: true),
                    UsdEurExchangeRate = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    SpotPriceInputCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FeePriceInputCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transaction_FeeAsset",
                        column: x => x.FeeAssetId,
                        principalTable: "Asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transaction_FromAsset",
                        column: x => x.FromAssetId,
                        principalTable: "Asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transaction_ToAsset",
                        column: x => x.ToAssetId,
                        principalTable: "Asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Asset",
                columns: new[] { "Id", "ExternalId", "ImageUrl", "Name", "Symbol", "AssetType" },
                values: new object[,]
                {
                    { new Guid("0200f65c-ea84-4ed6-b1c0-eb36527f11ed"), "usd", "https://static.okx.com/cdn/oksupport/asset/currency/icon/usd.png", "US Dollar", "USD", "FIAT" },
                    { new Guid("2574e866-b50c-41ea-9293-ce8964aefcd2"), "eur", "https://static.okx.com/cdn/oksupport/asset/currency/icon/eur.png", "Euro", "EUR", "FIAT" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_FeeAssetId",
                table: "Transaction",
                column: "FeeAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_FromAssetId",
                table: "Transaction",
                column: "FromAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_ToAssetId",
                table: "Transaction",
                column: "ToAssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropTable(
                name: "Asset");
        }
    }
}
