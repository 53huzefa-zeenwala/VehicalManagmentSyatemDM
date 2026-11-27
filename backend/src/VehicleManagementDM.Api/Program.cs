using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.BackgroundJobs;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Common.Database;
using VehicleManagementDM.Api.Common.Errors;
using VehicleManagementDM.Api.Common.Logging;
using VehicleManagementDM.Api.Common.Storage;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Data.Interceptors;
using VehicleManagementDM.Api.Features.Accounts;
using VehicleManagementDM.Api.Features.Auth;
using VehicleManagementDM.Api.Features.Lots;
using VehicleManagementDM.Api.Features.Notifications;
using VehicleManagementDM.Api.Features.Reports;
using VehicleManagementDM.Api.Features.Transfers;
using VehicleManagementDM.Api.Features.Trips;
using VehicleManagementDM.Api.Features.Vehicles;

const long DefaultMaxRequestBodyBytes = 1024 * 1024; // 1 MB; photo uploads raise it per endpoint (ARCHITECTURE.md §11)

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = DefaultMaxRequestBodyBytes);
services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = DefaultMaxRequestBodyBytes);

// Options
services.Configure<ErrorOptions>(configuration.GetSection(ErrorOptions.SectionName));
services.Configure<ErrorLoggingOptions>(configuration.GetSection(ErrorLoggingOptions.SectionName));
services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

// Core services
services.AddSingleton(TimeProvider.System);
services.AddSingleton<DatabaseReadiness>();
services.AddSingleton<FileErrorLog>();
services.AddSingleton<ErrorLogWriter>();
services.AddSingleton<ApiErrorWriter>();
services.AddSingleton<FileStore>();
services.AddHttpContextAccessor();

// Database (D-029, D-030)
services.AddSingleton<TimestampInterceptor>();
services.AddSingleton<AuditInterceptor>();
services.AddDbContext<AppDbContext>((provider, options) => options
    .UseSqlServer(configuration.GetConnectionString("Default"))
    .AddInterceptors(provider.GetRequiredService<TimestampInterceptor>(), provider.GetRequiredService<AuditInterceptor>()));
services.AddHostedService<DatabaseInitializer>();
services.AddHostedService<ErrorLogRetentionJob>();
services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

// Errors: one JSON format for everything (D-032)
services.AddExceptionHandler<GlobalExceptionHandler>();
services.AddProblemDetails();

// Authentication: cookie sessions (D-031)
services.AddAppAuth();

// Controllers and JSON conventions (ARCHITECTURE.md §7)
services
    .AddControllers(options => options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressMapClientErrors = true;
    options.InvalidModelStateResponseFactory = ValidationErrors.Respond;
});

// Features
services.AddScoped<AuthService>();
services.AddScoped<AccountService>();
services.AddScoped<LotService>();
services.AddScoped<VehicleService>();
services.AddScoped<VehicleHistoryService>();
services.AddScoped<TripService>();
services.AddScoped<TripQueries>();
services.AddScoped<TransferService>();
services.AddScoped<NotificationService>();
services.AddScoped<ReportService>();
services.AddSingleton<IPushSender, NoOpPushSender>(); // push off until a Firebase project exists (D-039)
services.AddHostedService<ReminderJob>();

var swaggerEnabled = configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();
}

var app = builder.Build();

if (string.IsNullOrWhiteSpace(configuration["DataProtection:KeysPath"]) && !app.Environment.IsDevelopment())
{
    app.Logger.LogCritical("DataProtection:KeysPath is not set: session cookies will stop working whenever the app restarts");
}

app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    var httpContext = context.HttpContext;
    await httpContext.RequestServices.GetRequiredService<ApiErrorWriter>()
        .WriteAsync(httpContext, StatusCodeErrors.For(httpContext));
});
app.UseMiddleware<DatabaseReadyMiddleware>();

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Secure by default: every controller endpoint needs a signed-in user unless it says [AllowAnonymous].
app.MapControllers().RequireAuthorization();
app.MapHealthChecks("/health", new() { ResponseWriter = DatabaseHealthCheck.WriteResponseAsync }).AllowAnonymous();

app.Run();

/// <summary>Entry point; public so integration tests can host the API.</summary>
public partial class Program;
