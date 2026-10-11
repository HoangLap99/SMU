using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;
using SMU.Infrastructure.Data;

namespace SMU.Application.Services;

/// <summary>
/// Service triển khai các nghiệp vụ Quản lý Tồn kho biển số xe:
/// 1. Bảng tổng hợp tồn kho theo đầu biển
/// 2. Danh sách các biển số chưa xuất khi click vào chi tiết 1 nhóm đầu biển
/// 3. Dữ liệu cấu hình cho bộ lọc tìm kiếm
/// </summary>
public class TonKhoService : ITonKhoService
{
    private readonly PostgresDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TonKhoService> _logger;

    public TonKhoService(
        PostgresDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<TonKhoService> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// API 1: Lấy Bảng Tổng Hợp Tồn Kho Theo Đầu Biển (áp dụng theo bộ lọc hiện tại).
    /// </summary>
    public async Task<TongHopTonKhoResponse> LayTongHopTonKhoAsync(
        FilterTonKhoRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new FilterTonKhoRequest();
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        var query = _dbContext.KhoBienSos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        // 1. Lọc theo từ khóa tìm số biển
        if (!string.IsNullOrWhiteSpace(request.TimSoBien))
        {
            var kw = request.TimSoBien.Trim().ToLower();
            query = query.Where(x => x.BienKiemSoat.ToLower().Contains(kw));
        }

        // 2. Lọc theo loại phương tiện / loại xe
        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            query = query.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        // 3. Lọc theo màu biển
        if (!string.IsNullOrWhiteSpace(request.MauBienSoId))
        {
            query = query.Where(x => x.MauBienSoId == request.MauBienSoId);
        }

        // 4. Lọc theo trạng thái
        if (!string.IsNullOrWhiteSpace(request.TrangThai))
        {
            if (request.TrangThai == "DuocBam")
            {
                query = query.Where(x => !x.DaXuatKho && x.TrangThai == "DuocBam" && !x.LaBienDauGia);
            }
            else if (request.TrangThai == "DaBam")
            {
                query = query.Where(x => x.DaXuatKho || x.TrangThai == "DaBam");
            }
            else if (request.TrangThai == "ChoDauGia")
            {
                query = query.Where(x => x.LaBienDauGia || x.TrangThai == "ChoDauGia");
            }
            else
            {
                query = query.Where(x => x.TrangThai == request.TrangThai);
            }
        }

        // 5. Lọc theo loại thời gian / khoảng ngày
        if (request.FromDate.HasValue)
        {
            query = query.Where(x => x.NgayNhapKho >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(x => x.NgayNhapKho <= request.ToDate.Value);
        }

        // Lấy danh sách biển số sau khi lọc
        var plates = await query
            .Select(x => new
            {
                x.Id,
                x.BienKiemSoat,
                x.LoaiXeId,
                x.MauBienSoId,
                x.DauBienSoId,
                x.TrangThai,
                x.DaXuatKho,
                x.LaBienDauGia
            })
            .ToListAsync(cancellationToken);

        if (plates.Count == 0)
        {
            return new TongHopTonKhoResponse
            {
                TongSoNhomDauBien = 0,
                TongTonKhoDuocBam = 0,
                TongDaBam = 0,
                TongChoDauGia = 0,
                TongSoBien = 0,
                Items = new List<NhomDauBienTonKhoItemResponse>()
            };
        }

        // Tải các danh mục liên quan để hiển thị tên và mã màu
        var loaiXeMap = await _dbContext.LoaiXes.AsNoTracking()
            .Where(x => x.Id != null)
            .ToDictionaryAsync(x => x.Id!, x => x.Ten ?? "Ô tô", cancellationToken);

        var mauBienMap = await _dbContext.MauBienSos.AsNoTracking()
            .Where(x => x.Id != null)
            .ToDictionaryAsync(x => x.Id!, x => new { Ten = x.Ten ?? "Trắng", Hex = x.MaMauHex ?? "#FFFFFF" }, cancellationToken);

        var dauBienMap = await _dbContext.DauBienSos.AsNoTracking()
            .Where(x => x.Id != null)
            .ToDictionaryAsync(x => x.Id!, x => x.KyHieuSeries ?? x.Ten ?? "Đầu biển", cancellationToken);

        // Group by theo Đầu biển, Loại xe, Màu biển
        var groups = plates
            .GroupBy(p =>
            {
                var dauBienTen = (p.DauBienSoId != null && dauBienMap.TryGetValue(p.DauBienSoId, out var db)) ? db : null;
                var dauBien = ExtractDauBien(p.BienKiemSoat, dauBienTen);
                return new
                {
                    DauBien = dauBien,
                    LoaiXeId = p.LoaiXeId ?? string.Empty,
                    MauBienSoId = p.MauBienSoId ?? string.Empty
                };
            })
            .Select(g =>
            {
                var tonKhoDuocBam = g.Count(x => !x.DaXuatKho && x.TrangThai == "DuocBam" && !x.LaBienDauGia);
                var daBam = g.Count(x => x.DaXuatKho || x.TrangThai == "DaBam");
                var choDauGia = g.Count(x => x.LaBienDauGia || x.TrangThai == "ChoDauGia");
                var tongBien = g.Count();

                var loaiPhuongTien = loaiXeMap.TryGetValue(g.Key.LoaiXeId, out var lx) ? lx : "Ô tô";
                string mauBien = "Trắng";
                string mauBienHex = "#FFFFFF";

                if (mauBienMap.TryGetValue(g.Key.MauBienSoId, out var mb))
                {
                    mauBien = mb.Ten;
                    mauBienHex = mb.Hex;
                }

                // Tìm DauBienSoId đại diện nếu có
                var dauBienSoId = g.FirstOrDefault(x => !string.IsNullOrEmpty(x.DauBienSoId))?.DauBienSoId;

                return new NhomDauBienTonKhoItemResponse
                {
                    DauBien = g.Key.DauBien,
                    DauBienSoId = dauBienSoId,
                    LoaiXeId = string.IsNullOrEmpty(g.Key.LoaiXeId) ? null : g.Key.LoaiXeId,
                    LoaiPhuongTien = loaiPhuongTien,
                    MauBienSoId = string.IsNullOrEmpty(g.Key.MauBienSoId) ? null : g.Key.MauBienSoId,
                    MauBien = mauBien,
                    MauBienHex = mauBienHex,
                    TonKhoDuocBam = tonKhoDuocBam,
                    DaBam = daBam,
                    ChoDauGia = choDauGia,
                    TongBien = tongBien
                };
            });

        // Lọc thêm theo đầu biển nếu người dùng chọn cụ thể
        if (!string.IsNullOrWhiteSpace(request.DauBien))
        {
            var dbFilter = request.DauBien.Trim().ToUpperInvariant();
            groups = groups.Where(x => x.DauBien.ToUpperInvariant().Contains(dbFilter));
        }

        // Sắp xếp: Ưu tiên các nhóm có số lượng tồn kho (được bấm) cao hơn lên trước (như trong ảnh mẫu)
        var itemsList = groups
            .OrderByDescending(x => x.TonKhoDuocBam)
            .ThenByDescending(x => x.TongBien)
            .ThenBy(x => x.DauBien)
            .ToList();

        // Gán STT 1, 2, 3...
        int stt = 1;
        foreach (var item in itemsList)
        {
            item.STT = stt++;
        }

        // Tính dòng tổng cộng
        var tongSoNhomDauBien = itemsList.Count;
        var tongTonKhoDuocBam = itemsList.Sum(x => x.TonKhoDuocBam);
        var tongDaBam = itemsList.Sum(x => x.DaBam);
        var tongChoDauGia = itemsList.Sum(x => x.ChoDauGia);
        var tongSoBien = itemsList.Sum(x => x.TongBien);

        return new TongHopTonKhoResponse
        {
            TongSoNhomDauBien = tongSoNhomDauBien,
            TongTonKhoDuocBam = tongTonKhoDuocBam,
            TongDaBam = tongDaBam,
            TongChoDauGia = tongChoDauGia,
            TongSoBien = tongSoBien,
            Items = itemsList
        };
    }

    /// <summary>
    /// API 2: Lấy danh sách các biển số CHƯA XUẤT KHO khi click vào nút "Chi tiết" của 1 nhóm đầu biển.
    /// Điều kiện: !DaXuatKho &amp;&amp; TrangThai == "DuocBam" &amp;&amp; !LaBienDauGia.
    /// </summary>
    public async Task<PagedResult<BienSoChuaXuatItemResponse>> LayDanhSachBienSoChuaXuatAsync(
        ChiTietTonKhoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        // Base query: Biển số chưa xuất kho, được bấm và không đem đấu giá
        var query = _dbContext.KhoBienSos
            .AsNoTracking()
            .Where(x => !x.DaXuatKho && x.TrangThai == "DuocBam" && !x.LaBienDauGia);

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        // Lọc theo Đầu biển (ví dụ "29AH-316", "30B-532", "29E-895" hoặc "30B-207")
        if (!string.IsNullOrWhiteSpace(request.DauBien))
        {
            var dauBienFilter = request.DauBien.Trim().ToUpperInvariant();
            query = query.Where(x => x.BienKiemSoat.ToUpper().StartsWith(dauBienFilter));
        }

        // Lọc thêm theo Loại xe nếu có
        if (!string.IsNullOrWhiteSpace(request.LoaiXeId))
        {
            query = query.Where(x => x.LoaiXeId == request.LoaiXeId);
        }

        // Lọc thêm theo Màu biển nếu có
        if (!string.IsNullOrWhiteSpace(request.MauBienSoId))
        {
            query = query.Where(x => x.MauBienSoId == request.MauBienSoId);
        }

        // Lọc theo từ khóa số biển nếu tìm kiếm trong modal chi tiết
        if (!string.IsNullOrWhiteSpace(request.KeyWord))
        {
            var kw = request.KeyWord.Trim().ToLower();
            query = query.Where(x => x.BienKiemSoat.ToLower().Contains(kw));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageIndex = request.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 50;

        var plates = await query
            .OrderBy(x => x.ThuTu)
            .ThenBy(x => x.BienKiemSoat)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (plates.Count == 0)
        {
            return new PagedResult<BienSoChuaXuatItemResponse>
            {
                Items = new List<BienSoChuaXuatItemResponse>(),
                TotalCount = totalCount,
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        // Tải thông tin danh mục để hiển thị tên
        var loaiXeIds = plates.Select(p => p.LoaiXeId).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().Distinct().ToList();
        var mauBienIds = plates.Select(p => p.MauBienSoId).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().Distinct().ToList();
        var dotNhapIds = plates.Select(p => p.DotNhapKhoId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

        var loaiXes = await _dbContext.LoaiXes.AsNoTracking()
            .Where(x => x.Id != null && loaiXeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id!, x => x.Ten ?? "Ô tô", cancellationToken);

        var mauBiens = await _dbContext.MauBienSos.AsNoTracking()
            .Where(x => x.Id != null && mauBienIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id!, x => new { Ten = x.Ten ?? "Trắng", Hex = x.MaMauHex ?? "#FFFFFF" }, cancellationToken);

        var dotNhaps = await _dbContext.DotNhapKhos.AsNoTracking()
            .Where(x => x.Id != null && dotNhapIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id!, x => x.MaDotNhap, cancellationToken);

        int currentStt = (pageIndex - 1) * pageSize + 1;
        var items = plates.Select(p =>
        {
            var lxTen = (p.LoaiXeId != null && loaiXes.TryGetValue(p.LoaiXeId, out var lx)) ? lx : "Ô tô";
            string mbTen = "Trắng";
            string mbHex = "#FFFFFF";

            if (p.MauBienSoId != null && mauBiens.TryGetValue(p.MauBienSoId, out var mb))
            {
                mbTen = mb.Ten;
                mbHex = mb.Hex;
            }

            var maDotNhap = (p.DotNhapKhoId != null && dotNhaps.TryGetValue(p.DotNhapKhoId, out var dn)) ? dn : null;
            var dauBien = ExtractDauBien(p.BienKiemSoat, request.DauBien);

            return new BienSoChuaXuatItemResponse
            {
                STT = currentStt++,
                Id = p.Id ?? string.Empty,
                BienKiemSoat = p.BienKiemSoat,
                DauBien = dauBien,
                LoaiXeId = p.LoaiXeId,
                LoaiPhuongTien = lxTen,
                MauBienSoId = p.MauBienSoId,
                MauBien = mbTen,
                MauBienHex = mbHex,
                TrangThai = p.TrangThai,
                TrangThaiTen = p.TrangThaiTen,
                DaXuatKho = false,
                NgayNhapKho = p.NgayNhapKho,
                DotNhapKhoId = p.DotNhapKhoId,
                MaDotNhap = maDotNhap,
                GhiChu = p.GhiChu
            };
        }).ToList();

        return new PagedResult<BienSoChuaXuatItemResponse>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = pageIndex,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// API 3: Lấy dữ liệu cấu hình bộ lọc tìm kiếm tồn kho (Danh sách đầu biển kèm số lượng, Loại xe, Màu biển, Trạng thái).
    /// </summary>
    public async Task<BoLocTonKhoResponse> LayThongTinBoLocAsync(
        string? customerId = null,
        CancellationToken cancellationToken = default)
    {
        customerId ??= _currentUserService.CustomerId;

        var query = _dbContext.KhoBienSos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        // Lấy danh sách biển số để tính số lượng tồn theo từng đầu biển
        var plates = await query
            .Select(x => new
            {
                x.BienKiemSoat,
                x.TrangThai,
                x.DaXuatKho,
                x.LaBienDauGia
            })
            .ToListAsync(cancellationToken);

        // Group theo đầu biển để sinh dropdown "Tất cả đầu biển (34)"
        var dauBienGroups = plates
            .GroupBy(p => ExtractDauBien(p.BienKiemSoat, null))
            .Select(g =>
            {
                var tonCount = g.Count(x => !x.DaXuatKho && x.TrangThai == "DuocBam" && !x.LaBienDauGia);
                return new DauBienFilterOption
                {
                    DauBien = g.Key,
                    SoLuongTon = tonCount,
                    TieuDeHienThi = $"{g.Key} ({tonCount} tồn)"
                };
            })
            .OrderByDescending(x => x.SoLuongTon)
            .ThenBy(x => x.DauBien)
            .ToList();

        var tongSoNhomDauBien = dauBienGroups.Count;
        var tongSoBienTon = dauBienGroups.Sum(x => x.SoLuongTon);

        // Danh mục Loại xe
        var loaiXes = await _dbContext.LoaiXes.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.ThuTu)
            .Select(x => new DanhMucOptionResponse
            {
                Id = x.Id ?? string.Empty,
                Ma = x.Ma,
                Ten = x.Ten ?? x.Ma,
                Extra = x.NhomXe
            })
            .ToListAsync(cancellationToken);

        // Danh mục Màu biển
        var mauBiens = await _dbContext.MauBienSos.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.ThuTu)
            .Select(x => new DanhMucOptionResponse
            {
                Id = x.Id ?? string.Empty,
                Ma = x.Ma,
                Ten = x.Ten ?? x.Ma,
                Extra = x.MaMauHex
            })
            .ToListAsync(cancellationToken);

        // Danh mục Trạng thái
        var trangThais = new List<DanhMucOptionResponse>
        {
            new() { Id = "DuocBam", Ma = "DuocBam", Ten = "Tồn kho (Được bấm)", Extra = "success" },
            new() { Id = "DaBam", Ma = "DaBam", Ten = "Đã bấm (Đã cấp)", Extra = "secondary" },
            new() { Id = "ChoDauGia", Ma = "ChoDauGia", Ten = "Chờ đấu giá", Extra = "warning" },
            new() { Id = "ThuHoi", Ma = "ThuHoi", Ten = "Thu hồi", Extra = "danger" }
        };

        return new BoLocTonKhoResponse
        {
            DanhSachDauBien = dauBienGroups,
            DanhSachLoaiXe = loaiXes,
            DanhSachMauBien = mauBiens,
            DanhSachTrangThai = trangThais,
            TongSoNhomDauBien = tongSoNhomDauBien,
            TongSoBienTon = tongSoBienTon
        };
    }

    /// <summary>
    /// API 4: Lấy Ma Trận 100 Số Dải Biển từ .00 đến .99 trong bảng KhoBienSo.
    /// Input là đầu biển số (Ví dụ: "29AC-963"), Output là dãy 100 số kèm trạng thái và màu sắc tương ứng.
    /// </summary>
    public async Task<MaTranDaiSoResponse> LayMaTranDaiSoAsync(
        string dauBien,
        string? customerId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dauBien))
        {
            throw new ArgumentException("Vui lòng cung cấp đầu biển số cần tra cứu ma trận.");
        }

        dauBien = dauBien.Trim().ToUpperInvariant();
        customerId ??= _currentUserService.CustomerId;

        // Truy vấn tất cả biển số trong kho có đầu biển tương ứng
        var query = _dbContext.KhoBienSos
            .AsNoTracking()
            .Where(x => x.BienKiemSoat.ToUpper().StartsWith(dauBien + ".") || x.BienKiemSoat.ToUpper().StartsWith(dauBien));

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(x => x.CustomerId == customerId || x.CustomerId == null);
        }

        var plates = await query.ToListAsync(cancellationToken);

        // Tải thông tin danh mục loại xe, màu biển để gán nếu có
        var loaiXeIds = plates.Select(p => p.LoaiXeId).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().Distinct().ToList();
        var mauBienIds = plates.Select(p => p.MauBienSoId).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().Distinct().ToList();

        var loaiXes = await _dbContext.LoaiXes.AsNoTracking()
            .Where(x => x.Id != null && loaiXeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id!, x => x.Ten ?? "Ô tô", cancellationToken);

        var mauBiens = await _dbContext.MauBienSos.AsNoTracking()
            .Where(x => x.Id != null && mauBienIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id!, x => new { Ten = x.Ten ?? "Trắng", Hex = x.MaMauHex ?? "#FFFFFF" }, cancellationToken);

        // Danh sách chú giải màu sắc chuẩn theo giao diện
        var chuGiaiList = new List<ChuGiaiMauItem>
        {
            new() { Ma = "DuocBam", Ten = "Biển được bấm (Tồn)", MaMauHex = "#10B981", MoTa = "Biển số tồn kho sẵn sàng bấm cấp phát" },
            new() { Ma = "DaBam", Ten = "Biển đã bấm", MaMauHex = "#374151", MoTa = "Biển số đã được bấm / xuất kho" },
            new() { Ma = "ChoDauGia", Ten = "Chờ đấu giá", MaMauHex = "#8B5CF6", MoTa = "Biển số đưa vào diện kho đấu giá" },
            new() { Ma = "DauGiaThanhCong", Ten = "Đấu giá thành công", MaMauHex = "#F59E0B", MoTa = "Biển số đã trúng đấu giá thành công" },
            new() { Ma = "ChuaNhap", Ten = "Chưa nhập", MaMauHex = "#E5E7EB", MoTa = "Số còn trống trong dải (chưa khai báo nhập kho)" }
        };

        // Sinh danh sách 100 số từ .00 đến .99
        var danhSachSo = new List<MaTranSoItemResponse>(100);

        for (int i = 0; i < 100; i++)
        {
            var soDuoi = i.ToString("D2");
            var kyHieu = "." + soDuoi;
            var bienKiemSoatDuKien = $"{dauBien}.{soDuoi}";

            // Tìm biển số trong kho khớp với số đuôi này
            var plate = plates.FirstOrDefault(p =>
                p.BienKiemSoat.Equals(bienKiemSoatDuKien, StringComparison.OrdinalIgnoreCase) ||
                p.BienKiemSoat.EndsWith("." + soDuoi, StringComparison.OrdinalIgnoreCase) ||
                p.BienKiemSoat.EndsWith("-" + soDuoi, StringComparison.OrdinalIgnoreCase) ||
                p.BienKiemSoat.EndsWith(soDuoi, StringComparison.OrdinalIgnoreCase));

            if (plate != null)
            {
                string trangThai;
                string trangThaiTen;
                string mauSacHienThi;
                string mauSacClass;

                if (plate.LaBienDauGia)
                {
                    if (plate.TrangThai == "DauGiaThanhCong" || (plate.DaXuatKho && plate.TrangThai != "DuocBam"))
                    {
                        trangThai = "DauGiaThanhCong";
                        trangThaiTen = "ĐẤU GIÁ THÀNH CÔNG";
                        mauSacHienThi = "#F59E0B";
                        mauSacClass = "warning";
                    }
                    else
                    {
                        trangThai = "ChoDauGia";
                        trangThaiTen = "CHỜ ĐẤU GIÁ";
                        mauSacHienThi = "#8B5CF6";
                        mauSacClass = "purple";
                    }
                }
                else
                {
                    if (plate.DaXuatKho || plate.TrangThai == "DaBam")
                    {
                        trangThai = "DaBam";
                        trangThaiTen = "ĐÃ BẤM";
                        mauSacHienThi = "#374151";
                        mauSacClass = "dark";
                    }
                    else if (plate.TrangThai == "ThuHoi")
                    {
                        trangThai = "ThuHoi";
                        trangThaiTen = "THU HỒI";
                        mauSacHienThi = "#EF4444";
                        mauSacClass = "danger";
                    }
                    else
                    {
                        trangThai = "DuocBam";
                        trangThaiTen = "ĐƯỢC BẤM";
                        mauSacHienThi = "#10B981";
                        mauSacClass = "success";
                    }
                }

                var lxTen = (plate.LoaiXeId != null && loaiXes.TryGetValue(plate.LoaiXeId, out var lx)) ? lx : "Ô tô";
                string mbTen = "Trắng";
                string mbHex = "#FFFFFF";

                if (plate.MauBienSoId != null && mauBiens.TryGetValue(plate.MauBienSoId, out var mb))
                {
                    mbTen = mb.Ten;
                    mbHex = mb.Hex;
                }

                danhSachSo.Add(new MaTranSoItemResponse
                {
                    STT = i,
                    SoDuoi = soDuoi,
                    KyHieuHienThi = kyHieu,
                    Id = plate.Id,
                    BienKiemSoat = plate.BienKiemSoat,
                    DaCoTrongKho = true,
                    TrangThai = trangThai,
                    TrangThaiTen = trangThaiTen,
                    MauSacHienThi = mauSacHienThi,
                    MauSacClass = mauSacClass,
                    LoaiXeId = plate.LoaiXeId,
                    LoaiPhuongTien = lxTen,
                    MauBienSoId = plate.MauBienSoId,
                    MauBien = mbTen,
                    MauBienHex = mbHex,
                    LaBienDauGia = plate.LaBienDauGia,
                    DaXuatKho = plate.DaXuatKho,
                    NgayNhapKho = plate.NgayNhapKho,
                    NgayXuatKho = plate.NgayXuatKho,
                    GhiChu = plate.GhiChu
                });
            }
            else
            {
                // Số chưa nhập vào kho (Trống)
                danhSachSo.Add(new MaTranSoItemResponse
                {
                    STT = i,
                    SoDuoi = soDuoi,
                    KyHieuHienThi = kyHieu,
                    Id = null,
                    BienKiemSoat = bienKiemSoatDuKien,
                    DaCoTrongKho = false,
                    TrangThai = "ChuaNhap",
                    TrangThaiTen = "TRỐNG",
                    MauSacHienThi = "#E5E7EB",
                    MauSacClass = "empty"
                });
            }
        }

        var soDuocBam = danhSachSo.Count(x => x.TrangThai == "DuocBam");
        var soDaBam = danhSachSo.Count(x => x.TrangThai == "DaBam");
        var soChoDauGia = danhSachSo.Count(x => x.TrangThai == "ChoDauGia");
        var soDauGiaThanhCong = danhSachSo.Count(x => x.TrangThai == "DauGiaThanhCong");
        var soChuaNhap = danhSachSo.Count(x => x.TrangThai == "ChuaNhap");
        var tongTrongKho = 100 - soChuaNhap;

        return new MaTranDaiSoResponse
        {
            DauBien = dauBien,
            TieuDe = $"Ma Trận 100 Số Dải Biển: {dauBien}",
            TongSoBienDaiSo = 100,
            TongSoBienTrongKho = tongTrongKho,
            SoBienDuocBam = soDuocBam,
            SoBienDaBam = soDaBam,
            SoBienChoDauGia = soChoDauGia,
            SoBienDauGiaThanhCong = soDauGiaThanhCong,
            SoBienChuaNhap = soChuaNhap,
            DanhSachChuGiai = chuGiaiList,
            DanhSachSo = danhSachSo
        };
    }

    /// <summary>
    /// API 5: Cập nhật trạng thái của 1 số trong ma trận dải số.
    /// </summary>
    public async Task<bool> CapNhatTrangThaiSoAsync(
        CapNhatTrangThaiSoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var dauBien = request.DauBien.Trim().ToUpperInvariant();
        var soDuoi = request.SoDuoi.Trim().PadLeft(2, '0');
        var bienKiemSoat = $"{dauBien}.{soDuoi}";
        var customerId = request.CustomerId ?? _currentUserService.CustomerId;

        var plate = await _dbContext.KhoBienSos
            .FirstOrDefaultAsync(x => (x.BienKiemSoat == bienKiemSoat || x.BienKiemSoat.EndsWith("." + soDuoi))
                                   && x.BienKiemSoat.StartsWith(dauBien)
                                   && (customerId == null || x.CustomerId == customerId || x.CustomerId == null), cancellationToken);

        if (plate == null)
        {
            // Nếu chưa có trong kho, tự động tạo mới bản ghi với trạng thái được chỉ định
            plate = new Core.Entities.KhoBienSo
            {
                CustomerId = customerId,
                DotNhapKhoId = "MATRAN_MANUAL",
                ThuTu = int.TryParse(soDuoi, out var num) ? num : 1,
                BienKiemSoat = bienKiemSoat,
                TrangThai = request.TrangThaiMoi,
                TrangThaiTen = request.TrangThaiMoi == "DuocBam" ? "Biển được bấm (Tồn kho)" :
                               request.TrangThaiMoi == "DaBam" ? "Đã bấm (Đã cấp)" :
                               request.TrangThaiMoi == "ChoDauGia" ? "Chờ đấu giá" :
                               request.TrangThaiMoi == "ThuHoi" ? "Thu hồi" : request.TrangThaiMoi,
                LaBienDauGia = request.LaBienDauGia ?? (request.TrangThaiMoi == "ChoDauGia" || request.TrangThaiMoi == "DauGiaThanhCong"),
                DaXuatKho = request.TrangThaiMoi == "DaBam",
                NgayXuatKho = request.TrangThaiMoi == "DaBam" ? DateTime.UtcNow : null,
                GhiChu = request.GhiChu ?? "Cập nhật trạng thái từ giao diện ma trận 100 số",
                NgayNhapKho = DateTime.UtcNow
            };

            await _dbContext.KhoBienSos.AddAsync(plate, cancellationToken);
        }
        else
        {
            plate.TrangThai = request.TrangThaiMoi;
            plate.TrangThaiTen = request.TrangThaiMoi == "DuocBam" ? "Biển được bấm (Tồn kho)" :
                                 request.TrangThaiMoi == "DaBam" ? "Đã bấm (Đã cấp)" :
                                 request.TrangThaiMoi == "ChoDauGia" ? "Chờ đấu giá" :
                                 request.TrangThaiMoi == "ThuHoi" ? "Thu hồi" : request.TrangThaiMoi;

            if (request.LaBienDauGia.HasValue)
            {
                plate.LaBienDauGia = request.LaBienDauGia.Value;
            }
            else if (request.TrangThaiMoi == "ChoDauGia")
            {
                plate.LaBienDauGia = true;
            }

            if (request.TrangThaiMoi == "DaBam")
            {
                plate.DaXuatKho = true;
                plate.NgayXuatKho ??= DateTime.UtcNow;
            }
            else if (request.TrangThaiMoi == "DuocBam")
            {
                plate.DaXuatKho = false;
                plate.NgayXuatKho = null;
            }

            if (!string.IsNullOrWhiteSpace(request.GhiChu))
            {
                plate.GhiChu = request.GhiChu;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã cập nhật trạng thái biển số '{BienKiemSoat}' sang '{TrangThai}'",
            plate.BienKiemSoat, plate.TrangThai);

        return true;
    }

    /// <summary>
    /// Helper: Tách nhóm dải đầu biển từ biển kiểm soát.
    /// Ví dụ: "29AH-316.05" => "29AH-316", "30B-532.88" => "30B-532", "30B-207.66" => "30B-207".
    /// </summary>
    private static string ExtractDauBien(string bienKiemSoat, string? dauBienMacDinh)
    {
        if (string.IsNullOrWhiteSpace(bienKiemSoat))
        {
            return !string.IsNullOrWhiteSpace(dauBienMacDinh) ? dauBienMacDinh : "Khác";
        }

        bienKiemSoat = bienKiemSoat.Trim().ToUpperInvariant();

        // Nếu có dấu '.', phần trước dấu '.' chính là dải đầu biển (chuẩn biển số 5 chữ số Việt Nam)
        int dotIndex = bienKiemSoat.IndexOf('.');
        if (dotIndex > 0)
        {
            return bienKiemSoat.Substring(0, dotIndex);
        }

        // Nếu có dấu '-' và sau đó có ít nhất 3 chữ số
        int dashIndex = bienKiemSoat.IndexOf('-');
        if (dashIndex > 0 && dashIndex + 4 <= bienKiemSoat.Length)
        {
            return bienKiemSoat.Substring(0, dashIndex + 4);
        }

        return !string.IsNullOrWhiteSpace(dauBienMacDinh) ? dauBienMacDinh : bienKiemSoat;
    }
}
