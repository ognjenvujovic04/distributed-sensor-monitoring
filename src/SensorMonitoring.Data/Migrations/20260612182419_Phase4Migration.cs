using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SensorMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase4Migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastMessageId",
                table: "Sensors",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: "SENSOR-001",
                column: "LastMessageId",
                value: 0L);

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: "SENSOR-002",
                column: "LastMessageId",
                value: 0L);

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: "SENSOR-003",
                column: "LastMessageId",
                value: 0L);

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: "SENSOR-004",
                column: "LastMessageId",
                value: 0L);

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: "SENSOR-005",
                column: "LastMessageId",
                value: 0L);

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: "SENSOR-006",
                column: "LastMessageId",
                value: 0L);

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: "SENSOR-007",
                column: "LastMessageId",
                value: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMessageId",
                table: "Sensors");
        }
    }
}
