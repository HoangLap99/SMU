using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMU.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    TrialStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CapBac",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    NhomCapBac = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HeSoThamChieu = table.Column<double>(type: "double precision", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MoTa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapBac", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GoiThueBaoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    GioiHanUser = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DauBienSo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    KyHieuSeries = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MaTinhThanh = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MoTa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DauBienSo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoiThueBao",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    GioiHanUser = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MoTa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoiThueBao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KhuVucApDung",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    PhamVi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HeSoPhi = table.Column<double>(type: "double precision", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MoTa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhuVucApDung", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KyHieuQuyenBienLai",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    TuSo = table.Column<long>(type: "bigint", nullable: true),
                    DenSo = table.Column<long>(type: "bigint", nullable: true),
                    SoHienTai = table.Column<long>(type: "bigint", nullable: true),
                    NamPhatHanh = table.Column<int>(type: "integer", nullable: true),
                    DonViPhatHanh = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MoTa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KyHieuQuyenBienLai", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoaiThuTuc",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    ThoiHanXuLy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LePhiMacDinh = table.Column<decimal>(type: "numeric", nullable: true),
                    CanCuPhapLy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MoTa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiThuTuc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoaiXe",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    NhomXe = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SoChoNgoiToiDa = table.Column<int>(type: "integer", nullable: true),
                    TaiTrongToiDa = table.Column<double>(type: "double precision", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MoTa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiXe", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MauBienSo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    MaMauHex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DoiTuongSuDung = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MoTa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MauBienSo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MucThuLePhi",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    LoaiXeId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    NhomXe = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LoaiThuTucId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    PhanLoaiThuTuc = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    KhoangTriGiaXe = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CanCuPhapLy = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    BatBuocChonTriGiaXe = table.Column<bool>(type: "boolean", nullable: false),
                    GhiChuHuongDan = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MoTa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MucThuLePhi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TinhChatDotNhap",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    LoaiDotNhap = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MoTa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TinhChatDotNhap", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrangThaiBienSo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    ChoPhepCapMoi = table.Column<bool>(type: "boolean", nullable: false),
                    MauSacHienThi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MoTa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrangThaiBienSo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    FullName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CapBacId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    SoHieuCAND = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChucVu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VaiTroPhanQuyenId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Email = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VaiTroPhanQuyen",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    QuyenHan = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MoTa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaiTroPhanQuyen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MucThuLePhikhuVuc",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    MucThuLePhiId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    KhuVucApDungId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    MucThu = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifileById = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifileByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MucThuLePhikhuVuc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MucThuLePhikhuVuc_MucThuLePhi_MucThuLePhiId",
                        column: x => x.MucThuLePhiId,
                        principalTable: "MucThuLePhi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Username",
                table: "Accounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CapBac_CustomerId_Ma",
                table: "CapBac",
                columns: new[] { "CustomerId", "Ma" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Code",
                table: "Customer",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DauBienSo_CustomerId_Ma",
                table: "DauBienSo",
                columns: new[] { "CustomerId", "Ma" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoiThueBao_CustomerId_Ma",
                table: "GoiThueBao",
                columns: new[] { "CustomerId", "Ma" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KhuVucApDung_CustomerId_Ma",
                table: "KhuVucApDung",
                columns: new[] { "CustomerId", "Ma" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KyHieuQuyenBienLai_CustomerId_Ma",
                table: "KyHieuQuyenBienLai",
                columns: new[] { "CustomerId", "Ma" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoaiThuTuc_CustomerId_Ma",
                table: "LoaiThuTuc",
                columns: new[] { "CustomerId", "Ma" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoaiXe_CustomerId_Ma",
                table: "LoaiXe",
                columns: new[] { "CustomerId", "Ma" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MauBienSo_CustomerId_Ma",
                table: "MauBienSo",
                columns: new[] { "CustomerId", "Ma" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MucThuLePhi_CustomerId_Ma",
                table: "MucThuLePhi",
                columns: new[] { "CustomerId", "Ma" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MucThuLePhikhuVuc_MucThuLePhiId_KhuVucApDungId",
                table: "MucThuLePhikhuVuc",
                columns: new[] { "MucThuLePhiId", "KhuVucApDungId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TinhChatDotNhap_CustomerId_Ma",
                table: "TinhChatDotNhap",
                columns: new[] { "CustomerId", "Ma" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrangThaiBienSo_CustomerId_Ma",
                table: "TrangThaiBienSo",
                columns: new[] { "CustomerId", "Ma" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaiTroPhanQuyen_CustomerId_Ma",
                table: "VaiTroPhanQuyen",
                columns: new[] { "CustomerId", "Ma" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "CapBac");

            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "DauBienSo");

            migrationBuilder.DropTable(
                name: "GoiThueBao");

            migrationBuilder.DropTable(
                name: "KhuVucApDung");

            migrationBuilder.DropTable(
                name: "KyHieuQuyenBienLai");

            migrationBuilder.DropTable(
                name: "LoaiThuTuc");

            migrationBuilder.DropTable(
                name: "LoaiXe");

            migrationBuilder.DropTable(
                name: "MauBienSo");

            migrationBuilder.DropTable(
                name: "MucThuLePhikhuVuc");

            migrationBuilder.DropTable(
                name: "TinhChatDotNhap");

            migrationBuilder.DropTable(
                name: "TrangThaiBienSo");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "VaiTroPhanQuyen");

            migrationBuilder.DropTable(
                name: "MucThuLePhi");
        }
    }
}
