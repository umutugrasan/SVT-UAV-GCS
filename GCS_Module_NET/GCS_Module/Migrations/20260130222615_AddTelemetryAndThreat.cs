using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCS_Module.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryAndThreat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Targets_Id",
                table: "DetectedTargets");

            migrationBuilder.DropColumn(
                name: "FrameId",
                table: "DetectedTargets");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DetectedTargets");

            migrationBuilder.AlterColumn<string>(
                name: "GpsCoords",
                table: "DetectedTargets",
                type: "nvarchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)");

            migrationBuilder.AddColumn<double>(
                name: "Altitude",
                table: "DetectedTargets",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Battery",
                table: "DetectedTargets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsThreat",
                table: "DetectedTargets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Signal",
                table: "DetectedTargets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Speed",
                table: "DetectedTargets",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TrackId",
                table: "DetectedTargets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Targets_IsThreat",
                table: "DetectedTargets",
                column: "IsThreat");

            migrationBuilder.CreateIndex(
                name: "IX_Targets_TrackId",
                table: "DetectedTargets",
                column: "TrackId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Targets_IsThreat",
                table: "DetectedTargets");

            migrationBuilder.DropIndex(
                name: "IX_Targets_TrackId",
                table: "DetectedTargets");

            migrationBuilder.DropColumn(
                name: "Altitude",
                table: "DetectedTargets");

            migrationBuilder.DropColumn(
                name: "Battery",
                table: "DetectedTargets");

            migrationBuilder.DropColumn(
                name: "IsThreat",
                table: "DetectedTargets");

            migrationBuilder.DropColumn(
                name: "Signal",
                table: "DetectedTargets");

            migrationBuilder.DropColumn(
                name: "Speed",
                table: "DetectedTargets");

            migrationBuilder.DropColumn(
                name: "TrackId",
                table: "DetectedTargets");

            migrationBuilder.AlterColumn<string>(
                name: "GpsCoords",
                table: "DetectedTargets",
                type: "nvarchar(50)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FrameId",
                table: "DetectedTargets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "DetectedTargets",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Targets_Id",
                table: "DetectedTargets",
                column: "Id",
                unique: true);
        }
    }
}
