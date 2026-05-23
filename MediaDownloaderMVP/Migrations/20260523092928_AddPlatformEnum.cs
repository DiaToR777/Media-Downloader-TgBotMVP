using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaDownloaderTgBotMVP.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"CachedMedias\" ALTER COLUMN \"Platform\" TYPE integer USING \"Platform\"::integer;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Platform",
                table: "CachedMedias",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
