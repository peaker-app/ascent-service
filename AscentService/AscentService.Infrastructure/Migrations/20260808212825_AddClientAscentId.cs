using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AscentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientAscentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "client_ascent_id",
                table: "ascents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_ascents_user_client_id",
                table: "ascents",
                columns: new[] { "user_id", "client_ascent_id" },
                unique: true,
                filter: "client_ascent_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_ascents_user_client_id",
                table: "ascents");

            migrationBuilder.DropColumn(
                name: "client_ascent_id",
                table: "ascents");
        }
    }
}
