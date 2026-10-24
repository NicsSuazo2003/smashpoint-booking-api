using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmashPointBookingAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingsEnabledToClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "bookings_enabled",
                table: "clients",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "security_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorEmail = table.Column<string>(type: "text", nullable: true),
                    IpHash = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<IPAddress>(type: "inet", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Resource = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_events", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "security_events");

            migrationBuilder.DropColumn(
                name: "bookings_enabled",
                table: "clients");
        }
    }
}
