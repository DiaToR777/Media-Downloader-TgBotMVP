using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaDownloaderTgBotMVP.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoIdAndCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CachedMedias_SourceUrl_Quality_FileType",
                table: "CachedMedias");

            migrationBuilder.AddColumn<string>(
                name: "VideoId",
                table: "CachedMedias",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CachedMedias_Composite_Lookup",
                table: "CachedMedias",
                columns: new[] { "Platform", "VideoId", "FileType", "Quality" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CachedMedias_Composite_Lookup",
                table: "CachedMedias");

            migrationBuilder.DropColumn(
                name: "VideoId",
                table: "CachedMedias");

            migrationBuilder.CreateIndex(
                name: "IX_CachedMedias_SourceUrl_Quality_FileType",
                table: "CachedMedias",
                columns: new[] { "SourceUrl", "Quality", "FileType" });
        }
    }
}
