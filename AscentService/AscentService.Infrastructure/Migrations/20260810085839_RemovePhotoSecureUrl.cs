using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AscentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePhotoSecureUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "secure_url",
                table: "ascent_photos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "secure_url",
                table: "ascent_photos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
