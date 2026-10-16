using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMU.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddLogHoatDong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""LichSuGoiThueBao"" (
                    ""Id"" character varying(36) NOT NULL,
                    ""CustomerId"" character varying(36) NOT NULL,
                    ""CustomerCode"" character varying(50),
                    ""CustomerName"" character varying(250),
                    ""GoiThueBaoId"" character varying(36) NOT NULL,
                    ""GoiThueBaoMa"" character varying(50),
                    ""GoiThueBaoTen"" character varying(250) NOT NULL,
                    ""GioiHanUser"" integer NOT NULL DEFAULT 1,
                    ""LoaiGiaoDich"" character varying(50) NOT NULL DEFAULT 'GiaHan',
                    ""LoaiGiaoDichTen"" character varying(100),
                    ""HinhThucGiaHan"" character varying(100),
                    ""SoThang"" integer,
                    ""NgayBatDau"" timestamp without time zone NOT NULL,
                    ""NgayKetThuc"" timestamp without time zone NOT NULL,
                    ""NgayKetThucCu"" timestamp without time zone,
                    ""SoTien"" numeric(18,2),
                    ""SoHopDong"" character varying(100),
                    ""GhiChu"" character varying(1000),
                    ""TrangThai"" character varying(50) NOT NULL DEFAULT 'HieuLuc',
                    ""NguoiThucHien"" character varying(250),
                    ""Created"" timestamp without time zone NOT NULL,
                    ""CreatedById"" character varying(36),
                    ""CreatedByName"" text,
                    ""Modified"" timestamp without time zone NOT NULL,
                    ""ModifileById"" character varying(36),
                    ""ModifileByName"" text,
                    CONSTRAINT ""PK_LichSuGoiThueBao"" PRIMARY KEY (""Id"")
                );

                CREATE INDEX IF NOT EXISTS ""IX_LichSuGoiThueBao_Created"" ON ""LichSuGoiThueBao"" (""Created"");
                CREATE INDEX IF NOT EXISTS ""IX_LichSuGoiThueBao_CustomerId"" ON ""LichSuGoiThueBao"" (""CustomerId"");

                CREATE TABLE IF NOT EXISTS ""LogHoatDong"" (
                    ""Id"" character varying(36) NOT NULL,
                    ""CustomerId"" character varying(36),
                    ""CustomerName"" character varying(250),
                    ""LoaiNghiepVu"" character varying(50) NOT NULL,
                    ""LoaiNghiepVuTen"" character varying(100) NOT NULL,
                    ""HanhDong"" character varying(50) NOT NULL,
                    ""HanhDongTen"" character varying(150) NOT NULL,
                    ""ChungTuId"" character varying(36),
                    ""MaChungTu"" character varying(100) NOT NULL,
                    ""BienKiemSoat"" character varying(250),
                    ""NoiDung"" character varying(2000) NOT NULL,
                    ""ChiTietThayDoi"" text,
                    ""TrangThai"" character varying(50) NOT NULL,
                    ""TrangThaiTen"" character varying(100) NOT NULL,
                    ""ThoiGian"" timestamp without time zone NOT NULL,
                    ""NguoiThucHienId"" character varying(36),
                    ""NguoiThucHien"" character varying(250),
                    ""IpAddress"" character varying(100),
                    ""GhiChu"" character varying(1000),
                    ""Created"" timestamp without time zone NOT NULL,
                    ""CreatedById"" character varying(36),
                    ""CreatedByName"" text,
                    ""Modified"" timestamp without time zone NOT NULL,
                    ""ModifileById"" character varying(36),
                    ""ModifileByName"" text,
                    CONSTRAINT ""PK_LogHoatDong"" PRIMARY KEY (""Id"")
                );

                CREATE INDEX IF NOT EXISTS ""IX_LogHoatDong_BienKiemSoat"" ON ""LogHoatDong"" (""BienKiemSoat"");
                CREATE INDEX IF NOT EXISTS ""IX_LogHoatDong_ChungTuId"" ON ""LogHoatDong"" (""ChungTuId"");
                CREATE INDEX IF NOT EXISTS ""IX_LogHoatDong_CustomerId"" ON ""LogHoatDong"" (""CustomerId"");
                CREATE INDEX IF NOT EXISTS ""IX_LogHoatDong_HanhDong"" ON ""LogHoatDong"" (""HanhDong"");
                CREATE INDEX IF NOT EXISTS ""IX_LogHoatDong_LoaiNghiepVu"" ON ""LogHoatDong"" (""LoaiNghiepVu"");
                CREATE INDEX IF NOT EXISTS ""IX_LogHoatDong_MaChungTu"" ON ""LogHoatDong"" (""MaChungTu"");
                CREATE INDEX IF NOT EXISTS ""IX_LogHoatDong_ThoiGian"" ON ""LogHoatDong"" (""ThoiGian"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS ""LogHoatDong"";
                DROP TABLE IF EXISTS ""LichSuGoiThueBao"";
            ");
        }
    }
}
