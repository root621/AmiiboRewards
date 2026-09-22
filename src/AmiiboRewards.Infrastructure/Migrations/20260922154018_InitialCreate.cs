using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiiboRewards.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Amiibo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    InternalTableId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amiibo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Amiibo_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    InternalId = table.Column<string>(type: "text", nullable: false),
                    AssetType = table.Column<int>(type: "integer", nullable: false),
                    SourcePath = table.Column<string>(type: "text", nullable: false),
                    OutputPath = table.Column<string>(type: "text", nullable: false),
                    SourceSha256 = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    InternalId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RewardType = table.Column<int>(type: "integer", nullable: false),
                    IconAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    NameSourcePath = table.Column<string>(type: "text", nullable: true),
                    DescriptionSourcePath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rewards_Assets_IconAssetId",
                        column: x => x.IconAssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Rewards_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AmiiboRewards",
                columns: table => new
                {
                    AmiiboId = table.Column<Guid>(type: "uuid", nullable: false),
                    RewardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Pool = table.Column<string>(type: "text", nullable: false),
                    Probability = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Condition = table.Column<string>(type: "text", nullable: true),
                    DropSourcePath = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmiiboRewards", x => new { x.AmiiboId, x.RewardId, x.Pool });
                    table.ForeignKey(
                        name: "FK_AmiiboRewards_Amiibo_AmiiboId",
                        column: x => x.AmiiboId,
                        principalTable: "Amiibo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AmiiboRewards_Rewards_RewardId",
                        column: x => x.RewardId,
                        principalTable: "Rewards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Amiibo_GameId_InternalTableId",
                table: "Amiibo",
                columns: new[] { "GameId", "InternalTableId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AmiiboRewards_RewardId",
                table: "AmiiboRewards",
                column: "RewardId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_GameId_InternalId_AssetType",
                table: "Assets",
                columns: new[] { "GameId", "InternalId", "AssetType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_Code",
                table: "Games",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_GameId_InternalId",
                table: "Rewards",
                columns: new[] { "GameId", "InternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_IconAssetId",
                table: "Rewards",
                column: "IconAssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmiiboRewards");

            migrationBuilder.DropTable(
                name: "Amiibo");

            migrationBuilder.DropTable(
                name: "Rewards");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "Games");
        }
    }
}
