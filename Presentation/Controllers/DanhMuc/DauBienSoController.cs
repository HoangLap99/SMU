using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// API Quản lý Danh mục Đầu biển số xe (gồm đúng 5 API: GetList, Set, DeleteMultiple, ImportExcel, ExportExcel).
/// Tên bảng cơ sở dữ liệu: DauBienSo
/// Tuyến đường API: /api/daubienso
/// </summary>
[Route("api/daubienso")]
[Tags("Danh mục - Đầu biển số")]
public class DauBienSoController : BaseDanhMucController<IDauBienSoService, DauBienSoResponse, SetDauBienSoRequest>
{
    public DauBienSoController(IDauBienSoService service) : base(service)
    {
    }

    protected override string GetExportFileName() => "dau_bien_so";
}
