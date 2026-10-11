using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// API Quản lý Danh mục Ký hiệu quyển biên lai (gồm đúng 5 API: GetList, Set, DeleteMultiple, ImportExcel, ExportExcel).
/// Tên bảng cơ sở dữ liệu: KyHieuQuyenBienLai
/// Tuyến đường API: /api/kyhieuquyenbienlai
/// </summary>
[Route("api/kyhieuquyenbienlai")]
[Tags("Danh mục - Ký hiệu quyển biên lai")]
public class KyHieuQuyenBienLaiController : BaseDanhMucController<IKyHieuQuyenBienLaiService, KyHieuQuyenBienLaiResponse, SetKyHieuQuyenBienLaiRequest>
{
    public KyHieuQuyenBienLaiController(IKyHieuQuyenBienLaiService service) : base(service)
    {
    }

    protected override string GetExportFileName() => "ky_hieu_quyen_bien_lai";
}
