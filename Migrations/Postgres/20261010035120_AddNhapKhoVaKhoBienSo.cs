using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMU.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddNhapKhoVaKhoBienSo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DotNhapKho",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    MaDotNhap = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NgayNhapKho = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LoaiXeId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    MauBienSoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    DauBienSoIds = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    KyHieuDaiDauBien = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CanBoTiepNhan = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    TinhChatDotNhapId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    SoCongVan = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    GhiChu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TongSoBien = table.Column<int>(type: "integer", nullable: false),
                    SoBienDuocBam = table.Column<int>(type: "integer", nullable: false),
                    SoBienDaBam = table.Column<int>(type: "integer", nullable: false),
                    SoBienChoDauGia = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotNhapKho", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KhoBienSo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    DotNhapKhoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    BienKiemSoat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TrangThai = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TrangThaiTen = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LoaiXeId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    MauBienSoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    DauBienSoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    GhiChu = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NgayNhapKho = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhoBienSo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DotNhapKho_CustomerId_MaDotNhap",
                table: "DotNhapKho",
                columns: new[] { "CustomerId", "MaDotNhap" });

            migrationBuilder.CreateIndex(
                name: "IX_DotNhapKho_NgayNhapKho",
                table: "DotNhapKho",
                column: "NgayNhapKho");

            migrationBuilder.CreateIndex(
                name: "IX_KhoBienSo_CustomerId_BienKiemSoat",
                table: "KhoBienSo",
                columns: new[] { "CustomerId", "BienKiemSoat" });

            migrationBuilder.CreateIndex(
                name: "IX_KhoBienSo_DotNhapKhoId",
                table: "KhoBienSo",
                column: "DotNhapKhoId");

            migrationBuilder.CreateIndex(
                name: "IX_KhoBienSo_TrangThai",
                table: "KhoBienSo",
                column: "TrangThai");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DotNhapKho");

            migrationBuilder.DropTable(
                name: "KhoBienSo");
        }
    }
}
