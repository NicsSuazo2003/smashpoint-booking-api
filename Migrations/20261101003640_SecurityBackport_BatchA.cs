using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmashPointBookingAPI.Migrations
{
    /// <inheritdoc />
    public partial class SecurityBackport_BatchA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_timeslots_CourtId",
                table: "timeslots");

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutUntil",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetExpiresAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                table: "users",
                type: "text",
                nullable: true);

            // Postgres requires an explicit USING cast for text->jsonb.
            // The table is empty, so this is safe.
            migrationBuilder.Sql(
                @"ALTER TABLE security_events
                  ALTER COLUMN ""Metadata"" TYPE jsonb
                  USING ""Metadata""::jsonb;");

            migrationBuilder.AddColumn<DateTime>(
                name: "booking_close_date",
                table: "clients",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "booking_open_date",
                table: "clients",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "booking_window_mode",
                table: "clients",
                type: "text",
                nullable: false,
                defaultValue: "rolling");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentExpiresAt",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.CreateIndex(
                name: "IX_timeslots_CourtId_Date_StartTime",
                table: "timeslots",
                columns: new[] { "CourtId", "Date", "StartTime" },
                unique: true,
                filter: "\"CourtId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_secevents_client_time",
                table: "security_events",
                columns: new[] { "ClientId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "ix_secevents_type_time",
                table: "security_events",
                columns: new[] { "ClientId", "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_ReferenceCode",
                table: "bookings",
                column: "ReferenceCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_timeslots_CourtId_Date_StartTime",
                table: "timeslots");

            migrationBuilder.DropIndex(
                name: "ix_secevents_client_time",
                table: "security_events");

            migrationBuilder.DropIndex(
                name: "ix_secevents_type_time",
                table: "security_events");

            migrationBuilder.DropIndex(
                name: "IX_bookings_ReferenceCode",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LockoutUntil",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordResetExpiresAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "booking_close_date",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "booking_open_date",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "booking_window_mode",
                table: "clients");

            migrationBuilder.Sql(
                @"ALTER TABLE security_events
                  ALTER COLUMN ""Metadata"" TYPE text
                  USING ""Metadata""::text;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentExpiresAt",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_timeslots_CourtId",
                table: "timeslots",
                column: "CourtId");
        }
    }
}