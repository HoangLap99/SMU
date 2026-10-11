using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// API Quản lý Danh mục Khu vực áp dụng (gồm đúng 5 API: GetList, Set, DeleteMultiple, ImportExcel, ExportExcel).
/// Tên bảng cơ sở dữ liệu: KhuVucApDung
/// Tuyến đường API: /api/khuvucapdung
/// </summary>
[Route("api/khuvucapdung")]
[Tags("Danh mục - Khu vực áp dụng")]
public class KhuVucApDungController : BaseDanhMucController<IKhuVucApDungService, KhuVucApDungResponse, SetKhuVucApDungRequest>
{
    public KhuVucApDungController(IKhuVucApDungService service) : base(service)
    {
    }

    protected override string GetExportFileName() => "khu_vuc_ap_dung";
}
