using SMU.Extensions;
using SMU.Infrastructure.Data;

// Cho phép xử lý linh hoạt timestamp DateTime trong PostgreSQL Npgsql
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các dịch vụ theo từng Layer (Dependency Injection)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddPresentationServices(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();

// 2. Kích hoạt Middleware xử lý lỗi toàn cục (.NET 8 Exception Handling)
app.UseExceptionHandler();

// 3. Cấu hình HTTP Request Pipeline & Swagger (Hỗ trợ cả Development và Production/Server)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Đường dẫn tương đối giúp tương thích cả IIS Virtual Application (/SMU/swagger) và Root (/swagger)
    c.SwaggerEndpoint("./v1/swagger.json", "SMU v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "SMU - Document AI API (Clean Architecture & JWT Auth)";
});

app.UseCors("AllowAll");
// Không dùng UseHttpsRedirection khi server chạy trên cổng HTTP (như http://103.130.212.35:2020)

// Phục vụ các file tĩnh trong wwwroot (View nhập kho, CSS, JS)
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Tự động chuyển hướng từ trang chủ về Swagger UI
app.MapGet("/", () => Results.Redirect("swagger/index.html"));

app.MapControllers();

// Đảm bảo cấu trúc các cột trong cơ sở dữ liệu (không tự động thêm dữ liệu)
await DbInitializer.SeedAsync(app.Services);

app.Run();
