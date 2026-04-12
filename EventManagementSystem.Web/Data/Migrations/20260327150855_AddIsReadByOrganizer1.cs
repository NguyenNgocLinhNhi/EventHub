using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagementSystem.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsReadByOrganizer1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReadByOrganizer",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReadByOrganizer",
                table: "BookingDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReadByOrganizer",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsReadByOrganizer",
                table: "BookingDetails");
        }
    }
}
