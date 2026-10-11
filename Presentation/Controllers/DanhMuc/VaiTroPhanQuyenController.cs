using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// API Quản lý Danh mục Vai trò phân quyền (gồm 6 API: GetAll không phân trang, GetList phân trang, Set, DeleteMultiple, ImportExcel, ExportExcel).
/// Tên bảng cơ sở dữ liệu: VaiTroPhanQuyen
/// Tuyến đường API: /api/vaitrophanquyen
/// </summary>
[Route("api/vaitrophanquyen")]
[Tags("Danh mục - Vai trò phân quyền")]
public class VaiTroPhanQuyenController : BaseDanhMucController<IVaiTroPhanQuyenService, VaiTroPhanQuyenResponse, SetVaiTroPhanQuyenRequest>
{
    public VaiTroPhanQuyenController(IVaiTroPhanQuyenService service) : base(service)
    {
    }

    protected override string GetExportFileName() => "vai_tro_phan_quyen";
}
