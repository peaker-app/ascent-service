using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AscentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialAscentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ascents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    peak_id = table.Column<Guid>(type: "uuid", nullable: false),
                    peak_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    peak_altitude_m = table.Column<int>(type: "integer", nullable: false),
                    ascent_date = table.Column<DateOnly>(type: "date", nullable: false),
                    companions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    route_notes = table.Column<string>(type: "text", nullable: true),
                    visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Public"),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    conditions = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ascents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "processed_messages",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_messages", x => x.message_id);
                });

            migrationBuilder.CreateTable(
                name: "ascent_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cloudinary_public_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    secure_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<short>(type: "smallint", nullable: false),
                    uploaded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ascent_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ascent_photos", x => x.id);
                    table.ForeignKey(
                        name: "FK_ascent_photos_ascents_ascent_id",
                        column: x => x.ascent_id,
                        principalTable: "ascents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_ascent_photo_position",
                table: "ascent_photos",
                columns: new[] { "ascent_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ascents_peak",
                table: "ascents",
                column: "peak_id");

            migrationBuilder.CreateIndex(
                name: "ix_ascents_user_date",
                table: "ascents",
                columns: new[] { "user_id", "ascent_date" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_processed_at_utc",
                table: "outbox_messages",
                column: "processed_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ascent_photos");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "processed_messages");

            migrationBuilder.DropTable(
                name: "ascents");
        }
    }
}
