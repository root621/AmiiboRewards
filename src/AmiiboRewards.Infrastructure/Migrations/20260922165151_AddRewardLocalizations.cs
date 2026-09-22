using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiiboRewards.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardLocalizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RewardLocalizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RewardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SourcePath = table.Column<string>(type: "text", nullable: false),
                    SourceSha256 = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardLocalizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RewardLocalizations_Rewards_RewardId",
                        column: x => x.RewardId,
                        principalTable: "Rewards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RewardLocalizations_RewardId_Locale",
                table: "RewardLocalizations",
                columns: new[] { "RewardId", "Locale" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RewardLocalizations");
        }
    }
}
