using IWOMR.Domain.Entities;
using IWOMR.Application.Common;

namespace IWOMR.Application.Common.Interfaces;

// ── Persistence ──────────────────────────────────────────────────────────────

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}

public interface ISkillRepository
{
    Task<IReadOnlyList<SkillCategory>> GetCategoriesAsync(CancellationToken ct = default);
    Task<SkillCategory?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default);
    Task AddUserSkillAsync(UserSkill skill, CancellationToken ct = default);
    Task DeleteUserSkillAsync(Guid skillId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserSkill>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}

public interface IMatchRepository
{
    /// <summary>
    /// Returns users within radiusKm who offer the requested category,
    /// ordered by combined score (reputation + distance).
    /// Uses PostGIS ST_DWithin under the hood.
    /// </summary>
    Task<IReadOnlyList<MatchResult>> FindNearbyAsync(
        GeoPoint origin,
        Guid categoryId,
        double radiusKm,
        int limit,
        CancellationToken ct = default);
}

public interface IExchangeRepository
{
    Task<Exchange?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Exchange>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Exchange exchange, CancellationToken ct = default);
    Task UpdateAsync(Exchange exchange, CancellationToken ct = default);
}

public interface IMessageRepository
{
    Task<IReadOnlyList<Message>> GetByExchangeIdAsync(Guid exchangeId, int limit, CancellationToken ct = default);
    Task AddAsync(Message message, CancellationToken ct = default);
}

public interface IReviewRepository
{
    Task<bool> ExistsAsync(Guid exchangeId, Guid reviewerId, CancellationToken ct = default);
    Task AddAsync(Review review, CancellationToken ct = default);
    Task<IReadOnlyList<Review>> GetByRevieweeIdAsync(Guid revieweeId, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// ── Storage ───────────────────────────────────────────────────────────────────

public interface IStorageService
{
    /// <summary>Upload a file and return its object key.</summary>
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>Generate a pre-signed URL valid for the given duration.</summary>
    Task<string> GetUrlAsync(string objectKey, TimeSpan? expiry = null, CancellationToken ct = default);

    Task DeleteAsync(string objectKey, CancellationToken ct = default);
}

// ── Notifications ─────────────────────────────────────────────────────────────

public interface INotificationService
{
    Task SendAsync(Guid userId, string title, string message, CancellationToken ct = default);
}

// ── Auth ──────────────────────────────────────────────────────────────────────

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? ValidateRefreshToken(string token);
}

public interface IPasswordService
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string hash);
}

// ── DTOs returned by repositories (not domain entities) ──────────────────────

public record MatchResult(
    Guid UserId,
    string Username,
    string? AvatarKey,
    string? Bio,
    decimal ReputationScore,
    int ExchangesCount,
    double DistanceKm,
    string SkillDescription,
    int SkillLevel);
