using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// API Quản lý Danh mục Trạng thái biển số xe (gồm đúng 5 API: GetList, Set, DeleteMultiple, ImportExcel, ExportExcel).
/// Tên bảng cơ sở dữ liệu: TrangThaiBienSo
/// Tuyến đường API: /api/trangthaibienso
/// </summary>
[Route("api/trangthaibienso")]
[Tags("Danh mục - Trạng thái biển số")]
public class TrangThaiBienSoController : BaseDanhMucController<ITrangThaiBienSoService, TrangThaiBienSoResponse, SetTrangThaiBienSoRequest>
{
    public TrangThaiBienSoController(ITrangThaiBienSoService service) : base(service)
    {
    }

    protected override string GetExportFileName() => "trang_thai_bien_so";
}
