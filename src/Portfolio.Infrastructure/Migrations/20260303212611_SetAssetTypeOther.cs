using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SetAssetTypeOther : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Asset 
                SET AssetType = 'OTHER'
                WHERE ExternalId IS NULL
                  AND AssetType != 'FIAT'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Asset 
                SET AssetType = 'CRYPTO'
                WHERE AssetType = 'OTHER'
            ");
        }
    }
}
