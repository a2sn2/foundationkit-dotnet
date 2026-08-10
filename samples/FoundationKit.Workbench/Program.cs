using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Crud;
using FoundationKit.Application.Events;
using FoundationKit.Application.Modules;
using FoundationKit.Application.Persistence;
using FoundationKit.Auditing;
using FoundationKit.Caching;
using FoundationKit.FeatureManagement;
using FoundationKit.Infrastructure;
using FoundationKit.Infrastructure.Events;
using FoundationKit.Infrastructure.Persistence;
using FoundationKit.Infrastructure.Platform;
using FoundationKit.Localization;
using FoundationKit.Settings;
using FoundationKit.WebApi;
using FoundationKit.WebApi.Api;
using FoundationKit.WebApi.Crud;
using FoundationKit.Workbench;
using FoundationKit.Workbench.Application;
using FoundationKit.Workbench.Application.Admin;
using FoundationKit.Workbench.Application.CoreCrud;
using FoundationKit.Workbench.Application.Shared;
using FoundationKit.Workbench.Application.User;
using FoundationKit.Workbench.Contracts;
using FoundationKit.Workbench.Domain;
using FoundationKit.Workbench.Endpoints;
using FoundationKit.Workbench.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFoundationInfrastructure();
builder.Services.AddFoundationWebApi();
builder.Services.AddFoundationProject("foundationkit-workbench");
builder.Services.AddSingleton<ISettingSource>(_ => new InMemorySettingSource(
[
    new SettingEntry(SettingScope.Global, WorkbenchPlatformReference.DefaultCultureSetting, "ar-YE"),
    new SettingEntry(SettingScope.Global, WorkbenchPlatformReference.DefaultTimeZoneSetting, "UTC"),
    new SettingEntry(
        SettingScope.Global,
        SettingBackedFeatureEvaluator.GetEnabledSettingKey(WorkbenchPlatformReference.CatalogPreviewFeature),
        "true")
]));
builder.Services.AddSingleton<ISettingReader, SettingReader>();
builder.Services.AddSingleton<IFeatureEvaluator, SettingBackedFeatureEvaluator>();
builder.Services.AddSingleton(_ => new SupportedCultureSet(["ar-YE", "en-US"], "ar-YE"));
builder.Services.AddSingleton<ICacheStore>(_ => new InMemoryCacheStore(
    new InMemoryCacheOptions
    {
        MaximumEntries = 128,
        MaximumValueBytes = 1_048_576,
        MaximumTimeToLive = TimeSpan.FromHours(1)
    }));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FoundationKit Workbench API",
        Version = "v1",
        Description = "Executable Core architecture reference including the FoundationKit API Engine and generic CRUD vertical slice."
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalWorkbenchClient", policy =>
    {
        policy
            .SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("Workbench")
    ?? throw new InvalidOperationException("Connection string 'Workbench' is required. See docs/WORKBENCH.md.");

builder.Services.AddDbContext<WorkbenchDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString, sqlServer =>
        sqlServer.MigrationsAssembly(typeof(WorkbenchDbContext).Assembly.FullName));
    options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventsSaveChangesInterceptor>());
});

builder.Services.AddScoped<IRepository<BuildBrief, Guid>, EfRepository<BuildBrief, Guid, WorkbenchDbContext>>();
builder.Services.AddScoped<IRepository<AdminReview, Guid>, EfRepository<AdminReview, Guid, WorkbenchDbContext>>();
builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddScoped<ICrudAuthorizationPolicy<CoreCrudRecord, Guid>, CoreCrudAuthorizationPolicy>();
builder.Services.AddScoped<ICrudConcurrencyPolicy<CoreCrudRecord, CoreCrudUpdateRequest>, CoreCrudConcurrencyPolicy>();
builder.Services.AddScoped<ICrudQueryPolicy<CoreCrudRecord, Guid>, CoreCrudQueryPolicy>();
builder.Services.AddSingleton<IFoundationApiEntityTagProvider<CoreCrudResponse>, CoreCrudEntityTagProvider>();
builder.Services.AddFoundationEfCrudModule<
    CoreCrudRecord,
    Guid,
    CoreCrudCreateRequest,
    CoreCrudUpdateRequest,
    CoreCrudResponse,
    CoreCrudMapper,
    WorkbenchDbContext>(module => module
        .Named("CoreCrud", "core-crud")
        .Crud()
        .Api(api =>
        {
            api.Idempotency = FoundationApiIdempotencyMode.Required;
            api.Concurrency = FoundationApiConcurrencyMode.RequireIfMatch;
            api.MaximumFilters = 1;
            api.MaximumSorts = 1;
        })
        .Auditing()
        .Authorization()
        .Concurrency()
        .FeatureManagement()
        .Localization()
        .Caching()
        .UseManager<CoreCrudManager>());

builder.Services.AddSingleton<WorkbenchAuditSink>();
builder.Services.AddSingleton<IAuditSink>(services => services.GetRequiredService<WorkbenchAuditSink>());
builder.Services.AddSingleton<IAuditContextAccessor, WorkbenchAuditContextAccessor>();
builder.Services.AddScoped<IAuditRecorder, AuditRecorder>();
builder.Services.AddScoped<ICrudOperationObserver<CoreCrudRecord, Guid>, CrudAuditObserver<CoreCrudRecord, Guid>>();

builder.Services.AddSingleton<CatalogService>();
builder.Services.AddSingleton<ICapabilityCatalog>(serviceProvider => serviceProvider.GetRequiredService<CatalogService>());
builder.Services.AddScoped<CreateUserRequestUseCase>();
builder.Services.AddScoped<ReviewUserRequestUseCase>();
builder.Services.AddScoped<IAdminQueueReader, EfAdminQueueReader>();
builder.Services.AddScoped<IDomainEventHandler<BuildBriefCreated>, BuildBriefCreatedHandler>();

var app = builder.Build();

app.UseFoundationRequestPipeline();
app.UseCors("LocalWorkbenchClient");
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "FoundationKit Workbench API v1");
    options.DocumentTitle = "FoundationKit Workbench API";
});
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

await DatabaseBootstrapper.MigrateAsync(app.Services, app.Logger, app.Lifetime.ApplicationStopping);

app.MapSystemEndpoints();
app.MapUserPortalEndpoints();
app.MapAdminPortalEndpoints();
var coreCrudModule = app.Services.GetRequiredService<FoundationModuleDefinition<CoreCrudRecord, Guid>>();
app.MapFoundationCrud<CoreCrudRecord, Guid, CoreCrudCreateRequest, CoreCrudUpdateRequest, CoreCrudResponse>(coreCrudModule);

app.MapFallbackToFile("index.html");
app.Run();

public partial class Program
{
}
