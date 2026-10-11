using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// API Quản lý Danh mục Mức thu lệ phí (Quy định theo Thông tư 155/2025/TT-BTC).
/// Tên bảng cơ sở dữ liệu: MucThuLePhi
/// Tuyến đường API: /api/mucthulephi
/// </summary>
[Route("api/mucthulephi")]
[Tags("Danh mục - Mức thu lệ phí")]
public class MucThuLePhiController : BaseDanhMucController<IMucThuLePhiService, MucThuLePhiResponse, SetMucThuLePhiRequest>
{
    public MucThuLePhiController(IMucThuLePhiService service) : base(service)
    {
    }

    protected override string GetExportFileName() => "muc_thu_le_phi";
}
