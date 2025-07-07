using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.SubscriptionModule.Data.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class FixTypoInTrialStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TrialSart",
                table: "Subscription",
                newName: "TrialStart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TrialStart",
                table: "Subscription",
                newName: "TrialSart");
        }
    }
}
