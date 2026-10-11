using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SMU.Application.Interfaces;
using SMU.Application.Services;
using SMU.Infrastructure.Ai;
using SMU.Infrastructure.Ai.Configuration;
using SMU.Infrastructure.Data;
using SMU.Infrastructure.Extractors;
using SMU.Infrastructure.Extractors.Abstractions;
using SMU.Infrastructure.Extractors.Implementations;
using SMU.Infrastructure.Identity;
using SMU.Infrastructure.Identity.Configuration;
using SMU.Infrastructure.Storage;
using SMU.Infrastructure.Storage.Configuration;
using SMU.Presentation.Middlewares;

namespace SMU.Extensions;

/// <summary>
/// Các phương thức mở rộng (Extension Methods) đóng gói đăng ký Dependency Injection theo từng Layer.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký các dịch vụ thuộc tầng Application (Orchestrators, Use Cases).
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IDocumentAnalysisOrchestrator, DocumentAnalysisOrchestrator>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICustomerService, CustomerService>();

        // Đăng ký các dịch vụ danh mục hệ thống
        services.AddScoped<IDauBienSoService, DauBienSoService>();
        services.AddScoped<IMauBienSoService, MauBienSoService>();
        services.AddScoped<ITrangThaiBienSoService, TrangThaiBienSoService>();
        services.AddScoped<ILoaiXeService, LoaiXeService>();
        services.AddScoped<ILoaiThuTucService, LoaiThuTucService>();
        services.AddScoped<IKhuVucApDungService, KhuVucApDungService>();
        services.AddScoped<IKyHieuQuyenBienLaiService, KyHieuQuyenBienLaiService>();
        services.AddScoped<ITinhChatDotNhapService, TinhChatDotNhapService>();
        services.AddScoped<IGoiThueBaoService, GoiThueBaoService>();
        services.AddScoped<IMucThuLePhiService, MucThuLePhiService>();
        services.AddScoped<ICapBacService, CapBacService>();
        services.AddScoped<IVaiTroPhanQuyenService, VaiTroPhanQuyenService>();

        // Đăng ký dịch vụ Quản lý nhập kho và vật tư biển số xe
        services.AddScoped<INhapKhoService, NhapKhoService>();

        // Đăng ký dịch vụ Quản lý xuất kho và lập thủ tục biên lai thu tiền
        services.AddScoped<IXuatKhoService, XuatKhoService>();

        // Đăng ký dịch vụ Quản lý tồn kho biển số xe
        services.AddScoped<ITonKhoService, TonKhoService>();

        // Đăng ký dịch vụ Quản lý kiểm kê kho biển số xe
        services.AddScoped<IKiemKeKhoService, KiemKeKhoService>();

        // Đăng ký dịch vụ Lập biên lai cấp lại đăng ký xe (không xuất kho, không giảm tồn biển)
        services.AddScoped<ICapLaiDangKyXeService, CapLaiDangKyXeService>();

        // Đăng ký dịch vụ Báo cáo phân tích biến thiên Nhập - Xuất - Chi phí lệ phí
        services.AddScoped<IBaoCaoBienThienService, BaoCaoBienThienService>();

        // Đăng ký dịch vụ Báo cáo Nhập - Xuất - Tồn Biển số xe
        services.AddScoped<IBaoCaoNhapXuatTonService, BaoCaoNhapXuatTonService>();

        return services;
    }

    /// <summary>
    /// Đăng ký các dịch vụ thuộc tầng Infrastructure (AI Clients, Document Extractors, Strategy Resolvers, File Storage, JWT, Identity).
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Cấu hình kết nối cơ sở dữ liệu PostgreSQL qua Entity Framework Core
        var connectionString = configuration.GetConnectionString("Postgres");
        services.AddDbContext<PostgresDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(PostgresDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        });

        // 2. Cấu hình Options tự động validate
        services.AddOptions<GeminiSettings>()
            .Bind(configuration.GetSection(GeminiSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<FileStorageSettings>()
            .Bind(configuration.GetSection(FileStorageSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 3. Cấu hình HttpContext và CurrentUserService (truy xuất user hiện tại ở bất kỳ function nào)
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // 4. Cấu hình dịch vụ cấp phát token JWT (thời hạn 1 ngày)
        services.AddScoped<IJwtService, JwtService>();

        // 5. Cấu hình HttpClient tích hợp Gemini AI
        services.AddHttpClient<IAiService, GeminiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        // 6. Đăng ký các Strategy bóc tách tài liệu (Open/Closed Principle)
        services.AddScoped<IDocumentExtractorStrategy, ExcelExtractorStrategy>();
        services.AddScoped<IDocumentExtractorStrategy, WordExtractorStrategy>();
        services.AddScoped<IDocumentExtractorStrategy, PdfExtractorStrategy>();
        services.AddScoped<IDocumentExtractorStrategy, PlainTextExtractorStrategy>();

        // 7. Đăng ký Strategy Resolver / Facade
        services.AddScoped<IDocumentExtractionService, DocumentExtractionService>();

        // 8. Đăng ký dịch vụ lưu trữ file
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }

    /// <summary>
    /// Đăng ký các dịch vụ thuộc tầng Presentation (Global Exception Handler, JWT Authentication, Authorization, CORS, Upload Limits, Swagger).
    /// </summary>
    public static IServiceCollection AddPresentationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Xử lý lỗi toàn cục chuẩn RFC 7807 trong .NET 8
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // 2. Cấu hình xác thực JWT Authentication và phân quyền Authorization
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        var keyBytes = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // Hết hạn chính xác sau 1 ngày
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
            };
        });

        services.AddAuthorization();

        // 3. Giới hạn upload file 50MB
        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 50 * 1024 * 1024;
        });

        services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
        });

        // 4. Chính sách CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // 5. Swagger / OpenAPI Documentation kèm nút Authorize JWT
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SMU - Document AI API (Clean Architecture & JWT Auth)",
                Version = "v1",
                Description = "Hệ thống bóc tách tài liệu đa định dạng (Excel, Word, PDF, Text) và tích hợp AI thông minh với bảo mật JWT Authentication và Authorization."
            });

            // Tích hợp Bearer Token vào Swagger UI
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Nhập Token theo cú pháp: Bearer {token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }
}
