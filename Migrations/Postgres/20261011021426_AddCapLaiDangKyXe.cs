using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMU.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddCapLaiDangKyXe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CapLaiDangKyXe",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    KyHieuQuyenBienLaiId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    KyHieuQuyen = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SoBienLai = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NgayBienLai = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BienKiemSoat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LoaiXeId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    LoaiPhuongTien = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MucThuLePhiId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    NoiDungThuPhi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SoTienThu = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    HinhThucThuTien = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HoTenChuXe = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    SoCccd = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MaHoSoDvc = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LyDoCapLai = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TrangThai = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DaXacNhan = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXacNhan = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NguoiXacNhan = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapLaiDangKyXe", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CapLaiDangKyXe_CustomerId_BienKiemSoat",
                table: "CapLaiDangKyXe",
                columns: new[] { "CustomerId", "BienKiemSoat" });

            migrationBuilder.CreateIndex(
                name: "IX_CapLaiDangKyXe_CustomerId_SoBienLai",
                table: "CapLaiDangKyXe",
                columns: new[] { "CustomerId", "SoBienLai" });

            migrationBuilder.CreateIndex(
                name: "IX_CapLaiDangKyXe_KyHieuQuyenBienLaiId_SoBienLai",
                table: "CapLaiDangKyXe",
                columns: new[] { "KyHieuQuyenBienLaiId", "SoBienLai" });

            migrationBuilder.CreateIndex(
                name: "IX_CapLaiDangKyXe_NgayBienLai",
                table: "CapLaiDangKyXe",
                column: "NgayBienLai");

            migrationBuilder.CreateIndex(
                name: "IX_CapLaiDangKyXe_TrangThai",
                table: "CapLaiDangKyXe",
                column: "TrangThai");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapLaiDangKyXe");
        }
    }
}
