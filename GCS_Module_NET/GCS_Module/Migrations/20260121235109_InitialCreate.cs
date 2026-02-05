using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCS_Module.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DetectedTargets",
                columns: table => new
                {
                    DbId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PixelCoords = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    GpsCoords = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FrameId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectedTargets", x => x.DbId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Targets_Id",
                table: "DetectedTargets",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Targets_Timestamp",
                table: "DetectedTargets",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Targets_Type",
                table: "DetectedTargets",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetectedTargets");
        }
    }
}
