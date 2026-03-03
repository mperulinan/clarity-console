using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateTransactionAssetsToGuids : Migration
    {
        /// <inheritdoc />        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create missing assets
            migrationBuilder.Sql(@"
                INSERT INTO Asset (Id, ExternalId, Symbol, Name, AssetType)
                SELECT DISTINCT NEWID(), t.Code, t.Code, t.Code, 'CRYPTO'
                FROM (
                    SELECT FromAssetId as Code FROM [Transaction] WHERE FromAssetId IS NOT NULL
                    UNION
                    SELECT ToAssetId FROM [Transaction] WHERE ToAssetId IS NOT NULL
                    UNION
                    SELECT FeeAsset FROM [Transaction] WHERE FeeAsset IS NOT NULL
                ) as t
                WHERE t.Code NOT IN (SELECT ExternalId FROM Asset)
            ");

            // 2. Rename columns to keep old data
            migrationBuilder.RenameColumn(name: "FromAssetId", table: "Transaction", newName: "FromAssetId_Old");
            migrationBuilder.RenameColumn(name: "ToAssetId", table: "Transaction", newName: "ToAssetId_Old");
            migrationBuilder.RenameColumn(name: "FeeAsset", table: "Transaction", newName: "FeeAsset_Old");

            // 3. Create new GUID columns
            migrationBuilder.AddColumn<Guid>(name: "FromAssetId", table: "Transaction", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "ToAssetId", table: "Transaction", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "FeeAssetId", table: "Transaction", type: "uniqueidentifier", nullable: true);

            // 4. Migrate data
            migrationBuilder.Sql(@"
                UPDATE T SET T.FromAssetId = A.Id FROM [Transaction] T INNER JOIN Asset A ON T.FromAssetId_Old = A.ExternalId;
                UPDATE T SET T.ToAssetId = A.Id FROM [Transaction] T INNER JOIN Asset A ON T.ToAssetId_Old = A.ExternalId;
                UPDATE T SET T.FeeAssetId = A.Id FROM [Transaction] T INNER JOIN Asset A ON T.FeeAsset_Old = A.ExternalId;
            ");

            // 5. Cleaning up old columns and adding constraints
            migrationBuilder.DropColumn(name: "FromAssetId_Old", table: "Transaction");
            migrationBuilder.DropColumn(name: "ToAssetId_Old", table: "Transaction");
            migrationBuilder.DropColumn(name: "FeeAsset_Old", table: "Transaction");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_FromAssetId",
                table: "Transaction",
                column: "FromAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_ToAssetId",
                table: "Transaction",
                column: "ToAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_FeeAssetId",
                table: "Transaction",
                column: "FeeAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_FromAsset",
                table: "Transaction",
                column: "FromAssetId",
                principalTable: "Asset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_ToAsset",
                table: "Transaction",
                column: "ToAssetId",
                principalTable: "Asset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_FeeAsset",
                table: "Transaction",
                column: "FeeAssetId",
                principalTable: "Asset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Recreate the old string columns
            migrationBuilder.AddColumn<string>(name: "FromAssetId_New", table: "Transaction", type: "nvarchar(50)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ToAssetId_New", table: "Transaction", type: "nvarchar(50)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "FeeAsset_New", table: "Transaction", type: "nvarchar(50)", nullable: true);

            // 2. Recover old string values using the Asset table
            migrationBuilder.Sql(@"
                UPDATE T SET T.FromAssetId_New = A.ExternalId FROM [Transaction] T INNER JOIN Asset A ON T.FromAssetId = A.Id;
                UPDATE T SET T.ToAssetId_New = A.ExternalId FROM [Transaction] T INNER JOIN Asset A ON T.ToAssetId = A.Id;
                UPDATE T SET T.FeeAsset_New = A.ExternalId FROM [Transaction] T INNER JOIN Asset A ON T.FeeAssetId = A.Id;
            ");

            // 3. Delete the new GUID columns and their constraints
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_FromAsset",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_ToAsset",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_FeeAsset",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_FromAssetId",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_ToAssetId",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_FeeAssetId",
                table: "Transaction");

            migrationBuilder.DropColumn(name: "FromAssetId", table: "Transaction");
            migrationBuilder.DropColumn(name: "ToAssetId", table: "Transaction");
            migrationBuilder.DropColumn(name: "FeeAssetId", table: "Transaction");

            // 4. Rename the new string columns back to the original names
            migrationBuilder.RenameColumn(table: "Transaction", name: "FromAssetId_New", newName: "FromAssetId");
            migrationBuilder.RenameColumn(table: "Transaction", name: "ToAssetId_New", newName: "ToAssetId");
            migrationBuilder.RenameColumn(table: "Transaction", name: "FeeAsset_New", newName: "FeeAsset");
        }
    }
}
