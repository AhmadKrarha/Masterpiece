using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materpiece.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEvSmartChargingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PowerOutputKw",
                table: "ChargerSlots",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerKwh",
                table: "ChargerSlots",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "BatteryCapacityKwh",
                table: "Bookings",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EnergyRequestedKwh",
                table: "Bookings",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "RatePricePerKwh",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "StartingPercentage",
                table: "Bookings",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TargetPercentage",
                table: "Bookings",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PowerOutputKw",
                table: "ChargerSlots");

            migrationBuilder.DropColumn(
                name: "PricePerKwh",
                table: "ChargerSlots");

            migrationBuilder.DropColumn(
                name: "BatteryCapacityKwh",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "EnergyRequestedKwh",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RatePricePerKwh",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "StartingPercentage",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TargetPercentage",
                table: "Bookings");
        }
    }
}
