using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMU.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddChiTietDotNhapVaKhoaPhieu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChiTietDotNhapBienSoId",
                table: "KhoBienSo",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrangThaiBienSoId",
                table: "KhoBienSo",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DaNhapKho",
                table: "DotNhapKho",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayNhapKhoThucTe",
                table: "DotNhapKho",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiNhapKho",
                table: "DotNhapKho",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "DotNhapKho",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ChiTietDotNhapBienSo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    DotNhapKhoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    BienKiemSoat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TrangThaiBienSoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    TrangThaiBienSoTen = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MauSacHienThi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LoaiXeId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    MauBienSoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    DauBienSoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    GhiChu = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDotNhapBienSo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDotNhapBienSo_BienKiemSoat",
                table: "ChiTietDotNhapBienSo",
                column: "BienKiemSoat");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDotNhapBienSo_CustomerId_DotNhapKhoId",
                table: "ChiTietDotNhapBienSo",
                columns: new[] { "CustomerId", "DotNhapKhoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietDotNhapBienSo");

            migrationBuilder.DropColumn(
                name: "ChiTietDotNhapBienSoId",
                table: "KhoBienSo");

            migrationBuilder.DropColumn(
                name: "TrangThaiBienSoId",
                table: "KhoBienSo");

            migrationBuilder.DropColumn(
                name: "DaNhapKho",
                table: "DotNhapKho");

            migrationBuilder.DropColumn(
                name: "NgayNhapKhoThucTe",
                table: "DotNhapKho");

            migrationBuilder.DropColumn(
                name: "NguoiNhapKho",
                table: "DotNhapKho");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "DotNhapKho");
        }
    }
}
