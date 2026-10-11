using Microsoft.AspNetCore.Mvc;
using SMU.Application.DTOs;
using SMU.Application.Interfaces;

namespace SMU.Presentation.Controllers;

/// <summary>
/// API Quản lý Danh mục Tính chất đợt nhập (gồm đúng 5 API: GetList, Set, DeleteMultiple, ImportExcel, ExportExcel).
/// Tên bảng cơ sở dữ liệu: TinhChatDotNhap
/// Tuyến đường API: /api/tinhchatdotnhap
/// </summary>
[Route("api/tinhchatdotnhap")]
[Tags("Danh mục - Tính chất đợt nhập")]
public class TinhChatDotNhapController : BaseDanhMucController<ITinhChatDotNhapService, TinhChatDotNhapResponse, SetTinhChatDotNhapRequest>
{
    public TinhChatDotNhapController(ITinhChatDotNhapService service) : base(service)
    {
    }

    protected override string GetExportFileName() => "tinh_chat_dot_nhap";
}
