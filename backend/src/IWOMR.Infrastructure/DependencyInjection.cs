using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using IWOMR.Application.Common.Interfaces;
using IWOMR.Infrastructure.Auth;
using IWOMR.Infrastructure.Notifications;
using IWOMR.Infrastructure.Persistence;
using IWOMR.Infrastructure.Persistence.Repositories;
using IWOMR.Infrastructure.Storage;

namespace IWOMR.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        // ── Database ──────────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(config.GetConnectionString("Default"),
                npg => npg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<IUserRepository,     UserRepository>();
        services.AddScoped<ISkillRepository,    SkillRepository>();
        services.AddScoped<IMatchRepository,    MatchRepository>();
        services.AddScoped<IExchangeRepository, ExchangeRepository>();
        services.AddScoped<IMessageRepository,  MessageRepository>();
        services.AddScoped<IReviewRepository,   ReviewRepository>();
        services.AddScoped<IUnitOfWork,         UnitOfWork>();

        // ── MinIO ─────────────────────────────────────────────────────────────
        services.AddMinio(c => c
            .WithEndpoint(config["MinIO:Endpoint"] ?? "localhost:9000")
            .WithCredentials(
                config["MinIO:AccessKey"] ?? "",
                config["MinIO:SecretKey"] ?? "")
            .WithSSL(false)
            .Build());
        services.AddScoped<IStorageService, MinioStorageService>();

        // ── ntfy ──────────────────────────────────────────────────────────────
        services.AddHttpClient<INotificationService, NtfyNotificationService>();

        // ── Auth ──────────────────────────────────────────────────────────────
        services.AddScoped<ITokenService,    JwtTokenService>();
        services.AddScoped<IPasswordService, PasswordService>();

        return services;
    }
}
