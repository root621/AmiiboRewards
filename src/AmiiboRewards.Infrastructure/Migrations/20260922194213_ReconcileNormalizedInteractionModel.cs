using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiiboRewards.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileNormalizedInteractionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Rewards",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Probability",
                table: "AmiiboRewards",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "InteractionKind",
                table: "AmiiboRewards",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxCount",
                table: "AmiiboRewards",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinCount",
                table: "AmiiboRewards",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutcomeKind",
                table: "AmiiboRewards",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "RawWeight",
                table: "AmiiboRewards",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialMetadata",
                table: "AmiiboRewards",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "InteractionKind",
                table: "AmiiboRewards");

            migrationBuilder.DropColumn(
                name: "MaxCount",
                table: "AmiiboRewards");

            migrationBuilder.DropColumn(
                name: "MinCount",
                table: "AmiiboRewards");

            migrationBuilder.DropColumn(
                name: "OutcomeKind",
                table: "AmiiboRewards");

            migrationBuilder.DropColumn(
                name: "RawWeight",
                table: "AmiiboRewards");

            migrationBuilder.DropColumn(
                name: "SpecialMetadata",
                table: "AmiiboRewards");

            migrationBuilder.AlterColumn<decimal>(
                name: "Probability",
                table: "AmiiboRewards",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,2)",
                oldPrecision: 7,
                oldScale: 2);
        }
    }
}
