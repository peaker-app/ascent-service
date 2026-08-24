using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AscentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedUsersAndPhotoPositionCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deleted_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deleted_users", x => x.id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_ascent_photo_position",
                table: "ascent_photos",
                sql: "position BETWEEN 0 AND 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deleted_users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_ascent_photo_position",
                table: "ascent_photos");
        }
    }
}
