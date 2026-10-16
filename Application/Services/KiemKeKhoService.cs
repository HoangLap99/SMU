using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Core.Entities;
using SMU.Infrastructure.Data;

namespace SMU.Application.Services;

/// <summary>
/// Service triển khai Quản lý Kiểm kê kho biển số xe:
/// - Khởi tạo dữ liệu phiếu kiểm kê từ bảng KhoBienSo đối với các biển chưa xuất.
/// - Popup xác định biển thiếu / thừa theo dải đầu biển.
/// - Thêm mới và cập nhật phiếu kiểm kê (Lưu nháp / Xác nhận hoàn tất).
/// - Lấy danh sách phiếu kiểm kê có phân trang và bộ lọc.
/// - Lấy chi tiết và xóa phiếu kiểm kê.
/// </summary>
public class KiemKeKhoService : IKiemKeKhoService
{
    private readonly PostgresDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogHoatDongService _logHoatDongService;

    public KiemKeKhoService(
        PostgresDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogHoatDongService logHoatDongService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logHoatDongService = logHoatDongService;
    }

    /// <summary>
    /// API Khởi tạo dữ liệu phiếu kiểm kê từ bảng KhoBienSo đối với các biển chưa xuất.
    /// </summary>
    public async Task<KhoiTaoKiemKeKhoResponse> KhoiTaoPhieuKiemKeAsync(
        KhoiTaoKiemKeKhoRequest request,
        CancellationToken cancellationToken = default)
    {
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        // 1. Lọc các biển số trong kho CHƯA XUẤT KHO (!DaXuatKho, TrangThai == 'DuocBam', !LaBienDauGia)
        var query = _dbContext.KhoBienSos.AsNoTracking()
            .Where(x => !x.DaXuatKho && x.TrangThai == "DuocBam" && !x.LaBienDauGia);

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            query = query.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        var plates = await query
            .Select(x => new
            {
                x.Id,
                x.BienKiemSoat,
                x.LoaiXeId,
                x.MauBienSoId
            })
            .ToListAsync(cancellationToken);

        // 2. Tải danh mục Loại xe & Màu biển số
        var mauBienIds = plates.Select(p => p.MauBienSoId).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().Distinct().ToList();
        var mauBiens = await _dbContext.MauBienSos.AsNoTracking()
            .Where(x => x.Id != null && mauBienIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id!, x => new { Ten = x.Ten ?? "Biển Trắng", Hex = x.MaMauHex ?? "#FFFFFF" }, cancellationToken);

        // 3. Gom nhóm theo Đầu biển và Màu biển
        var groups = plates
            .GroupBy(p => new
            {
                DauBien = ExtractDauBien(p.BienKiemSoat),
                p.MauBienSoId
            })
            .OrderBy(g => g.Key.DauBien)
            .ToList();

        int stt = 1;
        var nhomItems = new List<KiemKeNhomDauBienItemDto>();

        foreach (var grp in groups)
        {
            string mauTen = "Biển Trắng";
            string mauHex = "#FFFFFF";

            if (grp.Key.MauBienSoId != null && mauBiens.TryGetValue(grp.Key.MauBienSoId, out var mb))
            {
                mauTen = mb.Ten.StartsWith("Biển", StringComparison.OrdinalIgnoreCase) ? mb.Ten : $"Biển {mb.Ten}";
                mauHex = mb.Hex;
            }

            int count = grp.Count();

            var danhSachTrongKho = grp
                .OrderBy(p => p.BienKiemSoat)
                .Select(p => new BienTrongKhoItemDto
                {
                    KhoBienSoId = p.Id ?? string.Empty,
                    BienKiemSoat = p.BienKiemSoat,
                    TrangThaiKiemKe = "CoMat",
                    IsCoMat = true,
                    GhiChu = "Có mặt"
                })
                .ToList();

            nhomItems.Add(new KiemKeNhomDauBienItemDto
            {
                STT = stt++,
                DauBien = grp.Key.DauBien,
                MauBienSoId = grp.Key.MauBienSoId,
                MauBien = mauTen,
                MauBienHex = mauHex,
                TonHeThong = count,
                ThucTe = count,
                ChenhLech = 0,
                SoBienCoMat = count,
                SoBienThieu = 0,
                SoBienThua = 0,
                GhiChu = "Khớp sổ sách",
                DanhSachBienTrongKho = danhSachTrongKho,
                DanhSachBienThua = new List<BienThuaItemDto>()
            });
        }

        var tongTon = nhomItems.Sum(x => x.TonHeThong);

        // 4. Sinh mã phiếu kiểm kê tự động
        var today = DateTime.UtcNow;
        var datePrefix = $"KK-{today:yyyy-MM-dd}";
        var countToday = await _dbContext.KiemKeKhos.CountAsync(x => x.MaPhieuKiemKe.StartsWith(datePrefix), cancellationToken);
        var randomSuffix = (countToday + 1).ToString("D3");
        var maPhieu = $"{datePrefix}-{randomSuffix}";

        return new KhoiTaoKiemKeKhoResponse
        {
            MaPhieuKiemKe = maPhieu,
            NgayKiemKe = DateTime.UtcNow.Date,
            ThoiGianKiemKe = $"{today.Hour} giờ {today.Minute:D2} phút",
            LoaiPhuongTien = string.IsNullOrWhiteSpace(request.LoaiPhuongTien) ? "Ô tô" : request.LoaiPhuongTien,
            DiaDiemKiemKe = "Tổ đăng ký xe, Công an phường Nghĩa Đô",
            TruongDoanKiemKe = "Thiếu tá Bùi Văn Kiên",
            CanBoLapBienBan = "Bùi Văn Kiên",
            TongTonHeThong = tongTon,
            TongThucTe = tongTon,
            TongChenhLech = 0,
            DanhSachNhomDauBien = nhomItems
        };
    }

    /// <summary>
    /// API Lấy dữ liệu cho Popup "Xác định biển thiếu/thừa: Dải {dauBien} ({mauBien})".
    /// </summary>
    public async Task<ChiTietBienKiemKeResponse> LayChiTietBienPopupAsync(
        ChiTietBienKiemKeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DauBien))
        {
            throw new ArgumentException("Vui lòng truyền dải đầu biển cần xem chi tiết.");
        }

        var dauBien = request.DauBien.Trim().ToUpperInvariant();
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        // 1. Lấy danh sách biển trong kho chưa xuất thuộc dải đầu biển này
        var query = _dbContext.KhoBienSos.AsNoTracking()
            .Where(x => !x.DaXuatKho && x.TrangThai == "DuocBam" && !x.LaBienDauGia)
            .Where(x => x.BienKiemSoat.ToUpper().StartsWith(dauBien + ".") || x.BienKiemSoat.ToUpper().StartsWith(dauBien));

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        if (!string.IsNullOrWhiteSpace(request.MauBienSoId))
        {
            query = query.Where(x => x.MauBienSoId == request.MauBienSoId);
        }

        var platesInDb = await query
            .OrderBy(x => x.BienKiemSoat)
            .Select(x => new
            {
                Id = x.Id ?? string.Empty,
                x.BienKiemSoat,
                x.MauBienSoId
            })
            .ToListAsync(cancellationToken);

        // 2. Nếu có KiemKeKhoId, lấy thông tin các biển đã lưu trạng thái đối chiếu (Thiếu / Thừa)
        var recordedChiTiets = new List<KiemKeKhoChiTietBien>();
        if (!string.IsNullOrWhiteSpace(request.KiemKeKhoId))
        {
            recordedChiTiets = await _dbContext.KiemKeKhoChiTietBiens.AsNoTracking()
                .Where(x => x.KiemKeKhoId == request.KiemKeKhoId && x.DauBien.ToUpper() == dauBien)
                .ToListAsync(cancellationToken);
        }

        var thieuBienSoSet = recordedChiTiets
            .Where(x => x.TrangThaiKiemKe == "Thieu")
            .Select(x => x.BienKiemSoat.ToUpper())
            .ToHashSet();

        // 3. Danh sách biển tồn trong kho
        var danhSachBienTrongKho = platesInDb.Select(p =>
        {
            bool isThieu = thieuBienSoSet.Contains(p.BienKiemSoat.ToUpper());
            return new BienTrongKhoItemDto
            {
                KhoBienSoId = p.Id,
                BienKiemSoat = p.BienKiemSoat,
                TrangThaiKiemKe = isThieu ? "Thieu" : "CoMat",
                IsCoMat = !isThieu
            };
        }).ToList();

        // 4. Danh sách biển phát hiện thừa thực tế (chưa có trong phần mềm)
        var danhSachBienThua = recordedChiTiets
            .Where(x => x.LoaiBien == "BienThua" || x.TrangThaiKiemKe == "Thua")
            .Select(x => new BienThuaItemDto
            {
                Id = x.Id,
                BienKiemSoat = x.BienKiemSoat,
                GhiChu = x.GhiChu ?? "Phát hiện thừa thực tế"
            })
            .ToList();

        // Lọc theo từ khóa tìm kiếm nếu có
        var filteredTrongKho = danhSachBienTrongKho;
        if (!string.IsNullOrWhiteSpace(request.KeyWord))
        {
            var kw = request.KeyWord.Trim().ToUpper();
            filteredTrongKho = danhSachBienTrongKho.Where(x => x.BienKiemSoat.ToUpper().Contains(kw)).ToList();
        }

        // Tên màu biển hiển thị
        string mauBienTen = "Trắng";
        var firstMauId = platesInDb.FirstOrDefault()?.MauBienSoId ?? request.MauBienSoId;
        if (!string.IsNullOrWhiteSpace(firstMauId))
        {
            var mb = await _dbContext.MauBienSos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == firstMauId, cancellationToken);
            if (mb?.Ten != null)
            {
                mauBienTen = mb.Ten.Replace("Biển", "").Trim();
            }
        }

        int tonSoSach = platesInDb.Count;
        int bienCoMat = danhSachBienTrongKho.Count(x => x.IsCoMat);
        int bienBaoThieu = danhSachBienTrongKho.Count(x => !x.IsCoMat);
        int bienPhatHienThua = danhSachBienThua.Count;
        int thucTe = bienCoMat + bienPhatHienThua;
        int chenhLech = thucTe - tonSoSach;

        return new ChiTietBienKiemKeResponse
        {
            DauBien = dauBien,
            MauBien = mauBienTen,
            TieuDe = $"Xác định biển thiếu/thừa: Dải {dauBien} ({mauBienTen})",
            TonSoSach = tonSoSach,
            BienCoMat = bienCoMat,
            BienBaoThieu = bienBaoThieu,
            BienPhatHienThua = bienPhatHienThua,
            ThucTe = thucTe,
            ChenhLech = chenhLech,
            DanhSachBienTrongKho = filteredTrongKho,
            DanhSachBienThua = danhSachBienThua
        };
    }

    /// <summary>
    /// API Thêm mới hoặc cập nhật phiếu kiểm kê kho (Lưu nháp hoặc Xác nhận hoàn tất).
    /// </summary>
    public async Task<KiemKeKhoResponse> SetKiemKeKhoAsync(
        SetKiemKeKhoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        bool isHoanTat = request.XacNhanHoanTat || string.Equals(request.TrangThai, "HoanTat", StringComparison.OrdinalIgnoreCase);
        var trangThaiPhieu = isHoanTat ? "HoanTat" : "LuuNhap";

        // 1. TẬP HỢP TẤT CẢ BIỂN SỐ TỪ DanhSachNhomDauBien:
        // Mỗi nhóm dải đầu biển chứa DanhSachBienTrongKho (chuyển Có mặt <-> Thiếu) và DanhSachBienThua (tự động nhận diện đầu biển)
        var allChiTietMap = new Dictionary<string, ChiTietKiemKeRecord>(StringComparer.OrdinalIgnoreCase);

        if (request.DanhSachNhomDauBien != null)
        {
            foreach (var nhom in request.DanhSachNhomDauBien)
            {
                // Xử lý các biển trong kho của nhóm (chuyển đổi Có mặt <-> Thiếu)
                if (nhom.DanhSachBienTrongKho != null)
                {
                    foreach (var btk in nhom.DanhSachBienTrongKho)
                    {
                        if (string.IsNullOrWhiteSpace(btk.BienKiemSoat)) continue;
                        var bks = btk.BienKiemSoat.Trim().ToUpperInvariant();
                        var dauBien = ExtractDauBien(bks, nhom.DauBien);
                        bool isThieu = !btk.IsCoMat || string.Equals(btk.TrangThaiKiemKe, "Thieu", StringComparison.OrdinalIgnoreCase);

                        allChiTietMap[bks] = new ChiTietKiemKeRecord(
                            dauBien,
                            bks,
                            btk.KhoBienSoId,
                            "TrongKho",
                            isThieu ? "Thieu" : "CoMat",
                            btk.GhiChu ?? (isThieu ? "Kiểm kê báo thiếu" : "Có mặt")
                        );
                    }
                }

                // Xử lý các biển thừa (TỰ ĐỘNG XÁC ĐỊNH DẢI ĐẦU BIỂN THEO BIỂN KIỂM SOÁT)
                if (nhom.DanhSachBienThua != null)
                {
                    foreach (var bt in nhom.DanhSachBienThua)
                    {
                        if (string.IsNullOrWhiteSpace(bt.BienKiemSoat)) continue;
                        var bks = bt.BienKiemSoat.Trim().ToUpperInvariant();
                        var autoDauBien = ExtractDauBien(bks, nhom.DauBien);

                        allChiTietMap[bks] = new ChiTietKiemKeRecord(
                            autoDauBien,
                            bks,
                            null,
                            "BienThua",
                            "Thua",
                            bt.GhiChu ?? "Phát hiện thừa thực tế"
                        );
                    }
                }
            }
        }

        var allChiTiets = allChiTietMap.Values.ToList();

        // 2. TỰ ĐỘNG ĐẢM BẢO MỌI ĐẦU BIỂN CỦA BIỂN THỪA ĐỀU CÓ MẶT TRONG DanhSachNhomDauBien
        request.DanhSachNhomDauBien ??= new List<SetKiemKeNhomDauBienRequest>();
        var existingDauBiens = request.DanhSachNhomDauBien
            .Where(x => !string.IsNullOrWhiteSpace(x.DauBien))
            .Select(x => x.DauBien.Trim().ToUpperInvariant())
            .ToHashSet();

        // Tìm các biển thừa có đầu biển chưa có trong nhóm
        var missingDauBiens = allChiTiets
            .Where(x => x.LoaiBien == "BienThua" || x.TrangThaiKiemKe == "Thua")
            .Select(x => x.DauBien.Trim().ToUpperInvariant())
            .Where(d => !existingDauBiens.Contains(d))
            .Distinct()
            .ToList();

        foreach (var extraDauBien in missingDauBiens)
        {
            request.DanhSachNhomDauBien.Add(new SetKiemKeNhomDauBienRequest
            {
                STT = request.DanhSachNhomDauBien.Count + 1,
                DauBien = extraDauBien,
                MauBien = "Biển Trắng",
                TonHeThong = 0,
                ThucTe = 0,
                ChenhLech = 0,
                GhiChu = "Phát hiện thừa thực tế"
            });
            existingDauBiens.Add(extraDauBien);
        }

        // 3. TỰ ĐỘNG TÍNH TOÁN LẠI CHỈ SỐ CHO TỪNG NHÓM ĐẦU BIỂN
        int currentStt = 1;
        foreach (var nhom in request.DanhSachNhomDauBien)
        {
            nhom.STT = currentStt++;
            var dauBienKey = nhom.DauBien.Trim().ToUpperInvariant();

            var platesOfNhom = allChiTiets
                .Where(x => x.DauBien.Trim().Equals(dauBienKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var thieuList = platesOfNhom.Where(x => x.TrangThaiKiemKe == "Thieu").ToList();
            var thuaList = platesOfNhom.Where(x => x.LoaiBien == "BienThua" || x.TrangThaiKiemKe == "Thua").ToList();

            int soThieu = thieuList.Count;
            int soThua = thuaList.Count;

            // Số biển có mặt:
            int soCoMat;
            if (nhom.DanhSachBienTrongKho != null && nhom.DanhSachBienTrongKho.Any())
            {
                soCoMat = nhom.DanhSachBienTrongKho.Count(x => x.IsCoMat && !string.Equals(x.TrangThaiKiemKe, "Thieu", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                soCoMat = Math.Max(0, nhom.TonHeThong - soThieu);
            }

            nhom.SoBienCoMat = soCoMat;
            nhom.SoBienThieu = soThieu;
            nhom.SoBienThua = soThua;
            nhom.ThucTe = soCoMat + soThua;
            nhom.ChenhLech = nhom.ThucTe - nhom.TonHeThong;

            if (string.IsNullOrWhiteSpace(nhom.GhiChu) || nhom.GhiChu == "Khớp sổ sách" || nhom.GhiChu.StartsWith("Thừa") || nhom.GhiChu.StartsWith("Thiếu"))
            {
                nhom.GhiChu = nhom.ChenhLech == 0
                    ? "Khớp sổ sách"
                    : (nhom.ChenhLech > 0 ? $"Thừa {nhom.ChenhLech} biển" : $"Thiếu {Math.Abs(nhom.ChenhLech)} biển");
            }
        }

        // 4. TÍNH TOÁN LẠI TỔNG SỐ CHO PHIẾU
        int tongTonHeThong = request.DanhSachNhomDauBien.Sum(x => x.TonHeThong);
        int tongThucTe = request.DanhSachNhomDauBien.Sum(x => x.ThucTe);
        int tongChenhLech = tongThucTe - tongTonHeThong;

        // Lưu / Cập nhật Phiếu KiemKeKho
        KiemKeKho? kiemKe;
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            // THÊM MỚI
            kiemKe = new KiemKeKho
            {
                CustomerId = customerId,
                MaPhieuKiemKe = request.MaPhieuKiemKe.Trim(),
                NgayKiemKe = request.NgayKiemKe,
                ThoiGianKiemKe = request.ThoiGianKiemKe,
                LoaiXeId = request.LoaiXeId,
                LoaiPhuongTien = string.IsNullOrWhiteSpace(request.LoaiPhuongTien) ? "Ô tô" : request.LoaiPhuongTien,
                DiaDiemKiemKe = request.DiaDiemKiemKe,
                TruongDoanKiemKe = request.TruongDoanKiemKe,
                CanBoLapBienBan = request.CanBoLapBienBan,
                TongTonHeThong = tongTonHeThong,
                TongThucTe = tongThucTe,
                TongChenhLech = tongChenhLech,
                TrangThai = trangThaiPhieu,
                XacNhanHoanTat = isHoanTat,
                GhiChu = request.GhiChu
            };

            await _dbContext.KiemKeKhos.AddAsync(kiemKe, cancellationToken);
        }
        else
        {
            // CẬP NHẬT
            kiemKe = await _dbContext.KiemKeKhos
                .Include(x => x.DanhSachNhomDauBien)
                .Include(x => x.DanhSachChiTietBien)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (kiemKe == null)
            {
                throw new ArgumentException($"Không tìm thấy phiếu kiểm kê với Id '{request.Id}'.");
            }

            kiemKe.MaPhieuKiemKe = request.MaPhieuKiemKe.Trim();
            kiemKe.NgayKiemKe = request.NgayKiemKe;
            kiemKe.ThoiGianKiemKe = request.ThoiGianKiemKe;
            kiemKe.LoaiXeId = request.LoaiXeId;
            kiemKe.LoaiPhuongTien = string.IsNullOrWhiteSpace(request.LoaiPhuongTien) ? "Ô tô" : request.LoaiPhuongTien;
            kiemKe.DiaDiemKiemKe = request.DiaDiemKiemKe;
            kiemKe.TruongDoanKiemKe = request.TruongDoanKiemKe;
            kiemKe.CanBoLapBienBan = request.CanBoLapBienBan;
            kiemKe.TongTonHeThong = tongTonHeThong;
            kiemKe.TongThucTe = tongThucTe;
            kiemKe.TongChenhLech = tongChenhLech;
            kiemKe.TrangThai = trangThaiPhieu;
            kiemKe.XacNhanHoanTat = isHoanTat;
            kiemKe.GhiChu = request.GhiChu;

            // Xóa chi tiết cũ để cập nhật mới
            _dbContext.KiemKeKhoNhomDauBiens.RemoveRange(kiemKe.DanhSachNhomDauBien);
            _dbContext.KiemKeKhoChiTietBiens.RemoveRange(kiemKe.DanhSachChiTietBien);
        }

        // 5. LƯU BẢNG KiemKeKhoNhomDauBien
        var nhomEntityMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in request.DanhSachNhomDauBien)
        {
            var nhomEntity = new KiemKeKhoNhomDauBien
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = customerId,
                KiemKeKhoId = kiemKe.Id!,
                STT = item.STT,
                DauBien = item.DauBien,
                MauBienSoId = item.MauBienSoId,
                MauBien = item.MauBien,
                MauBienHex = item.MauBienHex,
                TonHeThong = item.TonHeThong,
                ThucTe = item.ThucTe,
                ChenhLech = item.ChenhLech,
                SoBienCoMat = item.SoBienCoMat,
                SoBienThieu = item.SoBienThieu,
                SoBienThua = item.SoBienThua,
                GhiChu = item.GhiChu
            };
            await _dbContext.KiemKeKhoNhomDauBiens.AddAsync(nhomEntity, cancellationToken);
            nhomEntityMap[item.DauBien.Trim()] = nhomEntity.Id;
        }

        // 6. LƯU BẢNG KiemKeKhoChiTietBien
        foreach (var ct in allChiTiets)
        {
            nhomEntityMap.TryGetValue(ct.DauBien.Trim(), out var nhomId);

            var ctEntity = new KiemKeKhoChiTietBien
            {
                CustomerId = customerId,
                KiemKeKhoId = kiemKe.Id!,
                KiemKeKhoNhomDauBienId = nhomId,
                DauBien = ct.DauBien,
                BienKiemSoat = ct.BienKiemSoat,
                KhoBienSoId = ct.KhoBienSoId,
                LoaiBien = ct.LoaiBien,
                TrangThaiKiemKe = ct.TrangThaiKiemKe,
                GhiChu = ct.GhiChu
            };
            await _dbContext.KiemKeKhoChiTietBiens.AddAsync(ctEntity, cancellationToken);
        }

        // 7. ĐỒNG BỘ VÀO BẢNG KhoBienSo KHI XÁC NHẬN HOÀN TẤT
        if (isHoanTat)
        {
            var mauBiensByDauBien = request.DanhSachNhomDauBien
                .Where(x => !string.IsNullOrEmpty(x.DauBien) && !string.IsNullOrEmpty(x.MauBienSoId))
                .ToDictionary(x => x.DauBien.ToUpper(), x => x.MauBienSoId) ?? new Dictionary<string, string?>();

            foreach (var ct in allChiTiets)
            {
                var bienKs = ct.BienKiemSoat.Trim().ToUpperInvariant();
                var dauBien = ct.DauBien.Trim().ToUpperInvariant();

                if (ct.TrangThaiKiemKe == "Thieu")
                {
                    // 1. Biển THIẾU thực tế -> XUẤT KHO (Đánh dấu DaXuatKho = true để giảm trừ tồn kho)
                    KhoBienSo? plate = null;
                    if (!string.IsNullOrWhiteSpace(ct.KhoBienSoId))
                    {
                        plate = await _dbContext.KhoBienSos.FirstOrDefaultAsync(x => x.Id == ct.KhoBienSoId, cancellationToken);
                    }

                    if (plate == null)
                    {
                        plate = await _dbContext.KhoBienSos.FirstOrDefaultAsync(x =>
                            x.BienKiemSoat.ToUpper() == bienKs &&
                            !x.DaXuatKho &&
                            (customerId == null || x.CustomerId == customerId || x.CustomerId == null), cancellationToken);
                    }

                    if (plate != null)
                    {
                        plate.DaXuatKho = true;
                        plate.NgayXuatKho = DateTime.UtcNow;
                        plate.TrangThai = "DaBam";
                        plate.TrangThaiTen = "Đã xuất (Kiểm kê báo thiếu)";
                        plate.GhiChu = string.IsNullOrWhiteSpace(plate.GhiChu)
                            ? $"Xuất kho điều chỉnh kiểm kê báo thiếu theo phiếu {kiemKe.MaPhieuKiemKe}"
                            : $"{plate.GhiChu} | Xuất kho điều chỉnh kiểm kê báo thiếu theo phiếu {kiemKe.MaPhieuKiemKe}";
                    }
                }
                else if (ct.LoaiBien == "BienThua" || ct.TrangThaiKiemKe == "Thua")
                {
                    // 2. Biển phát hiện THỪA thực tế -> NHẬP KHO (Tăng tồn kho: tạo mới hoặc khôi phục trong KhoBienSo)
                    var plate = await _dbContext.KhoBienSos.FirstOrDefaultAsync(x =>
                        x.BienKiemSoat.ToUpper() == bienKs &&
                        (customerId == null || x.CustomerId == customerId || x.CustomerId == null), cancellationToken);

                    if (plate != null)
                    {
                        plate.DaXuatKho = false;
                        plate.NgayXuatKho = null;
                        plate.TrangThai = "DuocBam";
                        plate.TrangThaiTen = "Biển được bấm (Tồn kho)";
                        plate.GhiChu = string.IsNullOrWhiteSpace(plate.GhiChu)
                            ? $"Nhập kho điều chỉnh phát hiện thừa theo phiếu kiểm kê {kiemKe.MaPhieuKiemKe}"
                            : $"{plate.GhiChu} | Nhập kho điều chỉnh phát hiện thừa theo phiếu {kiemKe.MaPhieuKiemKe}";
                    }
                    else
                    {
                        mauBiensByDauBien.TryGetValue(dauBien, out var mauId);

                        var newPlate = new KhoBienSo
                        {
                            CustomerId = customerId,
                            DotNhapKhoId = "KIEMKE_" + kiemKe.MaPhieuKiemKe,
                            BienKiemSoat = bienKs,
                            LoaiXeId = kiemKe.LoaiXeId,
                            MauBienSoId = mauId,
                            TrangThai = "DuocBam",
                            TrangThaiTen = "Biển được bấm (Tồn kho)",
                            LaBienDauGia = false,
                            DaXuatKho = false,
                            NgayNhapKho = DateTime.UtcNow,
                            GhiChu = $"Nhập kho phát hiện thừa thực tế từ phiếu kiểm kê {kiemKe.MaPhieuKiemKe}"
                        };

                        await _dbContext.KhoBienSos.AddAsync(newPlate, cancellationToken);
                    }
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        bool isThemMoi = string.IsNullOrWhiteSpace(request.Id);
        await _logHoatDongService.GhiLogAsync(
            "KiemKe",
            isHoanTat ? "XacNhan" : (isThemMoi ? "ThemMoi" : "CapNhat"),
            kiemKe.MaPhieuKiemKe,
            isHoanTat
                ? $"Xác nhận hoàn tất kiểm kê kho '{kiemKe.MaPhieuKiemKe}': Tồn hệ thống {kiemKe.TongTonHeThong}, Thực tế {kiemKe.TongThucTe}, Chênh lệch {kiemKe.TongChenhLech}. Đã cân bằng tồn kho."
                : (isThemMoi
                    ? $"Khởi tạo phiếu kiểm kê kho '{kiemKe.MaPhieuKiemKe}' ({kiemKe.LoaiPhuongTien}): Tồn hệ thống {kiemKe.TongTonHeThong}, Thực tế {kiemKe.TongThucTe}."
                    : $"Cập nhật phiếu kiểm kê kho '{kiemKe.MaPhieuKiemKe}'."),
            kiemKe.Id,
            null,
            kiemKe.TrangThai,
            isHoanTat ? "Hoàn tất" : "Lưu nháp",
            null,
            kiemKe.CustomerId,
            kiemKe.GhiChu,
            cancellationToken);

        return await LayChiTietKiemKeAsync(kiemKe.Id!, cancellationToken)
            ?? throw new InvalidOperationException("Không thể lấy dữ liệu phiếu kiểm kê vừa lưu.");
    }

    /// <summary>
    /// API Lấy danh sách phiếu kiểm kê kho có phân trang và bộ lọc.
    /// </summary>
    public async Task<PagedResult<KiemKeKhoResponse>> LayDanhSachKiemKeAsync(
        PagedKiemKeKhoRequest request,
        CancellationToken cancellationToken = default)
    {
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        var query = _dbContext.KiemKeKhos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        if (!string.IsNullOrWhiteSpace(request.LoaiPhuongTien))
        {
            query = query.Where(x => x.LoaiPhuongTien == request.LoaiPhuongTien);
        }

        if (!string.IsNullOrWhiteSpace(request.TrangThai))
        {
            query = query.Where(x => x.TrangThai == request.TrangThai);
        }

        if (request.XacNhanHoanTat.HasValue)
        {
            query = query.Where(x => x.XacNhanHoanTat == request.XacNhanHoanTat.Value);
        }

        if (request.TuNgay.HasValue)
        {
            query = query.Where(x => x.NgayKiemKe >= request.TuNgay.Value);
        }

        if (request.DenNgay.HasValue)
        {
            query = query.Where(x => x.NgayKiemKe <= request.DenNgay.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim().ToLower();
            query = query.Where(x =>
                x.MaPhieuKiemKe.ToLower().Contains(kw) ||
                (x.TruongDoanKiemKe != null && x.TruongDoanKiemKe.ToLower().Contains(kw)) ||
                (x.CanBoLapBienBan != null && x.CanBoLapBienBan.ToLower().Contains(kw)) ||
                (x.DiaDiemKiemKe != null && x.DiaDiemKiemKe.ToLower().Contains(kw)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        int pageIndex = request.CurrentPage > 0 ? request.CurrentPage : 1;
        int pageSize = request.PageSize > 0 ? request.PageSize : 10;

        var items = await query
            .OrderByDescending(x => x.NgayKiemKe)
            .ThenByDescending(x => x.Created)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new KiemKeKhoResponse
            {
                Id = x.Id ?? string.Empty,
                CustomerId = x.CustomerId,
                MaPhieuKiemKe = x.MaPhieuKiemKe,
                NgayKiemKe = x.NgayKiemKe,
                ThoiGianKiemKe = x.ThoiGianKiemKe,
                LoaiXeId = x.LoaiXeId,
                LoaiPhuongTien = x.LoaiPhuongTien,
                DiaDiemKiemKe = x.DiaDiemKiemKe,
                TruongDoanKiemKe = x.TruongDoanKiemKe,
                CanBoLapBienBan = x.CanBoLapBienBan,
                TongTonHeThong = x.TongTonHeThong,
                TongThucTe = x.TongThucTe,
                TongChenhLech = x.TongChenhLech,
                TrangThai = x.TrangThai,
                TrangThaiTen = x.XacNhanHoanTat ? "Xác nhận hoàn tất" : "Lưu nháp",
                XacNhanHoanTat = x.XacNhanHoanTat,
                GhiChu = x.GhiChu,
                Created = x.Created,
                CreatedByName = x.CreatedByName
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<KiemKeKhoResponse>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = pageIndex,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// API Lấy thông tin chi tiết 1 phiếu kiểm kê kho theo Id.
    /// </summary>
    public async Task<KiemKeKhoResponse?> LayChiTietKiemKeAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var item = await _dbContext.KiemKeKhos
            .AsNoTracking()
            .Include(x => x.DanhSachNhomDauBien)
            .Include(x => x.DanhSachChiTietBien)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item == null) return null;

        var cid = item.CustomerId;
        var chiTietList = item.DanhSachChiTietBien.ToList();

        // Tải các biển số từ KhoBienSo để bổ sung danh sách biển trong kho nếu chưa lưu snapshot chi tiết đầy đủ
        var platesInDb = await _dbContext.KhoBienSos.AsNoTracking()
            .Where(x => cid == null || x.CustomerId == cid || x.CustomerId == null)
            .Where(x => !x.DaXuatKho || (x.GhiChu != null && x.GhiChu.Contains(item.MaPhieuKiemKe)))
            .Select(x => new
            {
                x.Id,
                x.BienKiemSoat,
                x.MauBienSoId
            })
            .ToListAsync(cancellationToken);

        var nhomDtos = new List<KiemKeNhomDauBienItemDto>();
        foreach (var nhom in item.DanhSachNhomDauBien.OrderBy(x => x.STT))
        {
            var dauBienKey = nhom.DauBien.Trim().ToUpperInvariant();

            // 1. Danh sách biển thừa của nhóm này (Dựa vào biển số để tự xác định đầu biển số nào)
            var danhSachThua = chiTietList
                .Where(x => (x.LoaiBien == "BienThua" || x.TrangThaiKiemKe == "Thua") &&
                            ExtractDauBien(x.BienKiemSoat).Equals(dauBienKey, StringComparison.OrdinalIgnoreCase))
                .Select(x => new BienThuaItemDto
                {
                    Id = x.Id,
                    BienKiemSoat = x.BienKiemSoat,
                    GhiChu = x.GhiChu ?? "Phát hiện thừa thực tế"
                })
                .ToList();

            // 2. Danh sách biển trong kho (Có trạng thái để chuyển từ Có mặt sang thiếu)
            var trongKhoChiTiets = chiTietList
                .Where(x => x.LoaiBien == "TrongKho" &&
                            ExtractDauBien(x.BienKiemSoat).Equals(dauBienKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            List<BienTrongKhoItemDto> danhSachTrongKho;

            if (trongKhoChiTiets.Any())
            {
                // Nếu đã có danh sách các biển trong kho được lưu
                danhSachTrongKho = trongKhoChiTiets
                    .OrderBy(x => x.BienKiemSoat)
                    .Select(x =>
                    {
                        bool isThieu = string.Equals(x.TrangThaiKiemKe, "Thieu", StringComparison.OrdinalIgnoreCase);
                        return new BienTrongKhoItemDto
                        {
                            KhoBienSoId = x.KhoBienSoId ?? string.Empty,
                            BienKiemSoat = x.BienKiemSoat,
                            TrangThaiKiemKe = isThieu ? "Thieu" : "CoMat",
                            IsCoMat = !isThieu,
                            GhiChu = x.GhiChu
                        };
                    })
                    .ToList();
            }
            else
            {
                // Bổ sung từ KhoBienSo đối chiếu với các biển báo thiếu đã lưu
                var thieuSet = chiTietList
                    .Where(x => x.TrangThaiKiemKe == "Thieu" &&
                                ExtractDauBien(x.BienKiemSoat).Equals(dauBienKey, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.BienKiemSoat.ToUpper())
                    .ToHashSet();

                danhSachTrongKho = platesInDb
                    .Where(p => ExtractDauBien(p.BienKiemSoat).Equals(dauBienKey, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.BienKiemSoat)
                    .Select(p =>
                    {
                        bool isThieu = thieuSet.Contains(p.BienKiemSoat.ToUpper());
                        return new BienTrongKhoItemDto
                        {
                            KhoBienSoId = p.Id ?? string.Empty,
                            BienKiemSoat = p.BienKiemSoat,
                            TrangThaiKiemKe = isThieu ? "Thieu" : "CoMat",
                            IsCoMat = !isThieu,
                            GhiChu = isThieu ? "Kiểm kê báo thiếu" : "Có mặt"
                        };
                    })
                    .ToList();
            }

            int coMat = danhSachTrongKho.Count(x => x.IsCoMat);
            int thieu = danhSachTrongKho.Count(x => !x.IsCoMat);
            int thua = danhSachThua.Count;

            nhomDtos.Add(new KiemKeNhomDauBienItemDto
            {
                STT = nhom.STT,
                DauBien = nhom.DauBien,
                MauBienSoId = nhom.MauBienSoId,
                MauBien = nhom.MauBien,
                MauBienHex = nhom.MauBienHex,
                TonHeThong = nhom.TonHeThong,
                ThucTe = nhom.ThucTe,
                ChenhLech = nhom.ChenhLech,
                SoBienCoMat = nhom.SoBienCoMat > 0 ? nhom.SoBienCoMat : coMat,
                SoBienThieu = nhom.SoBienThieu > 0 ? nhom.SoBienThieu : thieu,
                SoBienThua = nhom.SoBienThua > 0 ? nhom.SoBienThua : thua,
                GhiChu = nhom.GhiChu ?? "Khớp sổ sách",
                DanhSachBienTrongKho = danhSachTrongKho,
                DanhSachBienThua = danhSachThua
            });
        }

        return new KiemKeKhoResponse
        {
            Id = item.Id ?? string.Empty,
            CustomerId = item.CustomerId,
            MaPhieuKiemKe = item.MaPhieuKiemKe,
            NgayKiemKe = item.NgayKiemKe,
            ThoiGianKiemKe = item.ThoiGianKiemKe,
            LoaiXeId = item.LoaiXeId,
            LoaiPhuongTien = item.LoaiPhuongTien,
            DiaDiemKiemKe = item.DiaDiemKiemKe,
            TruongDoanKiemKe = item.TruongDoanKiemKe,
            CanBoLapBienBan = item.CanBoLapBienBan,
            TongTonHeThong = item.TongTonHeThong,
            TongThucTe = item.TongThucTe,
            TongChenhLech = item.TongChenhLech,
            TrangThai = item.TrangThai,
            TrangThaiTen = item.XacNhanHoanTat ? "Xác nhận hoàn tất" : "Lưu nháp",
            XacNhanHoanTat = item.XacNhanHoanTat,
            GhiChu = item.GhiChu,
            Created = item.Created,
            CreatedByName = item.CreatedByName,
            DanhSachNhomDauBien = nhomDtos
        };
    }

    /// <summary>
    /// API Xóa phiếu kiểm kê kho theo Id.
    /// </summary>
    public async Task<bool> XoaKiemKeKhoAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;

        var item = await _dbContext.KiemKeKhos
            .Include(x => x.DanhSachNhomDauBien)
            .Include(x => x.DanhSachChiTietBien)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item == null) return false;

        _dbContext.KiemKeKhoNhomDauBiens.RemoveRange(item.DanhSachNhomDauBien);
        _dbContext.KiemKeKhoChiTietBiens.RemoveRange(item.DanhSachChiTietBien);
        _dbContext.KiemKeKhos.Remove(item);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logHoatDongService.GhiLogAsync(
            "KiemKe",
            "Xoa",
            item.MaPhieuKiemKe,
            $"Xóa phiếu kiểm kê kho '{item.MaPhieuKiemKe}' ({item.LoaiPhuongTien}) khỏi hệ thống.",
            item.Id,
            null,
            "DaXoa",
            "Đã xóa",
            null,
            item.CustomerId,
            item.GhiChu,
            cancellationToken);

        return true;
    }

    /// <summary>
    /// Hàm tách đầu biển từ biển số kiểm soát (Ví dụ: "30B-207.66" -> "30B-207", "29E-794.01" -> "29E-794").
    /// </summary>
    public static string ExtractDauBien(string bienKiemSoat, string? dauBienMacDinh = null)
    {
        if (string.IsNullOrWhiteSpace(bienKiemSoat))
            return !string.IsNullOrWhiteSpace(dauBienMacDinh) ? dauBienMacDinh : "Chưa xác định";

        var clean = bienKiemSoat.Trim().ToUpperInvariant();

        // 1. Chuẩn biển 5 số có dấu '.' (Ví dụ: 30B-207.66 -> 30B-207, 29E-794.01 -> 29E-794, 29AH-316.05 -> 29AH-316)
        var dotIndex = clean.LastIndexOf('.');
        if (dotIndex > 0)
        {
            return clean[..dotIndex];
        }

        // 2. Biển 5 số không có dấu '.' nhưng có gạch ngang (Ví dụ: 30B-20766 -> 30B-207, 29AH-31605 -> 29AH-316)
        var match5 = Regex.Match(clean, @"^([0-9]{2}[A-Z]{1,2}-[0-9]{3})[0-9]{2}$");
        if (match5.Success)
        {
            return match5.Groups[1].Value;
        }

        // 3. Biển liền không gạch không chấm (Ví dụ: 30B20766 -> 30B-207, 29E79401 -> 29E-794)
        var matchNoDash = Regex.Match(clean, @"^([0-9]{2}[A-Z]{1,2})([0-9]{3})[0-9]{2}$");
        if (matchNoDash.Success)
        {
            return $"{matchNoDash.Groups[1].Value}-{matchNoDash.Groups[2].Value}";
        }

        // 4. Nếu có dấu '-' và sau đó có 3 ký tự số (Ví dụ: 30B-207)
        var dashIndex = clean.IndexOf('-');
        if (dashIndex > 0 && dashIndex + 4 <= clean.Length)
        {
            return clean.Substring(0, dashIndex + 4);
        }

        return !string.IsNullOrWhiteSpace(dauBienMacDinh) ? dauBienMacDinh : clean;
    }

    /// <summary>
    /// Tự động xác định dải đầu biển số từ biển kiểm soát (Ví dụ: "30B-207.66" -> "30B-207", "29E-794.02" -> "29E-794").
    /// </summary>
    public async Task<XacDinhDauBienResponse> XacDinhDauBienTuBienSoAsync(
        string bienKiemSoat,
        string? customerId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bienKiemSoat))
        {
            throw new ArgumentException("Vui lòng truyền biển kiểm soát.");
        }

        var clean = bienKiemSoat.Trim().ToUpperInvariant();
        var dauBien = ExtractDauBien(clean);
        var cid = customerId ?? _currentUserService.CustomerId;

        // 1. Kiểm tra xem biển này đã có trong kho chưa
        var existingPlate = await _dbContext.KhoBienSos.AsNoTracking()
            .Where(x => x.BienKiemSoat.ToUpper() == clean)
            .Where(x => cid == null || x.CustomerId == cid || x.CustomerId == null)
            .Select(x => new { x.MauBienSoId, x.DaXuatKho, x.TrangThai })
            .FirstOrDefaultAsync(cancellationToken);

        string? mauBienSoId = existingPlate?.MauBienSoId;
        string mauBienTen = "Biển Trắng";
        string mauBienHex = "#FFFFFF";
        bool tonTaiTrongKho = existingPlate != null && !existingPlate.DaXuatKho && existingPlate.TrangThai == "DuocBam";

        // 2. Nếu chưa có biển cụ thể, tìm màu theo dải đầu biển trong kho
        if (string.IsNullOrWhiteSpace(mauBienSoId))
        {
            mauBienSoId = await _dbContext.KhoBienSos.AsNoTracking()
                .Where(x => x.BienKiemSoat.ToUpper().StartsWith(dauBien))
                .Where(x => cid == null || x.CustomerId == cid || x.CustomerId == null)
                .Select(x => x.MauBienSoId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(mauBienSoId))
        {
            var mb = await _dbContext.MauBienSos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == mauBienSoId, cancellationToken);
            if (mb != null)
            {
                mauBienTen = mb.Ten.StartsWith("Biển", StringComparison.OrdinalIgnoreCase) ? mb.Ten : $"Biển {mb.Ten}";
                mauBienHex = mb.MaMauHex ?? "#FFFFFF";
            }
        }

        return new XacDinhDauBienResponse
        {
            BienKiemSoat = clean,
            DauBien = dauBien,
            MauBienSoId = mauBienSoId,
            MauBien = mauBienTen,
            MauBienHex = mauBienHex,
            TonTaiTrongKho = tonTaiTrongKho
        };
    }
}

/// <summary>
/// Record nội bộ biểu diễn chi tiết đối chiếu 1 biển số trong phiếu kiểm kê.
/// </summary>
internal sealed record ChiTietKiemKeRecord(
    string DauBien,
    string BienKiemSoat,
    string? KhoBienSoId,
    string LoaiBien,
    string TrangThaiKiemKe,
    string? GhiChu
);
