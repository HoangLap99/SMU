using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMU.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddXuatKhoBienSo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DauBienSoIds",
                table: "DotNhapKho");

            migrationBuilder.AddColumn<bool>(
                name: "DaXuatKho",
                table: "KhoBienSo",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LaBienDauGia",
                table: "KhoBienSo",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayXuatKho",
                table: "KhoBienSo",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XuatKhoId",
                table: "KhoBienSo",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "KyHieuDaiDauBien",
                table: "DotNhapKho",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LaBienDauGia",
                table: "ChiTietDotNhapBienSo",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "XuatKhoBienSo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    LoaiThuTucId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    LoaiThuTucTen = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    NgayThucHien = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TinhChatXuatKho = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    KhoBienSoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    BienKiemSoat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LoaiXeId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    LoaiXeTen = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MauBienSoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    MauBienSoTen = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DauBienSoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    DauBienSoTen = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MucThuLePhiId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    MucThuLePhiTen = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    KhuVucApDungId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    KhuVucApDungTen = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    MucThuLePhiKhuVucId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    DieuKienTriGiaXe = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CanCuPhapLy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SoTienTuDongDien = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KyHieuQuyenBienLaiId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    KyHieuQuyen = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SoBienLai = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NgayBienLai = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SoTienThu = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    HoTenNguoiNop = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    SoCccd = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MaHoSoDvc = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GhiChu = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TrangThai = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XuatKhoBienSo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XuatKhoBienSo_CustomerId_BienKiemSoat",
                table: "XuatKhoBienSo",
                columns: new[] { "CustomerId", "BienKiemSoat" });

            migrationBuilder.CreateIndex(
                name: "IX_XuatKhoBienSo_CustomerId_SoBienLai",
                table: "XuatKhoBienSo",
                columns: new[] { "CustomerId", "SoBienLai" });

            migrationBuilder.CreateIndex(
                name: "IX_XuatKhoBienSo_KhoBienSoId",
                table: "XuatKhoBienSo",
                column: "KhoBienSoId");

            migrationBuilder.CreateIndex(
                name: "IX_XuatKhoBienSo_NgayThucHien",
                table: "XuatKhoBienSo",
                column: "NgayThucHien");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "XuatKhoBienSo");

            migrationBuilder.DropColumn(
                name: "DaXuatKho",
                table: "KhoBienSo");

            migrationBuilder.DropColumn(
                name: "LaBienDauGia",
                table: "KhoBienSo");

            migrationBuilder.DropColumn(
                name: "NgayXuatKho",
                table: "KhoBienSo");

            migrationBuilder.DropColumn(
                name: "XuatKhoId",
                table: "KhoBienSo");

            migrationBuilder.DropColumn(
                name: "LaBienDauGia",
                table: "ChiTietDotNhapBienSo");

            migrationBuilder.AlterColumn<string>(
                name: "KyHieuDaiDauBien",
                table: "DotNhapKho",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DauBienSoIds",
                table: "DotNhapKho",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
