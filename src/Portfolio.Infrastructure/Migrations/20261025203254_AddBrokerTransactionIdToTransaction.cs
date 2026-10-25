using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrokerTransactionIdToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrokerTransactionId",
                table: "Transaction",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_BrokerTransactionId",
                table: "Transaction",
                column: "BrokerTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transaction_BrokerTransactionId",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "BrokerTransactionId",
                table: "Transaction");
        }
    }
}
