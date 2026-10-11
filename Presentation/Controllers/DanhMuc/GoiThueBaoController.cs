using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// API Quản lý Danh mục Gói thuê bao (gồm đúng 5 API: GetList, Set, DeleteMultiple, ImportExcel, ExportExcel).
/// Tên bảng cơ sở dữ liệu: GoiThueBao
/// Tuyến đường API: /api/goithuebao
/// </summary>
[Route("api/goithuebao")]
[Tags("Danh mục - Gói thuê bao")]
public class GoiThueBaoController : BaseDanhMucController<IGoiThueBaoService, GoiThueBaoResponse, SetGoiThueBaoRequest>
{
    public GoiThueBaoController(IGoiThueBaoService service) : base(service)
    {
    }

    protected override string GetExportFileName() => "goi_thue_bao";
}
