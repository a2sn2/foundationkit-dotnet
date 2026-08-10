using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;
using Athar.Api;
using Athar.Application;
using Athar.Contracts;
using Athar.Domain;
using Athar.Infrastructure;
using FoundationKit.Application.Persistence;
using FoundationKit.Authorization;
using FoundationKit.Identity;
using FoundationKit.Infrastructure;
using FoundationKit.Infrastructure.Events;
using FoundationKit.Infrastructure.Persistence;
using FoundationKit.Notifications;
using FoundationKit.Notifications.Smtp;
using FoundationKit.Security;
using FoundationKit.WebApi;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

ProductionConfigurationValidator.Validate(
    builder.Configuration,
    builder.Environment.IsDevelopment());

var accountSecurity = builder.Configuration
    .GetSection(AccountSecurityOptions.SectionName)
    .Get<AccountSecurityOptions>()
    ?? new AccountSecurityOptions();
AccountSecurityOptionsValidator.Validate(accountSecurity);

var reverseProxySecurity = builder.Configuration
    .GetSection(TrustedProxyOptions.SectionName)
    .Get<TrustedProxyOptions>()
    ?? new TrustedProxyOptions();

builder.Services.AddFoundationInfrastructure();
builder.Services.AddFoundationWebApi();
builder.Services.AddFoundationTrustedProxyForwarding(reverseProxySecurity);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Athar.Application.ICurrentUser, CurrentUserAccessor>();
builder.Services.AddScoped<IAuthorizationSubject>(serviceProvider =>
    serviceProvider.GetRequiredService<Athar.Application.ICurrentUser>());
builder.Services.AddSingleton(AtharPermissions.CreateRolePermissionMap());
builder.Services.AddScoped<IAuthorizationEvaluator, RolePermissionAuthorizationEvaluator>();
builder.Services.AddScoped<IInitiativeManager, InitiativeManager>();
builder.Services.AddScoped<IInitiativeQueryService, InitiativeQueryService>();
builder.Services.AddScoped<IAuditWriter, AuditWriter>();
builder.Services.AddScoped<ISmtpNotificationObserver, AtharSmtpNotificationObserver>();
builder.Services.AddScoped<INotificationSender>(serviceProvider =>
{
    var deliveryOptions = serviceProvider
        .GetRequiredService<IOptions<AccountSecurityDeliveryOptions>>()
        .Value;
    return new SmtpNotificationSender(
        deliveryOptions.ToProviderOptions(),
        serviceProvider.GetRequiredService<ISmtpNotificationObserver>());
});
builder.Services.AddScoped<IAccountNotificationSender, AccountSecurityNotificationAdapter>();
builder.Services.AddScoped<IRepository<Initiative, Guid>, EfRepository<Initiative, Guid, AtharDbContext>>();
builder.Services.AddScoped<IRepository<InitiativeReview, Guid>, EfRepository<InitiativeReview, Guid, AtharDbContext>>();
builder.Services.AddScoped<FoundationKit.Application.Abstractions.IUnitOfWork, EfUnitOfWork<AtharDbContext>>();
builder.Services.AddSingleton<FoundationKit.Application.Abstractions.IClock, SystemClock>();

ConfigureDataProtection(builder);

var connectionString = builder.Configuration.GetConnectionString("Athar")
    ?? throw new InvalidOperationException("Connection string 'Athar' is required.");

builder.Services.AddDbContext<AtharDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString, sqlServer => sqlServer.MigrationsAssembly(typeof(AtharDbContext).Assembly.FullName));
    options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventsSaveChangesInterceptor>());
});

builder.Services
    .AddIdentity<AtharUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = accountSecurity.RequireConfirmedEmail;
        options.Password.RequiredLength = accountSecurity.PasswordRequiredLength;
        options.Password.RequireDigit = accountSecurity.PasswordRequireDigit;
        options.Password.RequireLowercase = accountSecurity.PasswordRequireLowercase;
        options.Password.RequireUppercase = accountSecurity.PasswordRequireUppercase;
        options.Password.RequireNonAlphanumeric = accountSecurity.PasswordRequireNonAlphanumeric;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AtharDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Athar.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AtharUser", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("AtharAdministrator", policy =>
    {
        policy.RequireRole(AtharRoles.Administrator);
        if (accountSecurity.RequireAdministratorMfa)
            policy.RequireFoundationMultiFactor();
    });
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "Athar.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: FoundationRateLimitPartitions.Authentication(context),
        factory: static _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("write", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: FoundationRateLimitPartitions.Write(context),
        factory: static _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services.AddOptions<DatabaseStartupOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseStartupOptions.SectionName))
    .Validate(options => options.MigrationAttempts is >= 1 and <= 300 && options.DelaySeconds is >= 1 and <= 30,
        "DatabaseStartup values are outside the supported range.")
    .ValidateOnStart();

builder.Services.AddOptions<AdminSeedOptions>()
    .Bind(builder.Configuration.GetSection(AdminSeedOptions.SectionName))
    .Validate(options => !options.Enabled || (!string.IsNullOrWhiteSpace(options.Email)
        && !string.IsNullOrWhiteSpace(options.Password) && options.Password.Length >= 12),
        "When AdminSeed is enabled, Email and a password of at least 12 characters are required.")
    .ValidateOnStart();

builder.Services.AddOptions<AccountSecurityOptions>()
    .Bind(builder.Configuration.GetSection(AccountSecurityOptions.SectionName))
    .Validate(options => options.PasswordRequiredLength is >= 1 and <= 128,
        "AccountSecurity:PasswordRequiredLength must be between 1 and 128.")
    .ValidateOnStart();

builder.Services.AddOptions<AccountSecurityDeliveryOptions>()
    .Bind(builder.Configuration.GetSection(AccountSecurityDeliveryOptions.SectionName))
    .Validate(options => options.SmtpPort is >= 1 and <= 65535,
        "AccountSecurity:SmtpPort must be a valid TCP port.")
    .ValidateOnStart();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "منصة أثر API",
        Version = "v1",
        Description = "مرجع إنتاجي عربي مبني على FoundationKit لإدارة المبادرات المجتمعية."
    });
});

var app = builder.Build();

app.UseFoundationTrustedProxyForwarding(reverseProxySecurity);

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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "منصة أثر API v1");
        options.DocumentTitle = "منصة أثر — Swagger";
    });
}

await DatabaseInitializer.InitializeAsync(app.Services, app.Lifetime.ApplicationStopping);

app.MapAtharEndpoints();
app.MapFallbackToFile("index.html");

app.Run();

static void ConfigureDataProtection(WebApplicationBuilder builder)
{
    var keysPath = builder.Configuration["DataProtection:KeysPath"];
    var certificatePath = builder.Configuration["DataProtection:CertificatePath"];
    var certificatePassword = builder.Configuration["DataProtection:CertificatePassword"];

    var dataProtection = builder.Services
        .AddDataProtection()
        .SetApplicationName("Athar");

    if (!string.IsNullOrWhiteSpace(keysPath))
    {
        var directory = new DirectoryInfo(keysPath);
        directory.Create();
        dataProtection.PersistKeysToFileSystem(directory);
    }

    if (!string.IsNullOrWhiteSpace(certificatePath))
    {
        var certificate = X509CertificateLoader.LoadPkcs12FromFile(
            certificatePath,
            certificatePassword,
            X509KeyStorageFlags.EphemeralKeySet);
        dataProtection.ProtectKeysWithCertificate(certificate);
    }
}

public partial class Program
{
}
