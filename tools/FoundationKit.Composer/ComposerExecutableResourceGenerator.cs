using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FoundationKit.Composer;

internal static class ComposerExecutableResourceGenerator
{
    private const string InitialMigrationId = "20260811000000_InitialGenerated";

    public static IReadOnlyDictionary<string, string> BuildFiles(
        ComposerManifest manifest,
        string projectPrefix)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPrefix);
        var model = manifest.ProjectModel
            ?? throw new ComposerGenerationException("Executable resource generation requires a schema-v2 project model.");
        var executable = model.Modules
            .SelectMany(module => module.Resources.Select(resource => (Module: module, Resource: resource)))
            .Where(item => item.Resource.IsExecutable)
            .ToArray();
        if (executable.Length == 0)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var usesAudit = executable.Any(item => Has(item.Resource, ComposerResourceBehavior.Auditing));
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in executable)
        {
            var folder = $"GeneratedModules/{item.Module.Name}";
            files[$"src/{projectPrefix}.Domain/{folder}/{item.Resource.Name}.cs"] =
                BuildDomainEntity(projectPrefix, item.Module, item.Resource);
            files[$"src/{projectPrefix}.Application/{folder}/{item.Resource.Name}Contracts.cs"] =
                BuildContracts(projectPrefix, item.Module, item.Resource);
            files[$"src/{projectPrefix}.Application/{folder}/{item.Resource.Name}Application.cs"] =
                BuildApplication(projectPrefix, item.Module, item.Resource);
            files[$"src/{projectPrefix}.Infrastructure/{folder}/{item.Resource.Name}EntityConfiguration.cs"] =
                BuildEntityConfiguration(manifest, projectPrefix, item.Module, item.Resource);
        }

        if (usesAudit)
        {
            files[$"src/{projectPrefix}.Application/GeneratedPlatform/GeneratedAuditSupport.cs"] =
                BuildAuditSupport(projectPrefix);
        }
        files[$"src/{projectPrefix}.Infrastructure/GeneratedPlatform/GeneratedDbContext.cs"] =
            BuildDbContext(manifest, projectPrefix, executable);
        files[$"src/{projectPrefix}.Infrastructure/GeneratedPlatform/Migrations/{InitialMigrationId}.cs"] =
            BuildMigration(manifest, projectPrefix, executable);
        files[$"src/{projectPrefix}.Api/GeneratedPlatform/GeneratedHttpIdentity.cs"] =
            BuildHttpIdentity(projectPrefix);
        files[$"src/{projectPrefix}.Api/GeneratedPlatform/GeneratedApiSupport.cs"] =
            BuildApiSupport(projectPrefix, executable);
        files[$"src/{projectPrefix}.Api/Program.cs"] =
            BuildProgram(manifest, projectPrefix, executable);
        files["GENERATED-FULLSTACK.md"] = BuildFullStackReport(manifest, executable);
        return files;
    }

    private static string BuildDomainEntity(
        string projectPrefix,
        ComposerModuleDefinition module,
        ComposerResourceDefinition resource)
    {
        var requiredInitializers = resource.Fields
            .Where(field => field.Required)
            .Select(field => $"        {field.Name} = string.Empty;")
            .ToArray();
        var constructorParameters = string.Join(", ", resource.Fields.Select(field =>
            $"{FieldClrType(field)} {Camel(field.Name)}"));
        var assignments = string.Join("\n", resource.Fields.Select(field =>
            $"        {field.Name} = {Camel(field.Name)};"));
        var properties = string.Join("\n", resource.Fields.Select(field =>
            $"    public {FieldClrType(field)} {field.Name} {{ get; private set; }}"));
        var concurrencyProperty = Has(resource, ComposerResourceBehavior.Concurrency)
            ? "\n    public int Version { get; private set; } = 1;\n"
            : string.Empty;
        var versionIncrement = Has(resource, ComposerResourceBehavior.Concurrency)
            ? "\n        Version = checked(Version + 1);"
            : string.Empty;

        return $$"""
            #nullable enable

            using FoundationKit.Domain.Primitives;

            namespace {{projectPrefix}}.Domain.GeneratedModules.{{module.Name}};

            public sealed class {{resource.Name}} : Entity<Guid>
            {
                private {{resource.Name}}()
                {
            {{string.Join("\n", requiredInitializers)}}
                }

                private {{resource.Name}}(Guid id, {{constructorParameters}}) : base(id)
                {
            {{assignments}}
                }

            {{properties}}
            {{concurrencyProperty}}
                public static {{resource.Name}} Create({{constructorParameters}}) =>
                    new(Guid.NewGuid(), {{string.Join(", ", resource.Fields.Select(field => Camel(field.Name)))}});

                public void ApplyUpdate({{constructorParameters}})
                {
            {{assignments}}{{versionIncrement}}
                }
            }
            """;
    }

    private static string BuildContracts(
        string projectPrefix,
        ComposerModuleDefinition module,
        ComposerResourceDefinition resource)
    {
        var createParameters = string.Join(",\n    ", resource.Fields.Select(BuildContractParameter));
        var responseParameters = new List<string> { "Guid Id" };
        responseParameters.AddRange(resource.Fields.Select(field => $"{FieldClrType(field)} {field.Name}"));
        if (Has(resource, ComposerResourceBehavior.Concurrency))
            responseParameters.Add("int Version");

        return $$"""
            #nullable enable

            using System.ComponentModel.DataAnnotations;

            namespace {{projectPrefix}}.Application.GeneratedModules.{{module.Name}};

            public sealed record {{resource.Name}}CreateRequest(
                {{createParameters}});

            public sealed record {{resource.Name}}UpdateRequest(
                {{createParameters}});

            public sealed record {{resource.Name}}Response(
                {{string.Join(",\n    ", responseParameters)}});
            """;
    }

    private static string BuildApplication(
        string projectPrefix,
        ComposerModuleDefinition module,
        ComposerResourceDefinition resource)
    {
        var entity = $"{projectPrefix}.Domain.GeneratedModules.{module.Name}.{resource.Name}";
        var createArgs = string.Join(", ", resource.Fields.Select(field => $"request.{field.Name}"));
        var responseArgs = new List<string> { "entity.Id" };
        responseArgs.AddRange(resource.Fields.Select(field => $"entity.{field.Name}"));
        if (Has(resource, ComposerResourceBehavior.Concurrency))
            responseArgs.Add("entity.Version");

        var authorization = Has(resource, ComposerResourceBehavior.Authorization)
            ? BuildAuthorizationPolicy(resource, entity)
            : string.Empty;
        var concurrency = Has(resource, ComposerResourceBehavior.Concurrency)
            ? BuildConcurrencyPolicy(resource, entity)
            : string.Empty;

        return $$"""
            #nullable enable

            using FoundationKit.Application.Abstractions;
            using FoundationKit.Application.Crud;
            using FoundationKit.Application.Results;

            namespace {{projectPrefix}}.Application.GeneratedModules.{{module.Name}};

            public sealed class {{resource.Name}}Mapper
                : ICrudMapper<{{entity}}, Guid, {{resource.Name}}CreateRequest, {{resource.Name}}UpdateRequest, {{resource.Name}}Response>
            {
                public {{entity}} Create({{resource.Name}}CreateRequest request) =>
                    {{entity}}.Create({{createArgs}});

                public void ApplyUpdate({{entity}} entity, {{resource.Name}}UpdateRequest request) =>
                    entity.ApplyUpdate({{createArgs}});

                public {{resource.Name}}Response ToReadModel({{entity}} entity) =>
                    new({{string.Join(", ", responseArgs)}});
            }

            {{authorization}}

            {{concurrency}}
            """;
    }

    private static string BuildAuthorizationPolicy(ComposerResourceDefinition resource, string entity) => $$"""
        public sealed class {{resource.Name}}AuthorizationPolicy(ICurrentUser currentUser)
            : ICrudAuthorizationPolicy<{{entity}}, Guid>
        {
            private readonly ICurrentUser _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            public ValueTask<Result> AuthorizeAsync(
                CrudAuthorizationContext<{{entity}}, Guid> context,
                CancellationToken cancellationToken = default)
            {
                ArgumentNullException.ThrowIfNull(context);
                var allowed = _currentUser.IsAuthenticated && _currentUser.IsInRole("admin");
                return ValueTask.FromResult(allowed
                    ? Result.Success()
                    : Result.Failure(Error.Forbidden(
                        "Generated.Authorization.AdminRequired",
                        "The generated reference resource requires an authenticated admin role.")));
            }
        }
        """;

    private static string BuildConcurrencyPolicy(ComposerResourceDefinition resource, string entity) => $$"""
        public sealed class {{resource.Name}}ConcurrencyPolicy
            : ICrudConcurrencyPolicy<{{entity}}, {{resource.Name}}UpdateRequest>
        {
            public Result Validate({{entity}} entity, {{resource.Name}}UpdateRequest request) =>
                Result.Failure(Error.PreconditionRequired(
                    "Generated.Version.Required",
                    "An If-Match concurrency token is required."));

            public Result Validate(
                {{entity}} entity,
                {{resource.Name}}UpdateRequest request,
                CrudConcurrencyPrecondition? precondition)
            {
                if (precondition is null)
                    return Validate(entity, request);

                var expected = $"\"{entity.Version}\"";
                return string.Equals(precondition.Token, expected, StringComparison.Ordinal)
                    ? Result.Success()
                    : Result.Failure(Error.PreconditionFailed(
                        "Generated.Version.PreconditionFailed",
                        "The resource changed since it was read."));
            }
        }
        """;

    private static string BuildEntityConfiguration(
        ComposerManifest manifest,
        string projectPrefix,
        ComposerModuleDefinition module,
        ComposerResourceDefinition resource)
    {
        var entity = $"{projectPrefix}.Domain.GeneratedModules.{module.Name}.{resource.Name}";
        var properties = new StringBuilder();
        foreach (var field in resource.Fields)
        {
            properties.Append("        builder.Property(entity => entity.").Append(field.Name)
                .Append(").HasMaxLength(").Append(field.MaximumLength).Append(')');
            if (field.Required)
                properties.Append(".IsRequired()");
            properties.AppendLine(";");
        }
        if (Has(resource, ComposerResourceBehavior.Concurrency))
            properties.AppendLine("        builder.Property(entity => entity.Version).IsConcurrencyToken();");

        return $$"""
            #nullable enable

            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;

            namespace {{projectPrefix}}.Infrastructure.GeneratedModules.{{module.Name}};

            public sealed class {{resource.Name}}EntityConfiguration
                : IEntityTypeConfiguration<{{entity}}>
            {
                public void Configure(EntityTypeBuilder<{{entity}}> builder)
                {
                    builder.ToTable({{JsonSerializer.Serialize(TableName(manifest, resource))}});
                    builder.HasKey(entity => entity.Id);
            {{properties.ToString().TrimEnd()}}
                }
            }
            """;
    }

    private static string BuildDbContext(
        ComposerManifest manifest,
        string projectPrefix,
        IReadOnlyList<(ComposerModuleDefinition Module, ComposerResourceDefinition Resource)> executable)
    {
        var dbSets = string.Join("\n", executable.Select(item =>
            $"    public DbSet<{projectPrefix}.Domain.GeneratedModules.{item.Module.Name}.{item.Resource.Name}> {item.Module.Name}{item.Resource.Name} => Set<{projectPrefix}.Domain.GeneratedModules.{item.Module.Name}.{item.Resource.Name}>();"));
        var hasIdempotency = executable.Any(item => item.Resource.Api.Idempotency != ComposerApiIdempotencyMode.Disabled);
        var idempotency = hasIdempotency
            ? $"\n        modelBuilder.AddFoundationIdempotencyStore({JsonSerializer.Serialize(IdempotencyTableName(manifest))});"
            : string.Empty;

        return $$"""
            #nullable enable

            using FoundationKit.Infrastructure.Idempotency;
            using Microsoft.EntityFrameworkCore;

            namespace {{projectPrefix}}.Infrastructure.GeneratedPlatform;

            public sealed class GeneratedDbContext(DbContextOptions<GeneratedDbContext> options) : DbContext(options)
            {
            {{dbSets}}

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    modelBuilder.ApplyConfigurationsFromAssembly(typeof(GeneratedDbContext).Assembly);{{idempotency}}
                    base.OnModelCreating(modelBuilder);
                }
            }
            """;
    }

    private static string BuildMigration(
        ComposerManifest manifest,
        string projectPrefix,
        IReadOnlyList<(ComposerModuleDefinition Module, ComposerResourceDefinition Resource)> executable)
    {
        var up = new StringBuilder();
        var down = new StringBuilder();
        foreach (var item in executable)
        {
            up.AppendLine(BuildResourceMigrationTable(manifest, item.Resource));
            down.Insert(0, $"        migrationBuilder.DropTable(name: {JsonSerializer.Serialize(TableName(manifest, item.Resource))});\n");
        }

        if (executable.Any(item => item.Resource.Api.Idempotency != ComposerApiIdempotencyMode.Disabled))
        {
            up.AppendLine(BuildIdempotencyMigrationTable(manifest));
            down.Insert(0, $"        migrationBuilder.DropTable(name: {JsonSerializer.Serialize(IdempotencyTableName(manifest))});\n");
        }

        return $$"""
            #nullable enable

            using Microsoft.EntityFrameworkCore.Infrastructure;
            using Microsoft.EntityFrameworkCore.Migrations;

            namespace {{projectPrefix}}.Infrastructure.GeneratedPlatform.Migrations;

            [DbContext(typeof({{projectPrefix}}.Infrastructure.GeneratedPlatform.GeneratedDbContext))]
            [Migration({{JsonSerializer.Serialize(InitialMigrationId)}})]
            public sealed class InitialGenerated : Migration
            {
                protected override void Up(MigrationBuilder migrationBuilder)
                {
            {{up.ToString().TrimEnd()}}
                }

                protected override void Down(MigrationBuilder migrationBuilder)
                {
            {{down.ToString().TrimEnd()}}
                }
            }
            """;
    }

    private static string BuildResourceMigrationTable(ComposerManifest manifest, ComposerResourceDefinition resource)
    {
        var columns = new List<string>
        {
            "Id = table.Column<Guid>(type: \"uniqueidentifier\", nullable: false)"
        };
        columns.AddRange(resource.Fields.Select(field =>
            $"{field.Name} = table.Column<string>(type: \"nvarchar({field.MaximumLength})\", maxLength: {field.MaximumLength}, nullable: {(!field.Required).ToString().ToLowerInvariant()})"));
        if (Has(resource, ComposerResourceBehavior.Concurrency))
            columns.Add("Version = table.Column<int>(type: \"int\", nullable: false)");

        return $$"""
                    migrationBuilder.CreateTable(
                        name: {{JsonSerializer.Serialize(TableName(manifest, resource))}},
                        columns: table => new
                        {
                            {{string.Join(",\n                ", columns)}}
                        },
                        constraints: table =>
                        {
                            table.PrimaryKey({{JsonSerializer.Serialize("PK_" + TableName(manifest, resource))}}, x => x.Id);
                        });
        """;
    }

    private static string BuildIdempotencyMigrationTable(ComposerManifest manifest)
    {
        var table = JsonSerializer.Serialize(IdempotencyTableName(manifest));
        return $$"""
                    migrationBuilder.CreateTable(
                        name: {{table}},
                        columns: table => new
                        {
                            ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                            OperationScope = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                            KeyHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                            RequestFingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                            State = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                            AcquiredUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                            ReplayUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                            CompletedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                            ResponseStatusCode = table.Column<int>(type: "int", nullable: true),
                            ResponseContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                            ResponseBody = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                            ResponseLocation = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                            ResponseEntityTag = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                        },
                        constraints: table =>
                        {
                            table.PrimaryKey(
                                {{JsonSerializer.Serialize("PK_" + IdempotencyTableName(manifest))}},
                                x => new { x.ProjectId, x.OperationScope, x.KeyHash });
                        });
                    migrationBuilder.CreateIndex(
                        name: {{JsonSerializer.Serialize("IX_" + IdempotencyTableName(manifest) + "_ReplayUntilUtc")}},
                        table: {{table}},
                        column: "ReplayUntilUtc");
        """;
    }

    private static string BuildAuditSupport(string projectPrefix) => $$"""
        #nullable enable

        using System.Collections.Concurrent;
        using FoundationKit.Application.Abstractions;
        using FoundationKit.Auditing;

        namespace {{projectPrefix}}.Application.GeneratedPlatform;

        public sealed class GeneratedClock : IClock
        {
            public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        }

        public sealed class GeneratedAuditSink : IAuditSink
        {
            private readonly ConcurrentQueue<AuditEvent> _events = new();
            public IReadOnlyCollection<AuditEvent> Events => _events.ToArray();

            public ValueTask WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
            {
                ArgumentNullException.ThrowIfNull(auditEvent);
                _events.Enqueue(auditEvent);
                return ValueTask.CompletedTask;
            }
        }

        public sealed class GeneratedAuditContextAccessor(ICurrentUser currentUser) : IAuditContextAccessor
        {
            private readonly ICurrentUser _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

            public AuditContext Current => new(
                ActorId: _currentUser.UserId?.ToString("D") ?? _currentUser.Email,
                CorrelationId: null,
                TenantId: null,
                Source: "generated-reference");
        }
        """;

    private static string BuildHttpIdentity(string projectPrefix) => $$"""
        #nullable enable

        using System.Security.Claims;
        using System.Text.Encodings.Web;
        using FoundationKit.Application.Abstractions;
        using Microsoft.AspNetCore.Authentication;
        using Microsoft.Extensions.Options;

        namespace {{projectPrefix}}.Api.GeneratedPlatform;

        public static class GeneratedAuthentication
        {
            public const string Scheme = "FoundationGenerated";
            public const string AdminPolicy = "FoundationGeneratedAdmin";
        }

        public sealed class GeneratedHeaderAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
        {
            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var rawUserId = Request.Headers["X-Foundation-User"].ToString().Trim();
                if (string.IsNullOrWhiteSpace(rawUserId))
                    return Task.FromResult(AuthenticateResult.NoResult());
                if (!Guid.TryParse(rawUserId, out var userId) || userId == Guid.Empty)
                {
                    return Task.FromResult(AuthenticateResult.Fail(
                        "X-Foundation-User must contain a non-empty GUID for the generated reference adapter."));
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userId.ToString("D")),
                    new(ClaimTypes.Name, userId.ToString("D"))
                };
                var email = Request.Headers["X-Foundation-Email"].ToString().Trim();
                if (!string.IsNullOrWhiteSpace(email))
                    claims.Add(new Claim(ClaimTypes.Email, email));
                foreach (var role in Request.Headers["X-Foundation-Roles"].ToString()
                             .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, GeneratedAuthentication.Scheme));
                return Task.FromResult(AuthenticateResult.Success(
                    new AuthenticationTicket(principal, GeneratedAuthentication.Scheme)));
            }
        }

        public sealed class GeneratedClaimsCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
        {
            private readonly IHttpContextAccessor _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
            private ClaimsPrincipal? User => _accessor.HttpContext?.User;

            public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

            public Guid? UserId =>
                Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                    ? userId
                    : null;

            public string? Email => User?.FindFirstValue(ClaimTypes.Email);

            public bool IsInRole(string role)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(role);
                return User?.IsInRole(role) == true;
            }
        }

        public sealed class GeneratedAnonymousCurrentUser : ICurrentUser
        {
            public bool IsAuthenticated => false;
            public Guid? UserId => null;
            public string? Email => null;
            public bool IsInRole(string role) => false;
        }
        """;

    private static string BuildApiSupport(
        string projectPrefix,
        IReadOnlyList<(ComposerModuleDefinition Module, ComposerResourceDefinition Resource)> executable)
    {
        var etagClasses = string.Join("\n\n", executable
            .Where(item => Has(item.Resource, ComposerResourceBehavior.Concurrency))
            .Select(item => $$"""
                public sealed class {{item.Resource.Name}}EntityTagProvider
                    : IFoundationApiEntityTagProvider<{{projectPrefix}}.Application.GeneratedModules.{{item.Module.Name}}.{{item.Resource.Name}}Response>
                {
                    public string? GetEntityTag({{projectPrefix}}.Application.GeneratedModules.{{item.Module.Name}}.{{item.Resource.Name}}Response response) =>
                        $"\"{response.Version}\"";
                }
                """));

        return $$"""
            #nullable enable

            using FoundationKit.WebApi.Api;

            namespace {{projectPrefix}}.Api.GeneratedPlatform;

            {{etagClasses}}
            """;
    }

    private static string BuildProgram(
        ComposerManifest manifest,
        string projectPrefix,
        IReadOnlyList<(ComposerModuleDefinition Module, ComposerResourceDefinition Resource)> executable)
    {
        var usesAudit = executable.Any(item => Has(item.Resource, ComposerResourceBehavior.Auditing));
        var usesAuthorization = executable.Any(item => Has(item.Resource, ComposerResourceBehavior.Authorization));
        var usesIdempotency = executable.Any(item => item.Resource.Api.Idempotency != ComposerApiIdempotencyMode.Disabled);
        var registrations = new StringBuilder();
        var mappings = new StringBuilder();

        foreach (var item in executable)
        {
            var resource = item.Resource;
            var domain = $"{projectPrefix}.Domain.GeneratedModules.{item.Module.Name}.{resource.Name}";
            var application = $"{projectPrefix}.Application.GeneratedModules.{item.Module.Name}";
            if (Has(resource, ComposerResourceBehavior.Authorization))
            {
                registrations.AppendLine(
                    $"builder.Services.AddScoped<ICrudAuthorizationPolicy<{domain}, Guid>, {application}.{resource.Name}AuthorizationPolicy>();");
            }
            if (Has(resource, ComposerResourceBehavior.Concurrency))
            {
                registrations.AppendLine(
                    $"builder.Services.AddScoped<ICrudConcurrencyPolicy<{domain}, {application}.{resource.Name}UpdateRequest>, {application}.{resource.Name}ConcurrencyPolicy>();");
                registrations.AppendLine(
                    $"builder.Services.AddSingleton<IFoundationApiEntityTagProvider<{application}.{resource.Name}Response>, {projectPrefix}.Api.GeneratedPlatform.{resource.Name}EntityTagProvider>();");
            }

            registrations.AppendLine(
                $"builder.Services.AddFoundationEfCrudModule<{domain}, Guid, {application}.{resource.Name}CreateRequest, {application}.{resource.Name}UpdateRequest, {application}.{resource.Name}Response, {application}.{resource.Name}Mapper, GeneratedDbContext>(module => module");
            registrations.AppendLine($"    .Named({JsonSerializer.Serialize(resource.Name)}, {JsonSerializer.Serialize(resource.Route)})");
            registrations.AppendLine("    .Crud(options => options.MaximumPageSize = 100)");
            registrations.AppendLine("    .Api(api =>");
            registrations.AppendLine("    {");
            registrations.AppendLine($"        api.RoutePrefix = {JsonSerializer.Serialize(resource.Api.RoutePrefix)};");
            registrations.AppendLine($"        api.Idempotency = FoundationApiIdempotencyMode.{ApiIdempotencyEnum(resource.Api.Idempotency)};");
            registrations.AppendLine($"        api.Concurrency = FoundationApiConcurrencyMode.{ApiConcurrencyEnum(resource.Api.Concurrency)};");
            registrations.AppendLine($"        api.MaximumFilters = {resource.Api.MaximumFilters};");
            registrations.AppendLine($"        api.MaximumSorts = {resource.Api.MaximumSorts};");
            registrations.AppendLine("    })");
            if (Has(resource, ComposerResourceBehavior.Auditing))
                registrations.AppendLine("    .Auditing()");
            if (Has(resource, ComposerResourceBehavior.Authorization))
                registrations.AppendLine("    .Authorization(GeneratedAuthentication.AdminPolicy)");
            if (Has(resource, ComposerResourceBehavior.Concurrency))
                registrations.AppendLine("    .Concurrency()");
            registrations.AppendLine("    );");

            if (Has(resource, ComposerResourceBehavior.Auditing))
            {
                registrations.AppendLine(
                    $"builder.Services.AddScoped<ICrudOperationObserver<{domain}, Guid>, CrudAuditObserver<{domain}, Guid>>();");
            }

            mappings.AppendLine(
                $"var {Camel(resource.Name)}Module = app.Services.GetRequiredService<FoundationModuleDefinition<{domain}, Guid>>();");
            mappings.AppendLine(
                $"app.MapFoundationCrud<{domain}, Guid, {application}.{resource.Name}CreateRequest, {application}.{resource.Name}UpdateRequest, {application}.{resource.Name}Response>({Camel(resource.Name)}Module);");
        }

        var identityServices = usesAuthorization
            ? $$"""
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddScoped<ICurrentUser, GeneratedClaimsCurrentUser>();
                builder.Services.AddAuthentication(GeneratedAuthentication.Scheme)
                    .AddScheme<AuthenticationSchemeOptions, GeneratedHeaderAuthenticationHandler>(GeneratedAuthentication.Scheme, _ => { });
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy(GeneratedAuthentication.AdminPolicy, policy =>
                    {
                        policy.AddAuthenticationSchemes(GeneratedAuthentication.Scheme);
                        policy.RequireAuthenticatedUser();
                        policy.RequireRole("admin");
                    });
                });
                """
            : "builder.Services.AddSingleton<ICurrentUser, GeneratedAnonymousCurrentUser>();";

        var auditServices = usesAudit
            ? $$"""
                builder.Services.AddSingleton<GeneratedClock>();
                builder.Services.AddSingleton<IClock>(services => services.GetRequiredService<GeneratedClock>());
                builder.Services.AddSingleton<GeneratedAuditSink>();
                builder.Services.AddSingleton<IAuditSink>(services => services.GetRequiredService<GeneratedAuditSink>());
                builder.Services.AddScoped<IAuditContextAccessor, GeneratedAuditContextAccessor>();
                builder.Services.AddScoped<IAuditRecorder, AuditRecorder>();
                """
            : string.Empty;

        var idempotencyServices = usesIdempotency
            ? "builder.Services.AddFoundationEfIdempotencyStore<GeneratedDbContext>();"
            : string.Empty;
        var auditUsing = usesAudit ? "using FoundationKit.Auditing;" : string.Empty;
        var authPipeline = usesAuthorization
            ? "app.UseAuthentication();\napp.UseAuthorization();"
            : string.Empty;
        var auditEndpoint = usesAudit
            ? usesAuthorization
                ? "app.MapGet(\"/api/foundationkit/audit\", (GeneratedAuditSink sink) => Results.Ok(new { count = sink.Events.Count, events = sink.Events.Select(item => new { item.Action, item.SubjectType, item.SubjectId, item.ActorId }) })).RequireAuthorization(GeneratedAuthentication.AdminPolicy);"
                : "app.MapGet(\"/api/foundationkit/audit\", (GeneratedAuditSink sink) => Results.Ok(new { count = sink.Events.Count, events = sink.Events.Select(item => new { item.Action, item.SubjectType, item.SubjectId, item.ActorId }) }));"
            : string.Empty;
        var swaggerSecurity = usesAuthorization
            ? """
                options.AddSecurityDefinition("FoundationGeneratedUser", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-Foundation-User",
                    Description = "Generated reference adapter only: non-empty GUID user identifier."
                });
                options.AddSecurityDefinition("FoundationGeneratedRoles", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-Foundation-Roles",
                    Description = "Generated reference adapter only: include admin for protected CRUD routes."
                });
                """
            : string.Empty;

        return $$"""
            #nullable enable

            using FoundationKit.Application.Abstractions;
            using FoundationKit.Application.Crud;
            using FoundationKit.Application.Modules;
            {{auditUsing}}
            using FoundationKit.Infrastructure;
            using FoundationKit.Infrastructure.Idempotency;
            using FoundationKit.Infrastructure.Platform;
            using FoundationKit.WebApi;
            using FoundationKit.WebApi.Api;
            using FoundationKit.WebApi.Crud;
            using {{projectPrefix}}.Api.GeneratedPlatform;
            {{(usesAudit ? $"using {projectPrefix}.Application.GeneratedPlatform;" : string.Empty)}}
            using {{projectPrefix}}.Infrastructure.GeneratedPlatform;
            using Microsoft.AspNetCore.Authentication;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.OpenApi.Models;

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddFoundationInfrastructure();
            builder.Services.AddFoundationWebApi();
            builder.Services.AddFoundationProject({{JsonSerializer.Serialize(ProjectIdentity(manifest))}});
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = {{JsonSerializer.Serialize(manifest.Name + " API")}},
                    Version = "v1",
                    Description = "FoundationKit Composer generated pre-frontend full-stack proof API."
                });
                {{swaggerSecurity}}
            });

            {{identityServices}}
            {{auditServices}}

            var connectionString = builder.Configuration.GetConnectionString("Generated")
                ?? throw new InvalidOperationException(
                    "Connection string 'Generated' is required. Supply it at runtime, for example through ConnectionStrings__Generated.");
            builder.Services.AddDbContext<GeneratedDbContext>(options =>
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.MigrationsAssembly(typeof(GeneratedDbContext).Assembly.FullName);
                    sql.MigrationsHistoryTable({{JsonSerializer.Serialize(MigrationHistoryTableName(manifest))}});
                }));
            {{idempotencyServices}}

            {{registrations.ToString().TrimEnd()}}

            var app = builder.Build();
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Generated API v1"));
            {{authPipeline}}
            app.UseFoundationRequestPipeline();

            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GeneratedDbContext>();
                await db.Database.MigrateAsync();
            }

            app.MapGet("/api/foundationkit/health", () => Results.Ok(new
            {
                status = "healthy",
                projectId = {{JsonSerializer.Serialize(ProjectIdentity(manifest))}},
                databaseNamespace = {{JsonSerializer.Serialize(SqlNamespace(manifest))}}
            }));
            {{auditEndpoint}}
            {{mappings.ToString().TrimEnd()}}
            app.Run();

            public partial class Program { }
            """;
    }

    private static string BuildFullStackReport(
        ComposerManifest manifest,
        IReadOnlyList<(ComposerModuleDefinition Module, ComposerResourceDefinition Resource)> executable)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Generated full-stack pre-frontend surface");
        builder.AppendLine();
        builder.Append("Project identity: `").Append(ProjectIdentity(manifest)).AppendLine("`");
        builder.Append("Database namespace: `").Append(SqlNamespace(manifest)).AppendLine("`");
        builder.Append("Migration history table: `").Append(MigrationHistoryTableName(manifest)).AppendLine("`");
        builder.AppendLine();
        builder.AppendLine("Executable resources:");
        foreach (var item in executable)
        {
            builder.Append("- `").Append(item.Module.Name).Append('.').Append(item.Resource.Name)
                .Append("` → `/").Append(item.Resource.Api.RoutePrefix).Append('/').Append(item.Resource.Route).AppendLine("`");
        }
        builder.AppendLine();
        builder.AppendLine("- Supply the SQL Server connection string at runtime through `ConnectionStrings__Generated`; Composer does not emit database credentials.");
        builder.AppendLine("- Generated proof authentication uses `X-Foundation-User` plus `X-Foundation-Roles: admin`; it is a bounded reference adapter, not a production identity system.");
        builder.AppendLine("- Authentication/authorization execute before FoundationKit durable-idempotency replay in the generated host pipeline.");
        builder.AppendLine("- Runtime Postman must be derived from `/swagger/v1/swagger.json` through the FoundationKit OpenAPI-to-Postman generator; it is intentionally not hand-authored here.");
        return builder.ToString();
    }

    private static string BuildContractParameter(ComposerResourceField field)
    {
        var attributes = new List<string>();
        if (field.Required)
            attributes.Add("Required");
        attributes.Add($"StringLength({field.MaximumLength})");
        return $"[property: {string.Join(", ", attributes)}] {FieldClrType(field)} {field.Name}";
    }

    private static string FieldClrType(ComposerResourceField field) => field.Type switch
    {
        ComposerResourceFieldType.Text => field.Required ? "string" : "string?",
        _ => throw new InvalidOperationException($"Unsupported executable field type '{field.Type}'.")
    };

    private static bool Has(ComposerResourceDefinition resource, ComposerResourceBehavior behavior) =>
        resource.Behaviors.Contains(behavior);

    private static string ApiIdempotencyEnum(ComposerApiIdempotencyMode value) => value switch
    {
        ComposerApiIdempotencyMode.Disabled => "Disabled",
        ComposerApiIdempotencyMode.Optional => "Optional",
        ComposerApiIdempotencyMode.Required => "Required",
        _ => throw new InvalidOperationException($"Unsupported API idempotency mode '{value}'.")
    };

    private static string ApiConcurrencyEnum(ComposerApiConcurrencyMode value) => value switch
    {
        ComposerApiConcurrencyMode.ApplicationPolicy => "ApplicationPolicy",
        ComposerApiConcurrencyMode.RequireIfMatch => "RequireIfMatch",
        _ => throw new InvalidOperationException($"Unsupported API concurrency mode '{value}'.")
    };

    private static string Camel(string value) => char.ToLowerInvariant(value[0]) + value[1..];

    private static string ProjectIdentity(ComposerManifest manifest)
    {
        var normalized = new string(manifest.Name.ToLowerInvariant()
            .Select(character => char.IsAsciiLetterOrDigit(character) ? character : '-')
            .ToArray());
        while (normalized.Contains("--", StringComparison.Ordinal))
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        normalized = normalized.Trim('-');
        var hash = ShortHash(manifest.Name);
        if (normalized.Length > 48)
            normalized = normalized[..48].TrimEnd('-');
        return $"{normalized}-{hash}";
    }

    private static string SqlNamespace(ComposerManifest manifest)
    {
        var normalized = new string(manifest.Name.ToLowerInvariant()
            .Select(character => char.IsAsciiLetterOrDigit(character) ? character : '_')
            .ToArray()).Trim('_');
        if (normalized.Length > 32)
            normalized = normalized[..32].TrimEnd('_');
        return $"{normalized}_{ShortHash(manifest.Name)}";
    }

    private static string TableName(ComposerManifest manifest, ComposerResourceDefinition resource)
    {
        var suffix = SqlIdentifier(resource.Route);
        if (suffix.Length > 56)
            suffix = $"{suffix[..47].TrimEnd('_')}_{ShortHash(resource.Route)}";
        return $"{SqlNamespace(manifest)}_{suffix}";
    }

    private static string IdempotencyTableName(ComposerManifest manifest) =>
        $"{SqlNamespace(manifest)}_idempotency";

    private static string MigrationHistoryTableName(ComposerManifest manifest) =>
        $"{SqlNamespace(manifest)}_migrations";

    private static string SqlIdentifier(string value) =>
        new(value.ToLowerInvariant().Select(character => char.IsAsciiLetterOrDigit(character) ? character : '_').ToArray());

    private static string ShortHash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..8].ToLowerInvariant();
}
