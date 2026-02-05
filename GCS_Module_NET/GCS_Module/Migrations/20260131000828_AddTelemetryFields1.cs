using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCS_Module.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryFields1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "Battery",
                table: "DetectedTargets",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Battery",
                table: "DetectedTargets",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");
        }
    }
}
