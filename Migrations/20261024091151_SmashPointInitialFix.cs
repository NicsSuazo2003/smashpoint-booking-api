using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmashPointBookingAPI.Migrations
{
    /// <inheritdoc />
    public partial class SmashPointInitialFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "available_amenities",
                table: "clients");

            migrationBuilder.AddColumn<string>(
                name: "available_amenities_json",
                table: "clients",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_advance_booking_days",
                table: "clients",
                type: "integer",
                nullable: false,
                defaultValue: 90);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "available_amenities_json",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "max_advance_booking_days",
                table: "clients");

            migrationBuilder.AddColumn<string>(
                name: "available_amenities",
                table: "clients",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
