using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// API Quản lý Danh mục Màu biển số xe (gồm đúng 5 API: GetList, Set, DeleteMultiple, ImportExcel, ExportExcel).
/// Tên bảng cơ sở dữ liệu: MauBienSo
/// Tuyến đường API: /api/maubienso
/// </summary>
[Route("api/maubienso")]
[Tags("Danh mục - Màu biển số")]
public class MauBienSoController : BaseDanhMucController<IMauBienSoService, MauBienSoResponse, SetMauBienSoRequest>
{
    public MauBienSoController(IMauBienSoService service) : base(service)
    {
    }

    protected override string GetExportFileName() => "mau_bien_so";
}
