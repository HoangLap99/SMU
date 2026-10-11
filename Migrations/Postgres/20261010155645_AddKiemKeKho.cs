using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMU.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddKiemKeKho : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KiemKeKho",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    MaPhieuKiemKe = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NgayKiemKe = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ThoiGianKiemKe = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LoaiXeId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    LoaiPhuongTien = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DiaDiemKiemKe = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TruongDoanKiemKe = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CanBoLapBienBan = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    TongTonHeThong = table.Column<int>(type: "integer", nullable: false),
                    TongThucTe = table.Column<int>(type: "integer", nullable: false),
                    TongChenhLech = table.Column<int>(type: "integer", nullable: false),
                    TrangThai = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_KiemKeKho", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KiemKeKhoChiTietBien",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    KiemKeKhoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    KiemKeKhoNhomDauBienId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    DauBien = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BienKiemSoat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    KhoBienSoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    LoaiBien = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TrangThaiKiemKe = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiemKeKhoChiTietBien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KiemKeKhoChiTietBien_KiemKeKho_KiemKeKhoId",
                        column: x => x.KiemKeKhoId,
                        principalTable: "KiemKeKho",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KiemKeKhoNhomDauBien",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    KiemKeKhoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    STT = table.Column<int>(type: "integer", nullable: false),
                    DauBien = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MauBienSoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    MauBien = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MauBienHex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TonHeThong = table.Column<int>(type: "integer", nullable: false),
                    ThucTe = table.Column<int>(type: "integer", nullable: false),
                    ChenhLech = table.Column<int>(type: "integer", nullable: false),
                    SoBienCoMat = table.Column<int>(type: "integer", nullable: false),
                    SoBienThieu = table.Column<int>(type: "integer", nullable: false),
                    SoBienThua = table.Column<int>(type: "integer", nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiemKeKhoNhomDauBien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KiemKeKhoNhomDauBien_KiemKeKho_KiemKeKhoId",
                        column: x => x.KiemKeKhoId,
                        principalTable: "KiemKeKho",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeKho_CustomerId_MaPhieuKiemKe",
                table: "KiemKeKho",
                columns: new[] { "CustomerId", "MaPhieuKiemKe" });

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeKho_NgayKiemKe",
                table: "KiemKeKho",
                column: "NgayKiemKe");

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeKhoChiTietBien_BienKiemSoat",
                table: "KiemKeKhoChiTietBien",
                column: "BienKiemSoat");

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeKhoChiTietBien_CustomerId_KiemKeKhoId",
                table: "KiemKeKhoChiTietBien",
                columns: new[] { "CustomerId", "KiemKeKhoId" });

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeKhoChiTietBien_KhoBienSoId",
                table: "KiemKeKhoChiTietBien",
                column: "KhoBienSoId");

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeKhoChiTietBien_KiemKeKhoId",
                table: "KiemKeKhoChiTietBien",
                column: "KiemKeKhoId");

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeKhoNhomDauBien_CustomerId_KiemKeKhoId",
                table: "KiemKeKhoNhomDauBien",
                columns: new[] { "CustomerId", "KiemKeKhoId" });

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeKhoNhomDauBien_DauBien",
                table: "KiemKeKhoNhomDauBien",
                column: "DauBien");

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeKhoNhomDauBien_KiemKeKhoId",
                table: "KiemKeKhoNhomDauBien",
                column: "KiemKeKhoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KiemKeKhoChiTietBien");

            migrationBuilder.DropTable(
                name: "KiemKeKhoNhomDauBien");

            migrationBuilder.DropTable(
                name: "KiemKeKho");
        }
    }
}
