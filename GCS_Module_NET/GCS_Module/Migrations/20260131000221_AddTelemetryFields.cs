using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCS_Module.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "FlightTime",
                table: "DetectedTargets",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Heading",
                table: "DetectedTargets",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SignalStrength",
                table: "DetectedTargets",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlightTime",
                table: "DetectedTargets");

            migrationBuilder.DropColumn(
                name: "Heading",
                table: "DetectedTargets");

            migrationBuilder.DropColumn(
                name: "SignalStrength",
                table: "DetectedTargets");
        }
    }
}
