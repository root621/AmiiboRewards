using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiiboRewards.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAmiiboCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AmiiboCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceFile = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CharacterId = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CharacterBaseId = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    SeriesId = table.Column<byte>(type: "smallint", nullable: false),
                    NumberingId = table.Column<int>(type: "integer", nullable: false),
                    NfpType = table.Column<byte>(type: "smallint", nullable: false),
                    Version = table.Column<byte>(type: "smallint", nullable: false),
                    SourceSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmiiboCatalog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AmiiboCatalog_CharacterBaseId",
                table: "AmiiboCatalog",
                column: "CharacterBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_AmiiboCatalog_CharacterId",
                table: "AmiiboCatalog",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_AmiiboCatalog_NumberingId",
                table: "AmiiboCatalog",
                column: "NumberingId");

            migrationBuilder.CreateIndex(
                name: "IX_AmiiboCatalog_SourceFile",
                table: "AmiiboCatalog",
                column: "SourceFile",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmiiboCatalog");
        }
    }
}
