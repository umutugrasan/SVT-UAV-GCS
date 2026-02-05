using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCS_Module.Migrations
{
    /// <inheritdoc />
    public partial class AddUavFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UavId",
                table: "DetectedTargets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UavName",
                table: "DetectedTargets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UavId",
                table: "DetectedTargets");

            migrationBuilder.DropColumn(
                name: "UavName",
                table: "DetectedTargets");
        }
    }
}
