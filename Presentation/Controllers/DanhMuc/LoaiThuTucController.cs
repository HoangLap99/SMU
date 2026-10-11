using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// API Quản lý Danh mục Loại thủ tục (gồm đúng 5 API: GetList, Set, DeleteMultiple, ImportExcel, ExportExcel).
/// Tên bảng cơ sở dữ liệu: LoaiThuTuc
/// Tuyến đường API: /api/loaithutuc
/// </summary>
[Route("api/loaithutuc")]
[Tags("Danh mục - Loại thủ tục")]
public class LoaiThuTucController : BaseDanhMucController<ILoaiThuTucService, LoaiThuTucResponse, SetLoaiThuTucRequest>
{
    public LoaiThuTucController(ILoaiThuTucService service) : base(service)
    {
    }

    protected override string GetExportFileName() => "loai_thu_tuc";
}
