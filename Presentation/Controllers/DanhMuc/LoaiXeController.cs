using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// API Quản lý Danh mục Loại xe (gồm đúng 5 API: GetList, Set, DeleteMultiple, ImportExcel, ExportExcel).
/// Tên bảng cơ sở dữ liệu: LoaiXe
/// Tuyến đường API: /api/loaixe
/// </summary>
[Route("api/loaixe")]
[Tags("Danh mục - Loại xe")]
public class LoaiXeController : BaseDanhMucController<ILoaiXeService, LoaiXeResponse, SetLoaiXeRequest>
{
    public LoaiXeController(ILoaiXeService service) : base(service)
    {
    }

    protected override string GetExportFileName() => "loai_xe";
}
