using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SMU.Core.Entities;
using SMU.Core.Enums;

namespace SMU.Infrastructure.Data;

/// <summary>
/// Khởi tạo và gieo dữ liệu mặc định (Seed Data) cho hệ thống khi ứng dụng khởi động.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Khởi tạo tài khoản Admin mặc định, danh mục nghiệp vụ và dữ liệu mẫu đợt nhập kho.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        var logger = scope.ServiceProvider.GetService<ILogger<PostgresDbContext>>();

        try
        {
            // ==========================================
            // 1. TÀI KHOẢN ADMIN MẶC ĐỊNH
            // ==========================================
            const string adminUsername = "admin";
            const string adminPassword = "123456";

            var existingAccount = await dbContext.Accounts
                .FirstOrDefaultAsync(a => a.Username.ToLower() == adminUsername.ToLower());

            if (existingAccount == null)
            {
                var adminUser = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == "admin@smu.vn");

                if (adminUser == null)
                {
                    adminUser = new User
                    {
                        FullName = "Quản trị viên hệ thống",
                        Email = "admin@smu.vn",
                        PhoneNumber = "0987654321",
                        IsActive = true,
                        Created = DateTime.UtcNow,
                        Modified = DateTime.UtcNow
                    };
                    dbContext.Users.Add(adminUser);
                    await dbContext.SaveChangesAsync();
                }

                var newAccount = new Account
                {
                    UserId = adminUser.Id,
                    Username = adminUsername,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Role = UserRole.Admin,
                    IsLocked = false,
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow
                };

                dbContext.Accounts.Add(newAccount);
                await dbContext.SaveChangesAsync();

                logger?.LogInformation("Đã khởi tạo thành công tài khoản Admin mặc định ({Username} / {Password})", adminUsername, adminPassword);
            }

            // ==========================================
            // 1.1. KHÁCH HÀNG / ĐƠN VỊ MẪU VÀ TÀI KHOẢN CÁN BỘ MẪU
            // ==========================================
            const string sampleCustomerCode = "CUS-001";
            var sampleCustomer = await dbContext.Customers
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == sampleCustomerCode);

            if (sampleCustomer == null)
            {
                sampleCustomer = new Customer
                {
                    Code = sampleCustomerCode,
                    Name = "Công an phường Tây Mỗ",
                    Description = "Đơn vị quản lý mẫu",
                    GioiHanUser = 20,
                    IsActive = true,
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow
                };
                dbContext.Customers.Add(sampleCustomer);
                await dbContext.SaveChangesAsync();
                logger?.LogInformation("Đã khởi tạo khách hàng mẫu '{Code}' - {Name}", sampleCustomer.Code, sampleCustomer.Name);
            }

            const string canboUsername = "canbo01";
            const string canboPassword = "Password123@";
            var existingCanBoAccount = await dbContext.Accounts
                .FirstOrDefaultAsync(a => a.Username.ToLower() == canboUsername);

            if (existingCanBoAccount == null)
            {
                var canboUser = new User
                {
                    CustomerId = sampleCustomer.Id,
                    FullName = "Cán bộ Nguyễn Văn A",
                    Email = "canbo01@smu.vn",
                    PhoneNumber = "0912345678",
                    IsActive = true,
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow
                };
                dbContext.Users.Add(canboUser);
                await dbContext.SaveChangesAsync();

                var canboAccount = new Account
                {
                    CustomerId = sampleCustomer.Id,
                    UserId = canboUser.Id,
                    Username = canboUsername,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(canboPassword),
                    Role = UserRole.User,
                    IsLocked = false,
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow
                };
                dbContext.Accounts.Add(canboAccount);
                await dbContext.SaveChangesAsync();
                logger?.LogInformation("Đã khởi tạo tài khoản cán bộ mẫu ({Username} / {Password} / Customer: {Code})", canboUsername, canboPassword, sampleCustomerCode);
            }

            // ==========================================
            // 2. SEED DANH MỤC NGHIỆP VỤ (NẾU CHƯA CÓ)
            // ==========================================
            // 2.1. Loại xe
            var loaiXeOTo = await dbContext.LoaiXes.FirstOrDefaultAsync(x => x.Ma == "OTO");
            if (loaiXeOTo == null)
            {
                loaiXeOTo = new LoaiXe { Ma = "OTO", Ten = "Ô tô", NhomXe = "Ô tô", ThuTu = 1, IsActive = true };
                var loaiXeMay = new LoaiXe { Ma = "XEMAY", Ten = "Mô tô, Xe máy", NhomXe = "Mô tô", ThuTu = 2, IsActive = true };
                dbContext.LoaiXes.AddRange(loaiXeOTo, loaiXeMay);
                await dbContext.SaveChangesAsync();
            }

            // 2.2. Màu biển số
            var mauTrang = await dbContext.MauBienSos.FirstOrDefaultAsync(x => x.Ma == "TRANG");
            if (mauTrang == null)
            {
                mauTrang = new MauBienSo { Ma = "TRANG", Ten = "Biển Trắng", MaMauHex = "#FFFFFF", DoiTuongSuDung = "Cá nhân, tổ chức trong nước", ThuTu = 1, IsActive = true };
                var mauXanh = new MauBienSo { Ma = "XANH", Ten = "Biển Xanh", MaMauHex = "#0055D4", DoiTuongSuDung = "Cơ quan hành chính nhà nước", ThuTu = 2, IsActive = true };
                var mauVang = new MauBienSo { Ma = "VANG", Ten = "Biển Vàng", MaMauHex = "#FFCC00", DoiTuongSuDung = "Xe kinh doanh vận tải", ThuTu = 3, IsActive = true };
                var mauDo = new MauBienSo { Ma = "DO", Ten = "Biển Đỏ", MaMauHex = "#D32F2F", DoiTuongSuDung = "Xe Quân đội", ThuTu = 4, IsActive = true };
                dbContext.MauBienSos.AddRange(mauTrang, mauXanh, mauVang, mauDo);
                await dbContext.SaveChangesAsync();
            }

            // 2.3. Danh mục Đầu biển số
            var dau30B = await dbContext.DauBienSos.FirstOrDefaultAsync(x => x.Ma == "30B");
            if (dau30B == null)
            {
                dau30B = new DauBienSo { Ma = "30B", Ten = "30B (Hà Nội)", KyHieuSeries = "30B", MaTinhThanh = "01", ThuTu = 1, IsActive = true };
                var dau29RM = new DauBienSo { Ma = "29RM", Ten = "29RM (Hà Nội)", KyHieuSeries = "29RM", MaTinhThanh = "01", ThuTu = 2, IsActive = true };
                var dau29E = new DauBienSo { Ma = "29E", Ten = "29E (Hà Nội)", KyHieuSeries = "29E", MaTinhThanh = "01", ThuTu = 3, IsActive = true };
                var dau30A = new DauBienSo { Ma = "30A", Ten = "30A (Hà Nội)", KyHieuSeries = "30A", MaTinhThanh = "01", ThuTu = 4, IsActive = true };
                var dau29A = new DauBienSo { Ma = "29A", Ten = "29A (Hà Nội)", KyHieuSeries = "29A", MaTinhThanh = "01", ThuTu = 5, IsActive = true };
                var dau51A = new DauBienSo { Ma = "51A", Ten = "51A (TP. Hồ Chí Minh)", KyHieuSeries = "51A", MaTinhThanh = "79", ThuTu = 6, IsActive = true };
                var dau51F = new DauBienSo { Ma = "51F", Ten = "51F (TP. Hồ Chí Minh)", KyHieuSeries = "51F", MaTinhThanh = "79", ThuTu = 7, IsActive = true };
                var dau60A = new DauBienSo { Ma = "60A", Ten = "60A (Đồng Nai)", KyHieuSeries = "60A", MaTinhThanh = "75", ThuTu = 8, IsActive = true };
                var dau43A = new DauBienSo { Ma = "43A", Ten = "43A (Đà Nẵng)", KyHieuSeries = "43A", MaTinhThanh = "48", ThuTu = 9, IsActive = true };
                dbContext.DauBienSos.AddRange(dau30B, dau29RM, dau29E, dau30A, dau29A, dau51A, dau51F, dau60A, dau43A);
                await dbContext.SaveChangesAsync();
            }

            // 2.4. Danh mục Tính chất đợt nhập
            var tinhChatTonDau = await dbContext.TinhChatDotNhaps.FirstOrDefaultAsync(x => x.Ma == "TON_DAU");
            if (tinhChatTonDau == null)
            {
                tinhChatTonDau = new TinhChatDotNhap { Ma = "TON_DAU", Ten = "Khai báo tồn đầu", LoaiDotNhap = "Khai báo ban đầu", ThuTu = 1, IsActive = true };
                var tinhChatDinhKy = new TinhChatDotNhap { Ma = "DINH_KY", Ten = "Định kỳ theo kế hoạch", LoaiDotNhap = "Định kỳ", ThuTu = 2, IsActive = true };
                var tinhChatDotXuat = new TinhChatDotNhap { Ma = "DOT_XUAT", Ten = "Bổ sung đột xuất", LoaiDotNhap = "Đột xuất", ThuTu = 3, IsActive = true };
                dbContext.TinhChatDotNhaps.AddRange(tinhChatTonDau, tinhChatDinhKy, tinhChatDotXuat);
                await dbContext.SaveChangesAsync();
            }

            // 2.5. Danh mục Trạng thái biển số
            var ttDuocBam = await dbContext.TrangThaiBienSos.FirstOrDefaultAsync(x => x.Ma == "DUOC_BAM");
            if (ttDuocBam == null)
            {
                ttDuocBam = new TrangThaiBienSo { Ma = "DUOC_BAM", Ten = "Biển được bấm (Tồn kho)", ChoPhepCapMoi = true, MauSacHienThi = "success", ThuTu = 1, IsActive = true };
                var ttChoDauGia = new TrangThaiBienSo { Ma = "CHO_DAU_GIA", Ten = "Chờ đấu giá", ChoPhepCapMoi = false, MauSacHienThi = "warning", ThuTu = 2, IsActive = true };
                var ttDaBam = new TrangThaiBienSo { Ma = "DA_BAM", Ten = "Đã bấm (Đã cấp)", ChoPhepCapMoi = false, MauSacHienThi = "secondary", ThuTu = 3, IsActive = true };
                var ttThuHoi = new TrangThaiBienSo { Ma = "THU_HOI", Ten = "Thu hồi", ChoPhepCapMoi = false, MauSacHienThi = "danger", ThuTu = 4, IsActive = true };
                dbContext.TrangThaiBienSos.AddRange(ttDuocBam, ttChoDauGia, ttDaBam, ttThuHoi);
                await dbContext.SaveChangesAsync();
            }

            // 2.6. Danh mục Loại thủ tục
            var thuTucCapBien = await dbContext.LoaiThuTucs.FirstOrDefaultAsync(x => x.Ma == "CAP_BIEN");
            if (thuTucCapBien == null)
            {
                thuTucCapBien = new LoaiThuTuc
                {
                    Ma = "CAP_BIEN",
                    Ten = "Cấp biển (Có xuất kho số)",
                    ThoiHanXuLy = "Trong ngày",
                    LePhiMacDinh = 20000000,
                    CanCuPhapLy = "Thông tư 155/2025/TT-BTC",
                    ThuTu = 1,
                    IsActive = true
                };
                var thuTucCapDoi = new LoaiThuTuc
                {
                    Ma = "CAP_DOI",
                    Ten = "Cấp đổi biển số",
                    ThoiHanXuLy = "Trong ngày",
                    LePhiMacDinh = 100000,
                    CanCuPhapLy = "Thông tư 155/2025/TT-BTC",
                    ThuTu = 2,
                    IsActive = true
                };
                var thuTucThuHoi = new LoaiThuTuc
                {
                    Ma = "THU_HOI",
                    Ten = "Thu hồi chứng nhận đăng ký, biển số xe",
                    ThoiHanXuLy = "Trong ngày",
                    LePhiMacDinh = 0,
                    CanCuPhapLy = "Thông tư 155/2025/TT-BTC",
                    ThuTu = 3,
                    IsActive = true
                };
                dbContext.LoaiThuTucs.AddRange(thuTucCapBien, thuTucCapDoi, thuTucThuHoi);
                await dbContext.SaveChangesAsync();
            }

            // 2.7. Danh mục Khu vực áp dụng
            var khuVuc1 = await dbContext.KhuVucApDungs.FirstOrDefaultAsync(x => x.Ma == "KHU_VUC_I");
            if (khuVuc1 == null)
            {
                khuVuc1 = new KhuVucApDung
                {
                    Ma = "KHU_VUC_I",
                    Ten = "Khu vực I (Hà Nội, TP.HCM)",
                    PhamVi = "Thành phố Hà Nội và Thành phố Hồ Chí Minh",
                    HeSoPhi = 1.0,
                    ThuTu = 1,
                    IsActive = true
                };
                var khuVuc2 = new KhuVucApDung
                {
                    Ma = "KHU_VUC_II",
                    Ten = "Khu vực II (TP trực thuộc tỉnh, thị xã)",
                    PhamVi = "Các thành phố trực thuộc tỉnh, các thị xã",
                    HeSoPhi = 0.5,
                    ThuTu = 2,
                    IsActive = true
                };
                var khuVuc3 = new KhuVucApDung
                {
                    Ma = "KHU_VUC_III",
                    Ten = "Khu vực III (Các khu vực còn lại)",
                    PhamVi = "Các huyện, thị trấn, xã còn lại",
                    HeSoPhi = 0.1,
                    ThuTu = 3,
                    IsActive = true
                };
                dbContext.KhuVucApDungs.AddRange(khuVuc1, khuVuc2, khuVuc3);
                await dbContext.SaveChangesAsync();
            }

            // 2.8. Danh mục Ký hiệu quyển biên lai
            var quyenAA26E = await dbContext.KyHieuQuyenBienLais.FirstOrDefaultAsync(x => x.Ma == "AA/26E");
            if (quyenAA26E == null)
            {
                quyenAA26E = new KyHieuQuyenBienLai
                {
                    Ma = "AA/26E",
                    Ten = "Quyển AA/26E",
                    TuSo = 1,
                    DenSo = 500,
                    SoHienTai = 112,
                    NamPhatHanh = 2026,
                    DonViPhatHanh = "Cục Cảnh sát giao thông",
                    ThuTu = 1,
                    IsActive = true
                };
                var quyenAB26E = new KyHieuQuyenBienLai
                {
                    Ma = "AB/26E",
                    Ten = "Quyển AB/26E",
                    TuSo = 1,
                    DenSo = 500,
                    SoHienTai = 1,
                    NamPhatHanh = 2026,
                    DonViPhatHanh = "Cục Cảnh sát giao thông",
                    ThuTu = 2,
                    IsActive = true
                };
                dbContext.KyHieuQuyenBienLais.AddRange(quyenAA26E, quyenAB26E);
                await dbContext.SaveChangesAsync();
            }

            // 2.9. Danh mục Mức thu lệ phí theo Thông tư 155/2025/TT-BTC
            var mtlpOto01 = await dbContext.MucThuLePhis.FirstOrDefaultAsync(x => x.Ma == "MTLP_OTO_01");
            if (mtlpOto01 == null && khuVuc1 != null)
            {
                mtlpOto01 = new MucThuLePhi
                {
                    Ma = "MTLP_OTO_01",
                    Ten = "[Ô tô] Cấp mới đăng ký kèm biển số Ô tô chở người từ 9 chỗ trở xuống (Trị giá trên 40 triệu đồng)",
                    LoaiXeId = loaiXeOTo?.Id,
                    NhomXe = "Ô tô",
                    LoaiThuTucId = thuTucCapBien?.Id,
                    PhanLoaiThuTuc = "Cấp mới kèm biển số",
                    KhoangTriGiaXe = "Trị giá trên 40 triệu đồng",
                    CanCuPhapLy = "Thông tư 155/2025/TT-BTC",
                    BatBuocChonTriGiaXe = true,
                    GhiChuHuongDan = "Khu vực I áp dụng cho TP Hà Nội và TP Hồ Chí Minh",
                    ThuTu = 1,
                    IsActive = true
                };

                var mtlpOto02 = new MucThuLePhi
                {
                    Ma = "MTLP_OTO_02",
                    Ten = "[Ô tô] Cấp mới đăng ký kèm biển số Ô tô chở người từ 9 chỗ trở xuống (Trị giá từ 40 triệu đồng trở xuống)",
                    LoaiXeId = loaiXeOTo?.Id,
                    NhomXe = "Ô tô",
                    LoaiThuTucId = thuTucCapBien?.Id,
                    PhanLoaiThuTuc = "Cấp mới kèm biển số",
                    KhoangTriGiaXe = "Trị giá từ 40 triệu đồng trở xuống",
                    CanCuPhapLy = "Thông tư 155/2025/TT-BTC",
                    BatBuocChonTriGiaXe = true,
                    GhiChuHuongDan = "Khu vực I áp dụng cho TP Hà Nội và TP Hồ Chí Minh",
                    ThuTu = 2,
                    IsActive = true
                };

                dbContext.MucThuLePhis.AddRange(mtlpOto01, mtlpOto02);
                await dbContext.SaveChangesAsync();

                var kv2 = await dbContext.KhuVucApDungs.FirstOrDefaultAsync(x => x.Ma == "KHU_VUC_II");
                var kv3 = await dbContext.KhuVucApDungs.FirstOrDefaultAsync(x => x.Ma == "KHU_VUC_III");

                var chiTiets = new List<MucThuLePhiKhuVuc>
                {
                    new() { MucThuLePhiId = mtlpOto01.Id!, KhuVucApDungId = khuVuc1.Id!, MucThu = 20000000, GhiChu = "Áp dụng cho TP Hà Nội và TP.HCM" },
                    new() { MucThuLePhiId = mtlpOto02.Id!, KhuVucApDungId = khuVuc1.Id!, MucThu = 2000000, GhiChu = "Áp dụng cho TP Hà Nội và TP.HCM" }
                };

                if (kv2 != null)
                {
                    chiTiets.Add(new() { MucThuLePhiId = mtlpOto01.Id!, KhuVucApDungId = kv2.Id!, MucThu = 1000000, GhiChu = "Áp dụng cho thành phố trực thuộc tỉnh" });
                    chiTiets.Add(new() { MucThuLePhiId = mtlpOto02.Id!, KhuVucApDungId = kv2.Id!, MucThu = 1000000, GhiChu = "Áp dụng cho thành phố trực thuộc tỉnh" });
                }

                if (kv3 != null)
                {
                    chiTiets.Add(new() { MucThuLePhiId = mtlpOto01.Id!, KhuVucApDungId = kv3.Id!, MucThu = 200000, GhiChu = "Áp dụng cho các khu vực còn lại" });
                    chiTiets.Add(new() { MucThuLePhiId = mtlpOto02.Id!, KhuVucApDungId = kv3.Id!, MucThu = 200000, GhiChu = "Áp dụng cho các khu vực còn lại" });
                }

                dbContext.MucThuLePhiKhuVucs.AddRange(chiTiets);
                await dbContext.SaveChangesAsync();
            }

            // 2.10. Danh mục Thủ tục & Mức thu lệ phí cấp lại đăng ký xe (Thông tư 155/2025/TT-BTC)
            var thuTucCapLai = await dbContext.LoaiThuTucs.FirstOrDefaultAsync(x => x.Ma == "CAP_LAI");
            if (thuTucCapLai == null)
            {
                thuTucCapLai = new LoaiThuTuc
                {
                    Ma = "CAP_LAI",
                    Ten = "Cấp lại đăng ký xe (Không xuất kho, không giảm tồn biển)",
                    ThoiHanXuLy = "Trong ngày",
                    LePhiMacDinh = 50000,
                    CanCuPhapLy = "Thông tư 155/2025/TT-BTC",
                    ThuTu = 4,
                    IsActive = true
                };
                dbContext.LoaiThuTucs.Add(thuTucCapLai);
                await dbContext.SaveChangesAsync();
            }

            var mtlpCapLai = await dbContext.MucThuLePhis.FirstOrDefaultAsync(x => x.Ma == "MTLP_CAP_LAI_DKX");
            if (mtlpCapLai == null)
            {
                mtlpCapLai = new MucThuLePhi
                {
                    Ma = "MTLP_CAP_LAI_DKX",
                    Ten = "Cấp lại riêng giấy chứng nhận đăng ký xe (mất, hư hỏng - không cấp biển mới) (50.000 đ)",
                    LoaiXeId = loaiXeOTo?.Id,
                    NhomXe = "Ô tô",
                    LoaiThuTucId = thuTucCapLai.Id,
                    PhanLoaiThuTuc = "Cấp lại đăng ký",
                    CanCuPhapLy = "Thông tư 155/2025/TT-BTC",
                    BatBuocChonTriGiaXe = false,
                    GhiChuHuongDan = "Mức thu thống nhất 50.000 đ theo Thông tư 155/2025/TT-BTC",
                    ThuTu = 3,
                    IsActive = true
                };
                dbContext.MucThuLePhis.Add(mtlpCapLai);
                await dbContext.SaveChangesAsync();

                if (khuVuc1 != null)
                {
                    var kv2 = await dbContext.KhuVucApDungs.FirstOrDefaultAsync(x => x.Ma == "KHU_VUC_II");
                    var kv3 = await dbContext.KhuVucApDungs.FirstOrDefaultAsync(x => x.Ma == "KHU_VUC_III");
                    var chiTietsCapLai = new List<MucThuLePhiKhuVuc>
                    {
                        new() { MucThuLePhiId = mtlpCapLai.Id!, KhuVucApDungId = khuVuc1.Id!, MucThu = 50000, GhiChu = "Áp dụng toàn quốc" }
                    };
                    if (kv2 != null) chiTietsCapLai.Add(new() { MucThuLePhiId = mtlpCapLai.Id!, KhuVucApDungId = kv2.Id!, MucThu = 50000, GhiChu = "Áp dụng toàn quốc" });
                    if (kv3 != null) chiTietsCapLai.Add(new() { MucThuLePhiId = mtlpCapLai.Id!, KhuVucApDungId = kv3.Id!, MucThu = 50000, GhiChu = "Áp dụng toàn quốc" });
                    dbContext.MucThuLePhiKhuVucs.AddRange(chiTietsCapLai);
                    await dbContext.SaveChangesAsync();
                }
            }

            // ==========================================
            // 3. SEED ĐỢT NHẬP KHO MẪU: NK-2026-08-01-DAU
            // ==========================================
            var dotNhapTonDau = await dbContext.DotNhapKhos.FirstOrDefaultAsync(x => x.MaDotNhap == "NK-2026-08-01-DAU");
            if (dotNhapTonDau == null)
            {
                dotNhapTonDau = new DotNhapKho
                {
                    MaDotNhap = "NK-2026-08-01-DAU",
                    NgayNhapKho = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                    LoaiXeId = loaiXeOTo?.Id,
                    MauBienSoId = mauTrang?.Id,
                    KyHieuDaiDauBien = JsonSerializer.Serialize(new[] { "30B", "29RM", "29E" }),
                    CanBoTiepNhan = "Trung Tá - Mai Cao",
                    TinhChatDotNhapId = tinhChatTonDau?.Id,
                    SoCongVan = "BB-TK-0108/CAP",
                    GhiChu = "Khai báo chốt tồn ban đầu chuyển đổi số hóa hệ thống",
                    TongSoBien = 513,
                    SoBienDuocBam = 513,
                    SoBienDaBam = 0,
                    SoBienChoDauGia = 0,
                    DaNhapKho = true,
                    TrangThai = "DaNhapKho",
                    NgayNhapKhoThucTe = new DateTime(2026, 8, 1, 8, 30, 0, DateTimeKind.Utc),
                    NguoiNhapKho = "Trung Tá - Mai Cao"
                };
                dbContext.DotNhapKhos.Add(dotNhapTonDau);
                await dbContext.SaveChangesAsync();

                // Tạo danh sách 513 biển số
                var samplePlates = new List<string>
                {
                    "30B-207.66", "30B-369.41", "30B-370.08", "30B-371.12", "30B-372.58",
                    "30B-373.99", "30B-374.01", "30B-375.88", "30B-376.62", "30B-377.77"
                };

                int currentNumber = 20767;
                while (samplePlates.Count < 513)
                {
                    var numStr = currentNumber.ToString().PadLeft(5, '0');
                    samplePlates.Add($"30B-{numStr.Substring(0, 3)}.{numStr.Substring(3)}");
                    currentNumber += 13;
                }

                // A. Lưu vào danh sách biển số riêng của đợt nhập (ChiTietDotNhapBienSo)
                var chiTietList = new List<ChiTietDotNhapBienSo>();
                // B. Lưu vào bảng lưu kho chính thức (KhoBienSo)
                var khoBienSos = new List<KhoBienSo>();

                int stt = 1;
                foreach (var plate in samplePlates)
                {
                    var chiTiet = new ChiTietDotNhapBienSo
                    {
                        DotNhapKhoId = dotNhapTonDau.Id!,
                        ThuTu = stt,
                        BienKiemSoat = plate,
                        TrangThaiBienSoId = ttDuocBam?.Id,
                        TrangThaiBienSoTen = ttDuocBam?.Ten ?? "Biển được bấm (Tồn kho)",
                        MauSacHienThi = "success",
                        LoaiXeId = loaiXeOTo?.Id,
                        MauBienSoId = mauTrang?.Id,
                        DauBienSoId = dau30B?.Id,
                        GhiChu = "Khai báo tồn ban đầu theo biên bản kiểm kê"
                    };
                    chiTietList.Add(chiTiet);

                    khoBienSos.Add(new KhoBienSo
                    {
                        DotNhapKhoId = dotNhapTonDau.Id!,
                        ChiTietDotNhapBienSoId = chiTiet.Id,
                        ThuTu = stt,
                        BienKiemSoat = plate,
                        TrangThai = "DuocBam",
                        TrangThaiTen = ttDuocBam?.Ten ?? "Biển được bấm (Tồn kho)",
                        TrangThaiBienSoId = ttDuocBam?.Id,
                        LoaiXeId = loaiXeOTo?.Id,
                        MauBienSoId = mauTrang?.Id,
                        DauBienSoId = dau30B?.Id,
                        GhiChu = "Khai báo tồn ban đầu theo biên bản kiểm kê; Loại ra phục vụ đấu giá; Khôi phục vào kho bấm ngẫu nhiên",
                        NgayNhapKho = dotNhapTonDau.NgayNhapKho
                    });
                    stt++;
                }

                await dbContext.ChiTietDotNhapBienSos.AddRangeAsync(chiTietList);
                await dbContext.KhoBienSos.AddRangeAsync(khoBienSos);
                await dbContext.SaveChangesAsync();

                logger?.LogInformation("Đã khởi tạo đợt nhập '{MaDotNhap}' (Đã nhập kho) với 513 biển số vào ChiTietDotNhapBienSo và KhoBienSo", dotNhapTonDau.MaDotNhap);
            }
            else
            {
                // Cập nhật cờ nếu đợt nhập đã có sẵn
                if (!dotNhapTonDau.DaNhapKho)
                {
                    dotNhapTonDau.DaNhapKho = true;
                    dotNhapTonDau.TrangThai = "DaNhapKho";
                    dotNhapTonDau.NgayNhapKhoThucTe = DateTime.UtcNow;
                    dotNhapTonDau.NguoiNhapKho = "Trung Tá - Mai Cao";
                    await dbContext.SaveChangesAsync();
                }

                // Nếu ChiTietDotNhapBienSo chưa có dòng nào cho đợt nhập này, gieo từ KhoBienSo
                var hasChiTiet = await dbContext.ChiTietDotNhapBienSos.AnyAsync(x => x.DotNhapKhoId == dotNhapTonDau.Id);
                if (!hasChiTiet)
                {
                    var khoList = await dbContext.KhoBienSos.Where(x => x.DotNhapKhoId == dotNhapTonDau.Id).ToListAsync();
                    var newChiTiets = khoList.Select(k => new ChiTietDotNhapBienSo
                    {
                        DotNhapKhoId = dotNhapTonDau.Id!,
                        ThuTu = k.ThuTu,
                        BienKiemSoat = k.BienKiemSoat,
                        TrangThaiBienSoId = ttDuocBam?.Id,
                        TrangThaiBienSoTen = k.TrangThaiTen,
                        MauSacHienThi = k.TrangThai == "ChoDauGia" ? "warning" : "success",
                        LoaiXeId = k.LoaiXeId,
                        MauBienSoId = k.MauBienSoId,
                        DauBienSoId = k.DauBienSoId,
                        GhiChu = k.GhiChu
                    }).ToList();
                    await dbContext.ChiTietDotNhapBienSos.AddRangeAsync(newChiTiets);
                    await dbContext.SaveChangesAsync();
                }
            }

            // ==========================================
            // 4. SEED CÁC NHÓM ĐẦU BIỂN TỒN KHO MẪU (THEO GIAO DIỆN TỔNG HỢP TỒN KHO)
            // ==========================================
            var has29AH = await dbContext.KhoBienSos.AnyAsync(x => x.BienKiemSoat.StartsWith("29AH-316"));
            if (!has29AH)
            {
                var xeMayEntity = await dbContext.LoaiXes.FirstOrDefaultAsync(x => x.Ma == "XEMAY");
                var oToEntity = await dbContext.LoaiXes.FirstOrDefaultAsync(x => x.Ma == "OTO");
                var mauTrangEntity = await dbContext.MauBienSos.FirstOrDefaultAsync(x => x.Ma == "TRANG");
                var mauVangEntity = await dbContext.MauBienSos.FirstOrDefaultAsync(x => x.Ma == "VANG");
                var ttDuocBamEntity = await dbContext.TrangThaiBienSos.FirstOrDefaultAsync(x => x.Ma == "DUOC_BAM");
                var ttDaBamEntity = await dbContext.TrangThaiBienSos.FirstOrDefaultAsync(x => x.Ma == "DA_BAM");

                var extraPlates = new List<KhoBienSo>();

                void AddPlateGroup(string prefix, int countDuocBam, int countDaBam, string? loaiXeId, string? mauBienSoId)
                {
                    int num = 1;
                    for (int i = 0; i < countDuocBam; i++)
                    {
                        var s = num.ToString().PadLeft(2, '0');
                        extraPlates.Add(new KhoBienSo
                        {
                            DotNhapKhoId = dotNhapTonDau?.Id ?? Guid.NewGuid().ToString(),
                            ThuTu = num,
                            BienKiemSoat = $"{prefix}.{s}",
                            TrangThai = "DuocBam",
                            TrangThaiTen = "Biển được bấm (Tồn kho)",
                            TrangThaiBienSoId = ttDuocBamEntity?.Id,
                            LoaiXeId = loaiXeId,
                            MauBienSoId = mauBienSoId,
                            DaXuatKho = false,
                            LaBienDauGia = false,
                            NgayNhapKho = DateTime.UtcNow
                        });
                        num++;
                    }

                    for (int i = 0; i < countDaBam; i++)
                    {
                        var s = num.ToString().PadLeft(2, '0');
                        extraPlates.Add(new KhoBienSo
                        {
                            DotNhapKhoId = dotNhapTonDau?.Id ?? Guid.NewGuid().ToString(),
                            ThuTu = num,
                            BienKiemSoat = $"{prefix}.{s}",
                            TrangThai = "DaBam",
                            TrangThaiTen = "Đã bấm (Đã cấp)",
                            TrangThaiBienSoId = ttDaBamEntity?.Id,
                            LoaiXeId = loaiXeId,
                            MauBienSoId = mauBienSoId,
                            DaXuatKho = true,
                            NgayXuatKho = DateTime.UtcNow.AddDays(-1),
                            LaBienDauGia = false,
                            NgayNhapKho = DateTime.UtcNow
                        });
                        num++;
                    }
                }

                // 1. 29AH-316 : Xe máy, Trắng, Tồn = 78, Đã bấm = 0
                AddPlateGroup("29AH-316", 78, 0, xeMayEntity?.Id, mauTrangEntity?.Id);

                // 2. 29AH-314 : Xe máy, Trắng, Tồn = 63, Đã bấm = 0
                AddPlateGroup("29AH-314", 63, 0, xeMayEntity?.Id, mauTrangEntity?.Id);

                // 3. 30B-532  : Ô tô, Trắng, Tồn = 56, Đã bấm = 0
                AddPlateGroup("30B-532", 56, 0, oToEntity?.Id, mauTrangEntity?.Id);

                // 4. 29AH-315 : Xe máy, Trắng, Tồn = 51, Đã bấm = 0
                AddPlateGroup("29AH-315", 51, 0, xeMayEntity?.Id, mauTrangEntity?.Id);

                // 5. 29E-895  : Ô tô, Vàng, Tồn = 49, Đã bấm = 51 (Tổng 100)
                AddPlateGroup("29E-895", 49, 51, oToEntity?.Id, mauVangEntity?.Id);

                // 6. 29E-794  : Ô tô, Vàng, Tồn = 39, Đã bấm = 0
                AddPlateGroup("29E-794", 39, 0, oToEntity?.Id, mauVangEntity?.Id);

                await dbContext.KhoBienSos.AddRangeAsync(extraPlates);
                await dbContext.SaveChangesAsync();

                logger?.LogInformation("Đã khởi tạo các nhóm đầu biển mẫu theo giao diện tổng hợp tồn kho ({Count} biển)", extraPlates.Count);
            }

            // 5. SEED DẢI BIỂN MẪU 29AC-963 CHO MA TRẬN 100 SỐ (00 - 99)
            var has29AC = await dbContext.KhoBienSos.AnyAsync(x => x.BienKiemSoat.StartsWith("29AC-963"));
            if (!has29AC)
            {
                var oToEntity = await dbContext.LoaiXes.FirstOrDefaultAsync(x => x.Ma == "OTO");
                var mauTrangEntity = await dbContext.MauBienSos.FirstOrDefaultAsync(x => x.Ma == "TRANG");
                var ttDuocBamEntity = await dbContext.TrangThaiBienSos.FirstOrDefaultAsync(x => x.Ma == "DUOC_BAM");
                var ttDaBamEntity = await dbContext.TrangThaiBienSos.FirstOrDefaultAsync(x => x.Ma == "DA_BAM");

                // Các số "ĐƯỢC BẤM" theo đúng ảnh mẫu: .52, .56, .60, .64, .68, .72, .76, .80, .84, .88, .92, .96
                var duocBamNumbers = new HashSet<int> { 52, 56, 60, 64, 68, 72, 76, 80, 84, 88, 92, 96 };
                var matranPlates = new List<KhoBienSo>();

                for (int i = 51; i <= 99; i++)
                {
                    bool isDuocBam = duocBamNumbers.Contains(i);
                    var s = i.ToString("D2");
                    matranPlates.Add(new KhoBienSo
                    {
                        DotNhapKhoId = dotNhapTonDau?.Id ?? Guid.NewGuid().ToString(),
                        ThuTu = i,
                        BienKiemSoat = $"29AC-963.{s}",
                        TrangThai = isDuocBam ? "DuocBam" : "DaBam",
                        TrangThaiTen = isDuocBam ? "Biển được bấm (Tồn kho)" : "Đã bấm (Đã cấp)",
                        TrangThaiBienSoId = isDuocBam ? ttDuocBamEntity?.Id : ttDaBamEntity?.Id,
                        LoaiXeId = oToEntity?.Id,
                        MauBienSoId = mauTrangEntity?.Id,
                        DaXuatKho = !isDuocBam,
                        NgayXuatKho = !isDuocBam ? DateTime.UtcNow.AddDays(-2) : null,
                        LaBienDauGia = false,
                        NgayNhapKho = DateTime.UtcNow
                    });
                }

                await dbContext.KhoBienSos.AddRangeAsync(matranPlates);
                await dbContext.SaveChangesAsync();
                logger?.LogInformation("Đã khởi tạo dải biển mẫu 29AC-963 cho màn hình Ma Trận 100 số ({Count} biển)", matranPlates.Count);
            }

            // ==========================================
            // 6. SEED DỮ LIỆU ĐỢT NHẬP VÀ BIÊN LAI XUẤT KHO CHO CÁC BÁO CÁO (TAB 3 - TAB 7)
            // ==========================================
            var loaiXeRoMooc = await dbContext.LoaiXes.FirstOrDefaultAsync(x => x.Ma == "ROMOOC");
            if (loaiXeRoMooc == null)
            {
                loaiXeRoMooc = new LoaiXe { Ma = "ROMOOC", Ten = "Rơ moóc", NhomXe = "Rơ moóc", ThuTu = 3, IsActive = true };
                dbContext.LoaiXes.Add(loaiXeRoMooc);
                await dbContext.SaveChangesAsync();
            }

            // 6.1. Seed 4 đợt nhập bổ sung (Tab 6: Bảng kê chi tiết đợt nhập biển số xe)
            var hasDotNhap2 = await dbContext.DotNhapKhos.AnyAsync(x => x.MaDotNhap == "NK-2026-08-10-29E");
            if (!hasDotNhap2)
            {
                var extraDotNhaps = new List<DotNhapKho>
                {
                    new()
                    {
                        MaDotNhap = "NK-2026-08-10-29E",
                        NgayNhapKho = new DateTime(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc),
                        LoaiXeId = loaiXeOTo?.Id,
                        KyHieuDaiDauBien = "29E-895",
                        TongSoBien = 100,
                        SoBienDuocBam = 36,
                        CanBoTiepNhan = "Nguyễn Thị Lan",
                        DaNhapKho = true,
                        TrangThai = "DaNhapKho",
                        NguoiNhapKho = "Nguyễn Thị Lan"
                    },
                    new()
                    {
                        MaDotNhap = "NK-2026-09-05-30B",
                        NgayNhapKho = new DateTime(2026, 9, 5, 8, 0, 0, DateTimeKind.Utc),
                        LoaiXeId = loaiXeOTo?.Id,
                        KyHieuDaiDauBien = "30B-530",
                        TongSoBien = 100,
                        SoBienDuocBam = 78,
                        CanBoTiepNhan = "Trung Tá - Mai Cao",
                        DaNhapKho = true,
                        TrangThai = "DaNhapKho",
                        NguoiNhapKho = "Trung Tá - Mai Cao"
                    },
                    new()
                    {
                        MaDotNhap = "NK-2026-09-20-29RM",
                        NgayNhapKho = new DateTime(2026, 9, 20, 8, 0, 0, DateTimeKind.Utc),
                        LoaiXeId = loaiXeRoMooc?.Id,
                        KyHieuDaiDauBien = "29RM-051",
                        TongSoBien = 50,
                        SoBienDuocBam = 42,
                        CanBoTiepNhan = "Trung Tá - Mai Cao",
                        DaNhapKho = true,
                        TrangThai = "DaNhapKho",
                        NguoiNhapKho = "Trung Tá - Mai Cao"
                    },
                    new()
                    {
                        MaDotNhap = "NK-2026-10-02-29E",
                        NgayNhapKho = new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc),
                        LoaiXeId = loaiXeOTo?.Id,
                        KyHieuDaiDauBien = "29E-895",
                        TongSoBien = 50,
                        SoBienDuocBam = 45,
                        CanBoTiepNhan = "Nguyễn Thị Lan",
                        DaNhapKho = true,
                        TrangThai = "DaNhapKho",
                        NguoiNhapKho = "Nguyễn Thị Lan"
                    }
                };

                await dbContext.DotNhapKhos.AddRangeAsync(extraDotNhaps);
                await dbContext.SaveChangesAsync();
                logger?.LogInformation("Đã khởi tạo 4 đợt nhập mẫu bổ sung cho Báo cáo đợt nhập");
            }

            // 6.2. Seed 8 biên lai xuất kho Cấp biển (Tab 7 & Tab 4)
            var hasXuatKhoBaoCao = await dbContext.XuatKhoBienSos.AnyAsync(x => x.SoBienLai == "0000101");
            if (!hasXuatKhoBaoCao)
            {
                var xuatKhosBaoCao = new List<XuatKhoBienSo>
                {
                    new()
                    {
                        LoaiThuTucId = thuTucCapBien?.Id ?? "CAP_BIEN",
                        LoaiThuTucTen = "Cấp biển (Có xuất kho số)",
                        NgayThucHien = new DateTime(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc),
                        NgayBienLai = new DateTime(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc),
                        KhoBienSoId = Guid.NewGuid().ToString(),
                        BienKiemSoat = "29E-895.02",
                        DauBienSoTen = "29E-895",
                        KyHieuQuyen = "AA/26E",
                        KyHieuQuyenBienLaiId = quyenAA26E?.Id ?? "AA/26E",
                        SoBienLai = "0000101",
                        HoTenNguoiNop = "Công ty TNHH Vận Tải Thủ Đô",
                        SoTienThu = 500000,
                        MucThuLePhiId = mtlpOto01?.Id ?? "MTLP",
                        KhuVucApDungId = khuVuc1?.Id ?? "KV1",
                        TrangThai = "HoanTat"
                    },
                    new()
                    {
                        LoaiThuTucId = thuTucCapBien?.Id ?? "CAP_BIEN",
                        LoaiThuTucTen = "Cấp biển (Có xuất kho số)",
                        NgayThucHien = new DateTime(2026, 8, 12, 9, 30, 0, DateTimeKind.Utc),
                        NgayBienLai = new DateTime(2026, 8, 12, 9, 30, 0, DateTimeKind.Utc),
                        KhoBienSoId = Guid.NewGuid().ToString(),
                        BienKiemSoat = "29E-895.03",
                        DauBienSoTen = "29E-895",
                        KyHieuQuyen = "AA/26E",
                        KyHieuQuyenBienLaiId = quyenAA26E?.Id ?? "AA/26E",
                        SoBienLai = "0000102",
                        HoTenNguoiNop = "Nguyễn Văn Minh",
                        SoTienThu = 500000,
                        MucThuLePhiId = mtlpOto01?.Id ?? "MTLP",
                        KhuVucApDungId = khuVuc1?.Id ?? "KV1",
                        TrangThai = "HoanTat"
                    },
                    new()
                    {
                        LoaiThuTucId = thuTucCapBien?.Id ?? "CAP_BIEN",
                        LoaiThuTucTen = "Cấp biển (Có xuất kho số)",
                        NgayThucHien = new DateTime(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc),
                        NgayBienLai = new DateTime(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc),
                        KhoBienSoId = Guid.NewGuid().ToString(),
                        BienKiemSoat = "29AH-313.40",
                        DauBienSoTen = "29AH-313",
                        KyHieuQuyen = "AA/26E",
                        KyHieuQuyenBienLaiId = quyenAA26E?.Id ?? "AA/26E",
                        SoBienLai = "0000105",
                        HoTenNguoiNop = "Hoàng Mai Phương",
                        SoTienThu = 1000000,
                        MucThuLePhiId = mtlpOto01?.Id ?? "MTLP",
                        KhuVucApDungId = khuVuc1?.Id ?? "KV1",
                        TrangThai = "HoanTat"
                    },
                    new()
                    {
                        LoaiThuTucId = thuTucCapBien?.Id ?? "CAP_BIEN",
                        LoaiThuTucTen = "Cấp biển (Có xuất kho số)",
                        NgayThucHien = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc),
                        NgayBienLai = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc),
                        KhoBienSoId = Guid.NewGuid().ToString(),
                        BienKiemSoat = "29E-895.04",
                        DauBienSoTen = "29E-895",
                        KyHieuQuyen = "AA/26E",
                        KyHieuQuyenBienLaiId = quyenAA26E?.Id ?? "AA/26E",
                        SoBienLai = "0000106",
                        HoTenNguoiNop = "Phạm Thành Long",
                        SoTienThu = 500000,
                        MucThuLePhiId = mtlpOto01?.Id ?? "MTLP",
                        KhuVucApDungId = khuVuc1?.Id ?? "KV1",
                        TrangThai = "HoanTat"
                    },
                    new()
                    {
                        LoaiThuTucId = thuTucCapBien?.Id ?? "CAP_BIEN",
                        LoaiThuTucTen = "Cấp biển (Có xuất kho số)",
                        NgayThucHien = new DateTime(2026, 9, 8, 9, 0, 0, DateTimeKind.Utc),
                        NgayBienLai = new DateTime(2026, 9, 8, 9, 0, 0, DateTimeKind.Utc),
                        KhoBienSoId = Guid.NewGuid().ToString(),
                        BienKiemSoat = "30B-530.01",
                        DauBienSoTen = "30B-530",
                        KyHieuQuyen = "AA/26E",
                        KyHieuQuyenBienLaiId = quyenAA26E?.Id ?? "AA/26E",
                        SoBienLai = "0000107",
                        HoTenNguoiNop = "Bùi Anh Tuấn",
                        SoTienThu = 20000000,
                        MucThuLePhiId = mtlpOto01?.Id ?? "MTLP",
                        KhuVucApDungId = khuVuc1?.Id ?? "KV1",
                        TrangThai = "HoanTat"
                    },
                    new()
                    {
                        LoaiThuTucId = thuTucCapBien?.Id ?? "CAP_BIEN",
                        LoaiThuTucTen = "Cấp biển (Có xuất kho số)",
                        NgayThucHien = new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc),
                        NgayBienLai = new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc),
                        KhoBienSoId = Guid.NewGuid().ToString(),
                        BienKiemSoat = "30B-530.02",
                        DauBienSoTen = "30B-530",
                        KyHieuQuyen = "AA/26E",
                        KyHieuQuyenBienLaiId = quyenAA26E?.Id ?? "AA/26E",
                        SoBienLai = "0000108",
                        HoTenNguoiNop = "Lê Hoàng Yến",
                        SoTienThu = 20000000,
                        MucThuLePhiId = mtlpOto01?.Id ?? "MTLP",
                        KhuVucApDungId = khuVuc1?.Id ?? "KV1",
                        TrangThai = "HoanTat"
                    },
                    new()
                    {
                        LoaiThuTucId = thuTucCapBien?.Id ?? "CAP_BIEN",
                        LoaiThuTucTen = "Cấp biển (Có xuất kho số)",
                        NgayThucHien = new DateTime(2026, 9, 24, 9, 0, 0, DateTimeKind.Utc),
                        NgayBienLai = new DateTime(2026, 9, 24, 9, 0, 0, DateTimeKind.Utc),
                        KhoBienSoId = Guid.NewGuid().ToString(),
                        BienKiemSoat = "29RM-051.01",
                        DauBienSoTen = "29RM-051",
                        KyHieuQuyen = "AA/26E",
                        KyHieuQuyenBienLaiId = quyenAA26E?.Id ?? "AA/26E",
                        SoBienLai = "0000110",
                        HoTenNguoiNop = "Công ty CP Đầu tư & Tiếp vận Bắc Á",
                        SoTienThu = 200000,
                        MucThuLePhiId = mtlpOto01?.Id ?? "MTLP",
                        KhuVucApDungId = khuVuc1?.Id ?? "KV1",
                        TrangThai = "HoanTat"
                    },
                    new()
                    {
                        LoaiThuTucId = thuTucCapBien?.Id ?? "CAP_BIEN",
                        LoaiThuTucTen = "Cấp biển (Có xuất kho số)",
                        NgayThucHien = new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc),
                        NgayBienLai = new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc),
                        KhoBienSoId = Guid.NewGuid().ToString(),
                        BienKiemSoat = "29E-895.05",
                        DauBienSoTen = "29E-895",
                        KyHieuQuyen = "AA/26E",
                        KyHieuQuyenBienLaiId = quyenAA26E?.Id ?? "AA/26E",
                        SoBienLai = "0000112",
                        HoTenNguoiNop = "Hợp tác xã Vận tải An Phát",
                        SoTienThu = 500000,
                        MucThuLePhiId = mtlpOto01?.Id ?? "MTLP",
                        KhuVucApDungId = khuVuc1?.Id ?? "KV1",
                        TrangThai = "HoanTat"
                    }
                };

                await dbContext.XuatKhoBienSos.AddRangeAsync(xuatKhosBaoCao);
                await dbContext.SaveChangesAsync();
                logger?.LogInformation("Đã khởi tạo 8 biên lai xuất kho Cấp biển cho Báo cáo");
            }

            // 6.3. Seed 4 biên lai các thủ tục khác (Sang tên, Cấp lại, Chỉ ghi biên lai)
            var hasThuTucKhac = await dbContext.CapLaiDangKyXes.AnyAsync(x => x.SoBienLai == "0000103");
            if (!hasThuTucKhac)
            {
                var thuTucKhacList = new List<CapLaiDangKyXe>
                {
                    new()
                    {
                        KyHieuQuyen = "AA/26E",
                        KyHieuQuyenBienLaiId = quyenAA26E?.Id ?? "AA/26E",
                        SoBienLai = "0000103",
                        NgayBienLai = new DateTime(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc),
                        BienKiemSoat = "30E-123.45",
                        LoaiPhuongTien = "Ô tô",
                        NoiDungThuPhi = "Sang tên xe",
                        SoTienThu = 150000,
                        HoTenChuXe = "Trần Thị Thu Trang",
                        TrangThai = "DaXacNhan",
                        DaXacNhan = true
                    },
                    new()
                    {
                        KyHieuQuyen = "AA/26E",
                        KyHieuQuyenBienLaiId = quyenAA26E?.Id ?? "AA/26E",
                        SoBienLai = "0000104",
                        NgayBienLai = new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc),
                        BienKiemSoat = "29A-888.66",
                        LoaiPhuongTien = "Ô tô",
                        NoiDungThuPhi = "Cấp lại đăng ký xe",
                        SoTienThu = 150000,
                        HoTenChuXe = "Vũ Quốc Bảo",
                        TrangThai = "DaXacNhan",
                        DaXacNhan = true
                    },
                    new()
                    {
                        KyHieuQuyen = "AA/26E",
                        KyHieuQuyenBienLaiId = quyenAA26E?.Id ?? "AA/26E",
                        SoBienLai = "0000109",
                        NgayBienLai = new DateTime(2026, 9, 18, 9, 0, 0, DateTimeKind.Utc),
                        BienKiemSoat = "29C-456.78",
                        LoaiPhuongTien = "Ô tô",
                        NoiDungThuPhi = "Sang tên xe",
                        SoTienThu = 150000,
                        HoTenChuXe = "Trịnh Quốc Doanh",
                        TrangThai = "DaXacNhan",
                        DaXacNhan = true
                    },
                    new()
                    {
                        KyHieuQuyen = "AA/26E",
                        KyHieuQuyenBienLaiId = quyenAA26E?.Id ?? "AA/26E",
                        SoBienLai = "0000111",
                        NgayBienLai = new DateTime(2026, 10, 3, 9, 0, 0, DateTimeKind.Utc),
                        BienKiemSoat = "29A-999.88",
                        LoaiPhuongTien = "Ô tô",
                        NoiDungThuPhi = "Chỉ ghi biên lai thu phí",
                        SoTienThu = 50000,
                        HoTenChuXe = "Đỗ Thị Kim Oanh",
                        TrangThai = "DaXacNhan",
                        DaXacNhan = true
                    }
                };

                await dbContext.CapLaiDangKyXes.AddRangeAsync(thuTucKhacList);
                await dbContext.SaveChangesAsync();
                logger?.LogInformation("Đã khởi tạo 4 biên lai thủ tục khác (Sang tên, Cấp lại, Chỉ ghi biên lai) cho Báo cáo");
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Lỗi xảy ra trong quá trình khởi tạo dữ liệu seed cho hệ thống.");
        }
    }
}
