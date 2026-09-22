using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiiboRewards.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAmiiboMappingMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MappingSource",
                table: "Amiibo",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MappingStatus",
                table: "Amiibo",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MappingSource",
                table: "Amiibo");

            migrationBuilder.DropColumn(
                name: "MappingStatus",
                table: "Amiibo");
        }
    }
}
