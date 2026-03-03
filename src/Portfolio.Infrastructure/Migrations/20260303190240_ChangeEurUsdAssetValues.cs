using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeEurUsdAssetValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Asset",
                keyColumn: "Id",
                keyValue: new Guid("0200f65c-ea84-4ed6-b1c0-eb36527f11ed"),
                columns: new[] { "Symbol", "Name", "ExternalId", "AssetType" },
                values: new object[] { "usd", "USD", "usd", "FIAT" });

            migrationBuilder.UpdateData(
                table: "Asset",
                keyColumn: "Id",
                keyValue: new Guid("2574e866-b50c-41ea-9293-ce8964aefcd2"),
                columns: new[] { "Symbol", "Name", "ExternalId", "AssetType" },
                values: new object[] { "eur", "EUR", "eur", "FIAT" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Asset",
                keyColumn: "Id",
                keyValue: new Guid("0200f65c-ea84-4ed6-b1c0-eb36527f11ed"),
                columns: new[] { "Symbol", "Name", "ExternalId", "AssetType" },
                values: new object[] { "usd", "usd", "usd", "CRYPTO" });

            migrationBuilder.UpdateData(
                table: "Asset",
                keyColumn: "Id",
                keyValue: new Guid("2574e866-b50c-41ea-9293-ce8964aefcd2"),
                columns: new[] { "Symbol", "Name", "ExternalId", "AssetType" },
                values: new object[] { "eur", "eur", "eur", "CRYPTO" });
        }
    }
}
