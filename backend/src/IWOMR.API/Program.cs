using StackExchange.Redis;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using IWOMR.API.Endpoints;
using IWOMR.API.Hubs;
using IWOMR.API.Middleware;
using IWOMR.Infrastructure;

// ── Serilog bootstrap (before host build) ─────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog (full config from appsettings) ────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341"));

    // ── Infrastructure (DB, repos, MinIO, ntfy, auth services) ───────────────
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── JWT Authentication ────────────────────────────────────────────────────
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret not configured.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = "iwomr",
                ValidAudience            = "iwomr-mobile",
                IssuerSigningKey         = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecret))
            };

            // Allow JWT from SignalR query string (WebSocket can't set headers)
            opt.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var token = ctx.Request.Query["access_token"];
                    var path  = ctx.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs"))
                        ctx.Token = token;
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // ── CORS (React Native dev — restrict in production) ─────────────────────
    builder.Services.AddCors(opt => opt.AddPolicy("MobileApp", policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));

    // ── SignalR ───────────────────────────────────────────────────────────────
    builder.Services.AddSignalR()
        .AddStackExchangeRedis(
            builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");

    // ── OpenAPI (Swagger in dev) ──────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1",
        new() { Title = "ItWorksOnMyResume API", Version = "v1" }));

    // ─────────────────────────────────────────────────────────────────────────
    var app = builder.Build();
    // ─────────────────────────────────────────────────────────────────────────

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<ExceptionMiddleware>();
    app.UseCors("MobileApp");
    app.UseAuthentication();
    app.UseAuthorization();

    // ── Endpoint groups ───────────────────────────────────────────────────────
    app.MapAuthEndpoints();
    app.MapUserEndpoints();
    app.MapSkillEndpoints();
    app.MapMatchEndpoints();
    app.MapExchangeEndpoints();

    // ── SignalR hub ───────────────────────────────────────────────────────────
    app.MapHub<ChatHub>("/hubs/chat");

    // ── Health check ──────────────────────────────────────────────────────────
    app.MapGet("/health", () => Results.Ok(new { status = "ok", ts = DateTimeOffset.UtcNow }))
        .AllowAnonymous();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
