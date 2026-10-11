using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// API Quản lý Danh mục Cấp bậc Công an nhân dân (gồm 6 API: GetAll không phân trang, GetList phân trang, Set, DeleteMultiple, ImportExcel, ExportExcel).
/// Tên bảng cơ sở dữ liệu: CapBac
/// Tuyến đường API: /api/capbac
/// </summary>
[Route("api/capbac")]
[Tags("Danh mục - Cấp bậc")]
public class CapBacController : BaseDanhMucController<ICapBacService, CapBacResponse, SetCapBacRequest>
{
    public CapBacController(ICapBacService service) : base(service)
    {
    }

    protected override string GetExportFileName() => "cap_bac";
}
