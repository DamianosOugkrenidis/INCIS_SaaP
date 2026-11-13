// Author: Damianos Pythagoras Ougkrenidis
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using INCIS_SaaP.Infrastructure.Filters;
using INCIS_SaaP.Infrastructure.Repository;
using INCIS_SaaP.Infrastructure.Tenancy;
using INCIS_SaaP.Infrastructure.Security;
using INCIS_SaaP.Infrastructure.Validators;
// Advanced Architecture & Security
using INCIS_SaaP.Domain.Common;
using INCIS_SaaP.Architecture.CQRS.Behaviors;
using INCIS_SaaP.Architecture.EventSourcing;
using INCIS_SaaP.Architecture.MessageBus;
using INCIS_SaaP.Architecture.OutboxPattern;
using INCIS_SaaP.Infrastructure.Encryption;
using INCIS_SaaP.Infrastructure.Privacy;
using INCIS_SaaP.Infrastructure.Resilience;
using INCIS_SaaP.Infrastructure.Security.ABAC;
using INCIS_SaaP.Infrastructure.Security.DLP;
using INCIS_SaaP.Infrastructure.Security.ZeroTrust;
// ML & AI Services
using INCIS_SaaP.ML.FormulaIntelligence;
using INCIS_SaaP.ML.RegulatoryIntelligence;
using INCIS_SaaP.ML.QualityPrediction;
using INCIS_SaaP.ML.SemanticSearch;
using INCIS_SaaP.ML.DocumentIntelligence;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Hangfire;
using Hangfire.SqlServer;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using Asp.Versioning;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

// ============================================================
// SERILOG BOOTSTRAP (early init for startup logging)
// ============================================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting INCIS SaaP application...");

    var builder = WebApplication.CreateBuilder(args);

    // ============================================================
    // HOSTING MODE (HTTPS enforcement vs. reverse-proxy / container)
    // ============================================================
    // Σε Development: HTTP (χωρίς secure-cookie enforcement).
    // Σε Production: από προεπιλογή απαιτεί HTTPS (secure cookies + HttpsRedirection).
    // Όταν το app τρέχει σε container πίσω από reverse proxy που τερματίζει το TLS
    // (ή σε plain-HTTP container), όρισε "Hosting:RequireHttps=false" ώστε:
    //   * τα cookies να μη σπάνε σε HTTP internal requests (SameAsRequest),
    //   * να μη γίνεται HttpsRedirection προς μη-υπάρχον HTTPS endpoint,
    //   * να διαβάζονται τα X-Forwarded-* από τον proxy.
    var requireHttps = !builder.Environment.IsDevelopment()
        && builder.Configuration.GetValue("Hosting:RequireHttps", true);

    var secureCookiePolicy = requireHttps
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;

    // Forwarded headers: όταν είμαστε πίσω από proxy (μη-Development ή explicit flag),
    // εμπιστευόμαστε X-Forwarded-For/Proto ώστε το app να "βλέπει" σωστά το scheme/IP.
    var behindProxy = !builder.Environment.IsDevelopment();
    if (behindProxy)
    {
        builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
            // Σε container/orchestrator τα proxy IPs είναι δυναμικά· καθαρίζουμε τις
            // default λίστες ώστε να γίνονται δεκτά τα forwarded headers.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    // ============================================================
    // SERILOG - Full initialization from appsettings
    // ============================================================
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "INCIS_SaaP")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));

    // ============================================================
    // MVC + API SERVICES
    // ============================================================
    builder.Services.Configure<INCIS_SaaP.Configuration.ApiWritePolicyOptions>(
        builder.Configuration.GetSection(INCIS_SaaP.Configuration.ApiWritePolicyOptions.SectionName));
    builder.Services.Configure<INCIS_SaaP.Configuration.ApiCsrfOptions>(
        builder.Configuration.GetSection(INCIS_SaaP.Configuration.ApiCsrfOptions.SectionName));

    builder.Services.AddControllersWithViews(options =>
    {
        // Global exception filter for API endpoints
        options.Filters.Add<GlobalExceptionFilter>();
        // CSRF (cookie-authenticated API writes) — Phase 0 default (log-only)
        options.Filters.Add<INCIS_SaaP.Filters.ApiCsrfFilter>();
        // H-2: πολιτική εγγραφής για τα API write endpoints (Phase 0 = log-only by default)
        options.Filters.Add<INCIS_SaaP.Filters.ApiWritePolicyFilter>();
    })
    .AddJsonOptions(options =>
    {
        // Αποφυγή 500 σε serialization EF entities με navigation cycles
        // (π.χ. RawMaterial -> Supplier -> RawMaterials).
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

    // ============================================================
    // FLUENT VALIDATION
    // ============================================================
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddFluentValidationClientsideAdapters();
    builder.Services.AddValidatorsFromAssemblyContaining<FormulaValidator>();

    // ============================================================
    // API VERSIONING
    // ============================================================
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version"));
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // ============================================================
    // MEMORY CACHE & RESPONSE COMPRESSION
    // ============================================================
    builder.Services.AddMemoryCache();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<GzipCompressionProvider>();
        options.Providers.Add<BrotliCompressionProvider>();
        options.MimeTypes = new[] {
            "text/plain", "text/css", "application/javascript",
            "text/html", "application/xml", "text/xml",
            "application/json", "text/json", "image/svg+xml"
        };
    });

    // ============================================================
    // SWAGGER / OPENAPI
    // ============================================================
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "INCIS SaaP API",
            Version = "v1",
            Description = "API for Cosmetic Formula Management System",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Damianos Pythagoras Ougkrenidis"
            }
        });

        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme.",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ============================================================
    // OPENTELEMETRY (Observability)
    // ============================================================
    var serviceName = builder.Configuration.GetValue<string>("OpenTelemetry:ServiceName") ?? "INCIS-SaaP";
    var otelDevelopment = builder.Environment.IsDevelopment();

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(serviceName))
        .WithTracing(tracing =>
        {
            tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                // Σε production ΔΕΝ καταγράφουμε το πλήρες SQL statement:
                // (α) αποφυγή διαρροής ευαίσθητων δεδομένων στα traces (GDPR),
                // (β) λιγότερο overhead. Στο development παραμένει για debugging.
                .AddSqlClientInstrumentation(options => options.SetDbStatementForText = otelDevelopment);

            // ConsoleExporter ΜΟΝΟ σε development. Σε production το console I/O ανά trace
            // είναι ακριβό/blocking — τα traces πρέπει να πηγαίνουν σε proper backend (OTLP).
            if (otelDevelopment)
                tracing.AddConsoleExporter();
        })
        .WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();

            if (otelDevelopment)
                metrics.AddConsoleExporter();
        });

    // ============================================================
    // REPOSITORY PATTERN & SERVICE LAYER
    // ============================================================
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Tenant service
    builder.Services.AddScoped<ITenantService, TenantService>();

    // Feature flags & formula services
    builder.Services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
    builder.Services.AddScoped<IFormulaGroupingService, FormulaGroupingService>();
    builder.Services.AddScoped<FormulaApprovalService>();
    builder.Services.AddScoped<FormulaCloneService>();
    builder.Services.AddScoped<FormulaTemplateService>();
    builder.Services.AddScoped<FormulaVersionService>();
    builder.Services.AddScoped<ExportService>();
    builder.Services.AddScoped<ImportService>();
    builder.Services.AddScoped<IInciCatalogImportService, InciCatalogImportService>();
    builder.Services.AddScoped<AuditService>();
    builder.Services.AddScoped<AllergenService>();
    builder.Services.AddScoped<HeavyMetalComplianceService>();
    builder.Services.AddScoped<PriceCalculationService>();
    builder.Services.AddScoped<FormulaDocumentService>();
    builder.Services.AddScoped<JwtService>();
    builder.Services.AddScoped<TwoFactorService>();
    builder.Services.AddScoped<EmailService>();

    // ============================================================
    // ADVANCED ARCHITECTURE (DDD, CQRS, Event Sourcing)
    // ============================================================
    builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
    builder.Services.AddScoped<ICQRSDispatcher, CQRSDispatcher>();
    builder.Services.AddScoped<IEventStore, SqlEventStore>();
    builder.Services.AddSingleton<IMessageBus, InProcessMessageBus>();
    builder.Services.AddScoped<IOutboxPublisher, OutboxPublisher>();
    builder.Services.AddScoped<IOutboxProcessor, OutboxProcessor>();
    if (!builder.Configuration.GetValue<bool>("DisableBackgroundServices"))
        builder.Services.AddHostedService<OutboxBackgroundService>();
    builder.Services.AddSingleton<ICircuitBreakerFactory, CircuitBreakerFactory>();

    // ============================================================
    // DATA PROTECTION (Advanced Encryption, Tokenization, Key Management)
    // ============================================================
    builder.Services.AddScoped<IKeyManagementService, KeyManagementService>();
    builder.Services.AddScoped<ITokenizationService, TokenizationService>();

    // ============================================================
    // ACCESS CONTROL (ABAC, Zero Trust, DLP)
    // ============================================================
    builder.Services.AddScoped<IAbacEngine, AbacEngine>();
    builder.Services.AddScoped<IZeroTrustEngine, ZeroTrustEngine>();
    builder.Services.AddScoped<IDlpService, DlpService>();

    // ============================================================
    // PRIVACY ENGINEERING (GDPR, Data Classification)
    // ============================================================
    builder.Services.AddScoped<IGdprService, GdprService>();

    // ============================================================
    // ML & AI SERVICES
    // ============================================================
    // Real ML.NET model (singleton: holds trained transformer + prediction engine)
    builder.Services.AddSingleton<IFormulaSuccessModel, FormulaSuccessModel>();
    builder.Services.AddScoped<IFormulaIntelligenceService, FormulaIntelligenceService>();
    builder.Services.AddScoped<IRegulatoryIntelligenceService, RegulatoryIntelligenceService>();
    builder.Services.AddScoped<IQualityPredictionService, QualityPredictionService>();
    builder.Services.AddScoped<ISemanticSearchService, SemanticSearchService>();
    builder.Services.AddScoped<IDocumentIntelligenceService, DocumentIntelligenceService>();

    // ============================================================
    // HEALTH CHECKS
    // ============================================================
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy())
        .AddDbContextCheck<INCISDbContext>("database");

    // ============================================================
    // DATABASE (Entity Framework Core + SQL Server)
    // ============================================================
    // Σε production προτεραιότητα στο env var / Azure App Setting (ώστε να μη χρησιμοποιηθεί το dev default του appsettings.json)
    var connectionString = Environment.GetEnvironmentVariable("INCIS_DB_CONNECTION_STRING")
                          ?? builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        connectionString = "Server=PYTHAGORAS;Database=INCIS_SaaP;Trusted_Connection=True;TrustServerCertificate=True;Max Pool Size=100;";
    }

    builder.Services.AddDbContext<INCISDbContext>(options =>
        options
            .UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(60);
            })
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

    // ============================================================
    // DISTRIBUTED CACHE (SQL Server — χρησιμοποιεί την υπάρχουσα βάση)
    // ============================================================
    // Microsoft-native distributed cache πάνω στην ίδια SQL Server βάση:
    // δεν χρειάζεται εξωτερικός server (π.χ. Redis) και δουλεύει σε multi-instance
    // (Azure App Service scale-out). Αν Cache:UseSqlServer=false ή λείπει connection
    // string, πέφτει σε in-memory distributed cache (single instance).
    var cacheSchemaName = builder.Configuration.GetValue("Cache:SchemaName", "dbo")!;
    var cacheTableName = builder.Configuration.GetValue("Cache:TableName", "AppCache")!;
    var useSqlServerCache = builder.Configuration.GetValue("Cache:UseSqlServer", true)
                            && !string.IsNullOrWhiteSpace(connectionString);

    if (useSqlServerCache)
    {
        builder.Services.AddDistributedSqlServerCache(options =>
        {
            options.ConnectionString = connectionString;
            options.SchemaName = cacheSchemaName;
            options.TableName = cacheTableName;
        });
        Log.Information("Distributed cache: SQL Server ({Schema}.{Table})", cacheSchemaName, cacheTableName);
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
        Log.Information("Distributed cache: in-memory (SQL Server cache disabled/unavailable)");
    }

    // Resilient cache abstraction (JSON serialization + graceful degradation)
    builder.Services.AddSingleton<INCIS_SaaP.Services.ICacheService, INCIS_SaaP.Services.CacheService>();

    // ============================================================
    // HANGFIRE (Background Jobs)
    // ============================================================
    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
    // Επιτρέπει απενεργοποίηση background services σε CI/integration tests (χωρίς SQL Server)
    var disableBackgroundServices = builder.Configuration.GetValue<bool>("DisableBackgroundServices");
    if (!disableBackgroundServices)
        builder.Services.AddHangfireServer();

    // ============================================================
    // DATA PROTECTION (DPAPI encryption)
    // ============================================================
    // DataProtection keys: σε Azure App Service το %HOME% είναι persistent & κοινό
    // ανάμεσα σε instances (scale-out) — διατηρεί έγκυρα cookies/antiforgery/tokens.
    // Τοπικά πέφτει σε LocalApplicationData.
    var dpBuilder = builder.Services.AddDataProtection().SetApplicationName("INCIS_SaaP");
    var dpBlobConn = builder.Configuration["DataProtection:BlobConnectionString"];
    var dpKeyVaultKeyId = builder.Configuration["DataProtection:KeyVaultKeyId"];

    if (!string.IsNullOrWhiteSpace(dpBlobConn))
    {
        // Azure Blob Storage: κοινό & ανθεκτικό persistence ανάμεσα σε instances/App Service plans
        var container = builder.Configuration["DataProtection:BlobContainer"] ?? "dataprotection-keys";
        var blobName = builder.Configuration["DataProtection:BlobName"] ?? "keys.xml";
        dpBuilder.PersistKeysToAzureBlobStorage(dpBlobConn, container, blobName);

        // Προαιρετικό encryption-at-rest των keys με Azure Key Vault
        if (!string.IsNullOrWhiteSpace(dpKeyVaultKeyId))
            dpBuilder.ProtectKeysWithAzureKeyVault(new Uri(dpKeyVaultKeyId), new Azure.Identity.DefaultAzureCredential());

        Log.Information("DataProtection keys persisted to Azure Blob Storage.");
    }
    else
    {
        // Τοπικά / single Azure App Service: filesystem (%HOME% persistent σε App Service)
        var azureHome = Environment.GetEnvironmentVariable("HOME");
        var keysPath = !string.IsNullOrEmpty(azureHome)
            ? Path.Combine(azureHome, "ASP.NET", "DataProtection-Keys")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "INCIS_SaaP", "keys");
        Directory.CreateDirectory(keysPath);
        dpBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
    }

    // ============================================================
    // IDENTITY (Authentication & Authorization)
    // ============================================================
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password settings - OWASP compliant
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 12;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

        // Sign in settings
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<INCISDbContext>()
    .AddDefaultTokenProviders()
    .AddPasswordValidator<CustomPasswordValidator<ApplicationUser>>();

    builder.Services.AddScoped<IPasswordValidator<ApplicationUser>, CustomPasswordValidator<ApplicationUser>>();

    // Configure Identity cookie for HTTP support in Development
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = secureCookiePolicy;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

    // ============================================================
    // SESSION
    // ============================================================
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SecurePolicy = secureCookiePolicy;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

    // ============================================================
    // RATE LIMITING
    // ============================================================
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("Strict", context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "default",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

        options.AddPolicy("Login", context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "default",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(5),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));
    });

    // ============================================================
    // ANTIFORGERY (CSRF)
    // ============================================================
    builder.Services.AddAntiforgery(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = secureCookiePolicy;
        options.FormFieldName = "__RequestVerificationToken";
        options.HeaderName = "X-CSRF-TOKEN";
    });

    // ============================================================
    // SIGNALR
    // ============================================================
    builder.Services.AddSignalR();

    // ============================================================
    // BUILD APP
    // ============================================================
    var app = builder.Build();

    // ============================================================
    // MIDDLEWARE PIPELINE
    // ============================================================
    // Forwarded headers ΠΡΩΤΑ: όταν είμαστε πίσω από reverse proxy (container/orchestrator),
    // ώστε Request.Scheme/RemoteIp να αντικατοπτρίζουν τον πραγματικό client πριν τρέξει
    // οτιδήποτε βασίζεται στο scheme (HttpsRedirection, secure cookies, HSTS).
    if (behindProxy)
    {
        app.UseForwardedHeaders();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // HSTS μόνο όταν πραγματικά επιβάλλουμε HTTPS (αλλιώς άσκοπο σε plain-HTTP container).
        if (requireHttps)
            app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
        if (!disableBackgroundServices)
            app.UseHangfireDashboard();
    }

    // Swagger μόνο σε Development (αποφυγή αποκάλυψης API σε production SaaS).
    // Για ενεργοποίηση σε prod: όρισε App Setting "Swagger:Enabled" = true.
    if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "INCIS SaaP API v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "INCIS SaaP API Documentation";
        });
    }

    // Health checks
    app.MapHealthChecks("/health");

    // Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString() ?? "unknown");
            diagnosticContext.Set("UserId", httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous");
        };
    });

    // Security headers (pen-test hardened + CSP)
    app.UsePenTestSecurityHeaders();
    app.UseSecurityHeaders();

    if (requireHttps)
    {
        app.UseHttpsRedirection();
    }
    app.UseStaticFiles();
    app.UseRouting();

    // Response compression
    app.UseResponseCompression();

    // Rate limiting
    app.UseRateLimiter();

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Tenant resolution (after auth, before controllers)
    app.UseTenantResolution();

    // Session
    app.UseSession();

    // Antiforgery
    app.UseAntiforgery();

    // ============================================================
    // ROUTES
    // ============================================================
    app.MapGet("/", context =>
    {
        // Ξεκίνα από τη σελίδα Login για μη συνδεδεμένους χρήστες.
        // Οι ήδη συνδεδεμένοι πάνε στο dashboard (Home/Index) — με ρητό path
        // ώστε να ΜΗΝ ξαναπερνά από αυτό το "/" endpoint (αποφυγή redirect loop).
        if (context.User?.Identity?.IsAuthenticated == true)
            context.Response.Redirect("/Home/Index");
        else
            context.Response.Redirect("/Account/Login");
        return Task.CompletedTask;
    });

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // SignalR Hubs
    app.MapHub<INCIS_SaaP.Hubs.NotificationHub>("/hubs/notifications");
    app.MapHub<INCIS_SaaP.Hubs.FormulaCollaborationHub>("/hubs/formula-collaboration");

    // ============================================================
    // SEED DATABASE
    // ============================================================
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<INCISDbContext>();
        // Relational → migrations· non-relational (π.χ. InMemory σε tests) → EnsureCreated
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();

            // Δημιουργία του πίνακα του SQL Server distributed cache (idempotent).
            // Το AddDistributedSqlServerCache ΔΕΝ δημιουργεί μόνο του τον πίνακα.
            if (useSqlServerCache)
                await EnsureSqlCacheTableAsync(context, cacheSchemaName, cacheTableName);
        }
        else
            await context.Database.EnsureCreatedAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await CreateRolesAsync(roleManager);
        await CreateAdminUserAsync(userManager, builder.Configuration);
    }

    // Schedule background jobs (μόνο όταν τα background services είναι ενεργά)
    if (!disableBackgroundServices)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            JobScheduler.ScheduleJobs();
        });
    }

    Log.Information("INCIS SaaP application started successfully.");
    app.Run();
}
// Μην "καταπίνεις" το HostAbortedException: το WebApplicationFactory (integration tests)
// το ρίχνει σκόπιμα κατά το Build για να αποκτήσει τον host. Διαφορετικά σπάει το test harness.
catch (Exception ex) when (ex is not Microsoft.Extensions.Hosting.HostAbortedException
                           && Environment.GetEnvironmentVariable("INCIS_RETHROW_STARTUP") != "1")
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

// ============================================================
// HELPER METHODS
// ============================================================
// Δημιουργεί (αν δεν υπάρχει) τον πίνακα που απαιτεί το SQL Server distributed cache.
// Το schema ταιριάζει με αυτό που παράγει το `dotnet sql-cache create`.
async Task EnsureSqlCacheTableAsync(INCISDbContext context, string schema, string table)
{
    try
    {
        var sql = $@"
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{schema}')
    EXEC('CREATE SCHEMA [{schema}]');

IF OBJECT_ID(N'[{schema}].[{table}]', N'U') IS NULL
BEGIN
    CREATE TABLE [{schema}].[{table}] (
        [Id] NVARCHAR(449) COLLATE SQL_Latin1_General_CP1_CS_AS NOT NULL,
        [Value] VARBINARY(MAX) NOT NULL,
        [ExpiresAtTime] DATETIMEOFFSET NOT NULL,
        [SlidingExpirationInSeconds] BIGINT NULL,
        [AbsoluteExpiration] DATETIMEOFFSET NULL,
        CONSTRAINT [PK_{table}] PRIMARY KEY ([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_{table}_ExpiresAtTime]
        ON [{schema}].[{table}] ([ExpiresAtTime]);
END";
        await context.Database.ExecuteSqlRawAsync(sql);
        Log.Information("SQL cache table ensured: {Schema}.{Table}", schema, table);
    }
    catch (Exception ex)
    {
        // Μη-θανατηφόρο: αν αποτύχει, το CacheService κάνει graceful degradation.
        Log.Warning(ex, "Could not ensure SQL cache table {Schema}.{Table}.", schema, table);
    }
}

async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
{
    string[] roles = { AppRoles.Admin, AppRoles.SuperUser, AppRoles.User, AppRoles.TenantAdmin };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

async Task CreateAdminUserAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
{
    // Read from configuration (User Secrets in dev, env vars in prod)
    var adminEmail = configuration["AdminSeed:Email"] ?? "admin.demo@incis.local";
    var adminPassword = configuration["AdminSeed:Password"];

    // Ασφαλής παράλειψη: κενό ή μη-αντικατεστημένο placeholder => δεν δημιουργείται admin.
    // Σε production όρισε το password μέσω User Secrets ή env var 'AdminSeed__Password'.
    if (string.IsNullOrWhiteSpace(adminPassword)
        || adminPassword.StartsWith("REPLACE_WITH", StringComparison.OrdinalIgnoreCase))
    {
        Log.Warning("AdminSeed:Password δεν έχει οριστεί. Παράλειψη admin seed. " +
                    "Όρισέ το μέσω User Secrets ή env var 'AdminSeed__Password' για να δημιουργηθεί ο αρχικός διαχειριστής.");
        return;
    }

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "Administrator",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            MustChangePassword = false
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (!createResult.Succeeded)
        {
            Log.Warning("Failed to create admin user: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }
        Log.Information("Admin user created: {Email}", adminEmail);
    }

    // Ensure admin role assignment
    if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
    {
        await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
    }
}

// ============================================================
// CUSTOM PASSWORD VALIDATOR
// ============================================================
/// <summary>
/// Custom password validator for additional security checks
/// </summary>
public class CustomPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : class
{
    public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string? password)
    {
        var errors = new List<IdentityError>();

        if (password == null)
        {
            errors.Add(new IdentityError { Description = "Password cannot be null" });
            return Task.FromResult(IdentityResult.Failed(errors.ToArray()));
        }

        // Check for common weak passwords
        var commonPasswords = new[] { "Password123!", "Admin@123!", "Welcome1!", "Qwerty123!", "1234567890" };
        if (commonPasswords.Any(p => password.Contains(p, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add(new IdentityError { Description = "Password is too common. Choose a stronger password." });
        }

        // Check for sequential characters
        if (HasSequentialChars(password))
        {
            errors.Add(new IdentityError { Description = "Password cannot contain sequential characters." });
        }

        // Check for repeated characters (more than 3)
        if (HasRepeatedChars(password))
        {
            errors.Add(new IdentityError { Description = "Password cannot contain more than 3 repeated characters." });
        }

        return errors.Count == 0
            ? Task.FromResult(IdentityResult.Success)
            : Task.FromResult(IdentityResult.Failed(errors.ToArray()));
    }

    private static bool HasSequentialChars(string password)
    {
        var sequences = new[] { "123", "234", "345", "456", "567", "678", "789", "890", "abc", "bcd", "cde", "def", "qwe", "wer", "rty" };
        var lower = password.ToLower();
        return sequences.Any(s => lower.Contains(s));
    }

    private static bool HasRepeatedChars(string password)
    {
        for (int i = 0; i < password.Length - 3; i++)
        {
            if (password[i] == password[i + 1] && password[i + 1] == password[i + 2] && password[i + 2] == password[i + 3])
                return true;
        }
        return false;
    }
}

// ============================================================
// SECURITY HEADERS MIDDLEWARE
// ============================================================
/// <summary>
/// Security header middleware extensions
/// </summary>
public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            // Content Security Policy - allow CDN resources
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://code.jquery.com; " +
                "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://fonts.googleapis.com; " +
                "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                "img-src 'self' data: blob:; " +
                "connect-src 'self' wss:; " +
                "frame-ancestors 'none'";

            await next();
        });
    }
}

// Make Program accessible to integration tests
public partial class Program { }
