using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddS3Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StoragePath",
                table: "Files",
                newName: "S3Key");

            migrationBuilder.AddColumn<string>(
                name: "S3BucketName",
                table: "Files",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "S3BucketName",
                table: "Files");

            migrationBuilder.RenameColumn(
                name: "S3Key",
                table: "Files",
                newName: "StoragePath");
        }
    }
}
