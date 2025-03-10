using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class musicdataFilePathInsteadOfFileRefObj : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MusicData_Files_FileReferenceId",
                table: "MusicData");

            migrationBuilder.DropIndex(
                name: "IX_MusicData_FileReferenceId",
                table: "MusicData");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "MusicData",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "MusicData");

            migrationBuilder.CreateIndex(
                name: "IX_MusicData_FileReferenceId",
                table: "MusicData",
                column: "FileReferenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_MusicData_Files_FileReferenceId",
                table: "MusicData",
                column: "FileReferenceId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
