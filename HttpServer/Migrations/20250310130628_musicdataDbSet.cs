using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class musicdataDbSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MusicMetadata_Files_FileReferenceId",
                table: "MusicMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_MusicMetadata_Users_UserId",
                table: "MusicMetadata");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MusicMetadata",
                table: "MusicMetadata");

            migrationBuilder.RenameTable(
                name: "MusicMetadata",
                newName: "MusicData");

            migrationBuilder.RenameColumn(
                name: "BPM",
                table: "MusicData",
                newName: "Bpm");

            migrationBuilder.RenameIndex(
                name: "IX_MusicMetadata_UserId",
                table: "MusicData",
                newName: "IX_MusicData_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_MusicMetadata_FileReferenceId",
                table: "MusicData",
                newName: "IX_MusicData_FileReferenceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MusicData",
                table: "MusicData",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MusicData_Files_FileReferenceId",
                table: "MusicData",
                column: "FileReferenceId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MusicData_Users_UserId",
                table: "MusicData",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MusicData_Files_FileReferenceId",
                table: "MusicData");

            migrationBuilder.DropForeignKey(
                name: "FK_MusicData_Users_UserId",
                table: "MusicData");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MusicData",
                table: "MusicData");

            migrationBuilder.RenameTable(
                name: "MusicData",
                newName: "MusicMetadata");

            migrationBuilder.RenameColumn(
                name: "Bpm",
                table: "MusicMetadata",
                newName: "BPM");

            migrationBuilder.RenameIndex(
                name: "IX_MusicData_UserId",
                table: "MusicMetadata",
                newName: "IX_MusicMetadata_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_MusicData_FileReferenceId",
                table: "MusicMetadata",
                newName: "IX_MusicMetadata_FileReferenceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MusicMetadata",
                table: "MusicMetadata",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MusicMetadata_Files_FileReferenceId",
                table: "MusicMetadata",
                column: "FileReferenceId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MusicMetadata_Users_UserId",
                table: "MusicMetadata",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
