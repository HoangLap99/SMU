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
            migrationBuilder.Sql("ALTER TABLE \"XuatKhoBienSo\" ADD COLUMN IF NOT EXISTS \"DaXuatKho\" boolean NOT NULL DEFAULT FALSE;");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_XuatKhoBienSo_DaXuatKho\" ON \"XuatKhoBienSo\" (\"DaXuatKho\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_XuatKhoBienSo_TrangThai\" ON \"XuatKhoBienSo\" (\"TrangThai\");");
            migrationBuilder.Sql("ALTER TABLE \"KiemKeKho\" ADD COLUMN IF NOT EXISTS \"XacNhanHoanTat\" boolean NOT NULL DEFAULT FALSE;");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_KiemKeKho_XacNhanHoanTat\" ON \"KiemKeKho\" (\"XacNhanHoanTat\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_KiemKeKho_TrangThai\" ON \"KiemKeKho\" (\"TrangThai\");");
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
