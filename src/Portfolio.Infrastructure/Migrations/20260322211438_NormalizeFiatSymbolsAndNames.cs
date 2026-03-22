using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeFiatSymbolsAndNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Asset",
                keyColumn: "Id",
                keyValue: new Guid("0200f65c-ea84-4ed6-b1c0-eb36527f11ed"),
                columns: new[] { "Name", "Symbol" },
                values: new object[] { "US Dollar", "USD" });

            migrationBuilder.UpdateData(
                table: "Asset",
                keyColumn: "Id",
                keyValue: new Guid("2574e866-b50c-41ea-9293-ce8964aefcd2"),
                columns: new[] { "Name", "Symbol" },
                values: new object[] { "Euro", "EUR" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Asset",
                keyColumn: "Id",
                keyValue: new Guid("0200f65c-ea84-4ed6-b1c0-eb36527f11ed"),
                columns: new[] { "Name", "Symbol" },
                values: new object[] { "USD", "usd" });

            migrationBuilder.UpdateData(
                table: "Asset",
                keyColumn: "Id",
                keyValue: new Guid("2574e866-b50c-41ea-9293-ce8964aefcd2"),
                columns: new[] { "Name", "Symbol" },
                values: new object[] { "EUR", "eur" });
        }
    }
}
