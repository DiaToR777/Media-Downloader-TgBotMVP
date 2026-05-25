using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaDownloaderTgBotMVP.Migrations
{
    /// <inheritdoc />
    public partial class AddEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("TRUNCATE TABLE \"CachedMedias\" CASCADE;");

            migrationBuilder.Sql("ALTER TABLE \"DownloadHistories\" ALTER COLUMN \"Status\" TYPE integer USING 0;");
            migrationBuilder.Sql("ALTER TABLE \"CachedMedias\" ALTER COLUMN \"Quality\" TYPE integer USING 0;");
            migrationBuilder.Sql("ALTER TABLE \"CachedMedias\" ALTER COLUMN \"FileType\" TYPE integer USING 0;");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DownloadHistories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Quality",
                table: "CachedMedias",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "FileType",
                table: "CachedMedias",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
