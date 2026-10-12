using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMU.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddDaXuatKhoVaXacNhanHoanTat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DaXuatKho",
                table: "XuatKhoBienSo",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_XuatKhoBienSo_DaXuatKho",
                table: "XuatKhoBienSo",
                column: "DaXuatKho");

            migrationBuilder.CreateIndex(
                name: "IX_XuatKhoBienSo_TrangThai",
                table: "XuatKhoBienSo",
                column: "TrangThai");

            migrationBuilder.AddColumn<bool>(
                name: "XacNhanHoanTat",
                table: "KiemKeKho",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeKho_XacNhanHoanTat",
                table: "KiemKeKho",
                column: "XacNhanHoanTat");

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeKho_TrangThai",
                table: "KiemKeKho",
                column: "TrangThai");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_XuatKhoBienSo_DaXuatKho",
                table: "XuatKhoBienSo");

            migrationBuilder.DropIndex(
                name: "IX_XuatKhoBienSo_TrangThai",
                table: "XuatKhoBienSo");

            migrationBuilder.DropColumn(
                name: "DaXuatKho",
                table: "XuatKhoBienSo");

            migrationBuilder.DropIndex(
                name: "IX_KiemKeKho_XacNhanHoanTat",
                table: "KiemKeKho");

            migrationBuilder.DropIndex(
                name: "IX_KiemKeKho_TrangThai",
                table: "KiemKeKho");

            migrationBuilder.DropColumn(
                name: "XacNhanHoanTat",
                table: "KiemKeKho");
        }
    }
}
