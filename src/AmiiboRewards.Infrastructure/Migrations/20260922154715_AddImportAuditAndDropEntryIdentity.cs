using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiiboRewards.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportAuditAndDropEntryIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AmiiboRewards",
                table: "AmiiboRewards");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionSourceSha256",
                table: "Rewards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameSourceSha256",
                table: "Rewards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "AmiiboRewards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "DropSourceSha256",
                table: "AmiiboRewards",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EntryIndex",
                table: "AmiiboRewards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AmiiboRewards",
                table: "AmiiboRewards",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ImportRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    SourcePath = table.Column<string>(type: "text", nullable: false),
                    SourceSha256 = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportRuns_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AmiiboRewards_AmiiboId_RewardId_Pool_EntryIndex",
                table: "AmiiboRewards",
                columns: new[] { "AmiiboId", "RewardId", "Pool", "EntryIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportRuns_GameId_Kind_SourceSha256",
                table: "ImportRuns",
                columns: new[] { "GameId", "Kind", "SourceSha256" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportRuns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AmiiboRewards",
                table: "AmiiboRewards");

            migrationBuilder.DropIndex(
                name: "IX_AmiiboRewards_AmiiboId_RewardId_Pool_EntryIndex",
                table: "AmiiboRewards");

            migrationBuilder.DropColumn(
                name: "DescriptionSourceSha256",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "NameSourceSha256",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AmiiboRewards");

            migrationBuilder.DropColumn(
                name: "DropSourceSha256",
                table: "AmiiboRewards");

            migrationBuilder.DropColumn(
                name: "EntryIndex",
                table: "AmiiboRewards");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AmiiboRewards",
                table: "AmiiboRewards",
                columns: new[] { "AmiiboId", "RewardId", "Pool" });
        }
    }
}
