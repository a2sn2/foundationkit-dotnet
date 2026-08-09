using System.Threading.RateLimiting;
using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using FoundationKit.Infrastructure;
using FoundationKit.Infrastructure.Events;
using FoundationKit.Infrastructure.Persistence;
using FoundationKit.Security;
using FoundationKit.WebApi;
using Madar.Api;
using Madar.Api.Security;
using Madar.Application.Cases;
using Madar.Application.Organization;
using Madar.Application.Security;
using Madar.Contracts.Security;
using Madar.Domain.Cases;
using Madar.Infrastructure;
using Madar.Infrastructure.Auditing;
using Madar.Infrastructure.Cases;
using Madar.Infrastructure.Identity;
using Madar.Infrastructure.Organization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFoundationInfrastructure();
builder.Services.AddFoundationWebApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMadarNotifications(builder.Configuration);

builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<ICurrentUser>(serviceProvider =>
    serviceProvider.GetRequiredService<CurrentUserAccessor>());
builder.Services.AddScoped<IAuthorizationSubject>(serviceProvider =>
    serviceProvider.GetRequiredService<CurrentUserAccessor>());
builder.Services.AddSingleton(MadarPermissions.CreateRolePermissionMap());
builder.Services.AddScoped<IAuthorizationEvaluator, RolePermissionAuthorizationEvaluator>();

builder.Services.AddScoped<ICaseNotificationCoordinator, CaseNotificationCoordinator>();
builder.Services.AddScoped<ICaseManager, CaseManager>();
builder.Services.AddScoped<ICaseRoutingManager, CaseRoutingManager>();
builder.Services.AddScoped<ICaseSlaManager, CaseSlaManager>();
builder.Services.AddScoped<ICaseCommentManager, CaseCommentManager>();
builder.Services.AddScoped<ICaseAttachmentManager, CaseAttachmentManager>();
builder.Services.AddScoped<CaseApprovalManager>();
builder.Services.AddScoped<ICaseApprovalManager, NotifyingCaseApprovalManager>();
builder.Services.AddScoped<CaseQueryService>();
builder.Services.AddScoped<ICaseQueryService>(serviceProvider =>
    serviceProvider.GetRequiredService<CaseQueryService>());
builder.Services.AddScoped<ICaseSlaQueryService>(serviceProvider =>
    serviceProvider.GetRequiredService<CaseQueryService>());
builder.Services.AddScoped<CaseCommentStore>();
builder.Services.AddScoped<ICaseCommentStore>(serviceProvider =>
    serviceProvider.GetRequiredService<CaseCommentStore>());
builder.Services.AddScoped<ICaseCommentQueryService>(serviceProvider =>
    serviceProvider.GetRequiredService<CaseCommentStore>());
builder.Services.AddScoped<CaseAttachmentStore>();
builder.Services.AddScoped<ICaseAttachmentStore>(serviceProvider =>
    serviceProvider.GetRequiredService<CaseAttachmentStore>());
builder.Services.AddScoped<ICaseAttachmentQueryService>(serviceProvider =>
    serviceProvider.GetRequiredService<CaseAttachmentStore>());
builder.Services.AddScoped<CaseApprovalStore>();
builder.Services.AddScoped<ICaseApprovalRepository>(serviceProvider =>
    serviceProvider.GetRequiredService<CaseApprovalStore>());
builder.Services.AddScoped<ICaseApprovalQueryService>(serviceProvider =>
    serviceProvider.GetRequiredService<CaseApprovalStore>());
builder.Services.AddSingleton<ICaseSlaPolicy, ConfiguredCaseSlaPolicy>();
builder.Services.AddScoped<ICaseTimelineService, CaseTimelineService>();
builder.Services.AddScoped<ICaseTimelineQueryService, CaseTimelineQueryService>();
builder.Services.AddScoped<IUserDirectory, UserDirectory>();
builder.Services.AddScoped<IDepartmentDirectory, DepartmentDirectory>();
builder.Services.AddScoped<IDepartmentAdministrationManager, DepartmentAdministrationManager>();
builder.Services.AddScoped<IDepartmentAdministrationStore, DepartmentAdministrationStore>();
builder.Services.AddScoped<IRepository<Case, Guid>, EfRepository<Case, Guid, MadarDbContext>>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork<MadarDbContext>>();
builder.Services.AddScoped<IMadarReadinessProbe, MadarReadinessProbe>();
builder.Services.AddSingleton<IClock, SystemClock>();

var attachmentStorageRoot = builder.Configuration["Madar:Attachments:StorageRoot"];
if (string.IsNullOrWhiteSpace(attachmentStorageRoot))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Madar:Attachments:StorageRoot must be explicitly configured outside Development.");
    }

    attachmentStorageRoot = Path.Combine(
        builder.Environment.ContentRootPath,
        "data",
        "attachments");
}

builder.Services.AddSingleton<ICaseAttachmentContentStore>(
    new FileSystemCaseAttachmentContentStore(attachmentStorageRoot));
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit =
        CaseAttachmentPolicy.MaxSizeBytes + (1024 * 1024);
});

builder.Services.AddScoped<IAuditSink, SqlAuditSink>();
builder.Services.AddScoped<IAuditContextAccessor, MadarAuditContextAccessor>();
builder.Services.AddScoped<IAuditRecorder, AuditRecorder>();

var connectionString = builder.Configuration.GetConnectionString("Madar")
    ?? throw new InvalidOperationException(
        "Connection string 'Madar' is required.");

builder.Services.AddDbContext<MadarDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(
        connectionString,
        sqlServer => sqlServer.MigrationsAssembly(
            typeof(MadarDbContext).Assembly.FullName));
    options.AddInterceptors(
        serviceProvider.GetRequiredService<DomainEventsSaveChangesInterceptor>());
});

builder.Services
    .AddIdentity<MadarUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<MadarDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Madar.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "Madar.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        FoundationRateLimitPartitions.Authentication(context),
        static _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("write", context => RateLimitPartition.GetFixedWindowLimiter(
        FoundationRateLimitPartitions.Write(context),
        static _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services.AddOptions<MadarBootstrapOptions>()
    .Bind(builder.Configuration.GetSection(MadarBootstrapOptions.SectionName))
    .Validate(options =>
        !options.Enabled
        || (IsBootstrapValue(options.AdministratorEmail)
            && IsBootstrapPassword(options.AdministratorPassword)
            && IsBootstrapValue(options.AdministratorDisplayName)
            && IsBootstrapValue(options.OperatorEmail)
            && IsBootstrapPassword(options.OperatorPassword)
            && IsBootstrapValue(options.OperatorDisplayName)),
        "Enabled Madar bootstrap requires administrator/operator email, display name, and strong password values.")
    .ValidateOnStart();

builder.Services.AddOptions<MadarDatabaseStartupOptions>()
    .Bind(builder.Configuration.GetSection(MadarDatabaseStartupOptions.SectionName))
    .Validate(
        options => options.MigrationAttempts is >= 1 and <= 300,
        "Madar:DatabaseStartup:MigrationAttempts must be between 1 and 300.")
    .Validate(
        options => options.DelaySeconds is >= 0 and <= 30,
        "Madar:DatabaseStartup:DelaySeconds must be between 0 and 30.")
    .ValidateOnStart();

builder.Services.AddOptions<MadarSlaOptions>()
    .Bind(builder.Configuration.GetSection(MadarSlaOptions.SectionName))
    .Validate(
        options => !options.Enabled || options.Durations.All(IsValidSlaDuration),
        "Enabled Madar SLA requires positive bounded durations for low, medium, high, and critical priorities.")
    .ValidateOnStart();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Madar API",
        Version = "v1",
        Description = "Operational case-management API built on FoundationKit."
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();
app.UseMiddleware<DatabaseExceptionMiddleware>();
app.UseFoundationRequestPipeline();
app.UseRateLimiter();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Madar API v1");
        options.DocumentTitle = "Madar — Swagger";
    });
}

await DatabaseInitializer.InitializeAsync(
    app.Services,
    app.Lifetime.ApplicationStopping);

app.MapMadarEndpoints();
app.MapMadarDepartmentEndpoints();
app.MapMadarDepartmentAdministrationEndpoints();
app.MapMadarCaseCommentEndpoints();
app.MapMadarCaseAttachmentEndpoints();
app.MapMadarCaseApprovalEndpoints();
app.MapFallbackToFile("index.html");

app.Run();

static bool IsBootstrapValue(string? value) =>
    !string.IsNullOrWhiteSpace(value);

static bool IsBootstrapPassword(string? value) =>
    !string.IsNullOrWhiteSpace(value) && value.Length >= 12;

static bool IsValidSlaDuration(TimeSpan? value) =>
    value.HasValue
    && value.Value > TimeSpan.Zero
    && value.Value <= TimeSpan.FromDays(365);

public partial class Program;
