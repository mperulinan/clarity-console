using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Asset",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Asset",
                keyColumn: "Id",
                keyValue: new Guid("0200f65c-ea84-4ed6-b1c0-eb36527f11ed"),
                column: "ImageUrl",
                value: "https://static.okx.com/cdn/oksupport/asset/currency/icon/usd.png");

            migrationBuilder.UpdateData(
                table: "Asset",
                keyColumn: "Id",
                keyValue: new Guid("2574e866-b50c-41ea-9293-ce8964aefcd2"),
                column: "ImageUrl",
                value: "https://static.okx.com/cdn/oksupport/asset/currency/icon/eur.png");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Asset");
        }
    }
}
