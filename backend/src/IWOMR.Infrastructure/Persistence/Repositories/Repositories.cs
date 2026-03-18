using Microsoft.EntityFrameworkCore;
using IWOMR.Application.Common.Interfaces;
using IWOMR.Domain.Entities;

namespace IWOMR.Infrastructure.Persistence.Repositories;

// ── UserRepository ────────────────────────────────────────────────────────────

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.Include(u => u.Skills).ThenInclude(s => s.Category)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await db.Users.AddAsync(user, ct);

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        return Task.CompletedTask;
    }
}

// ── SkillRepository ───────────────────────────────────────────────────────────

public class SkillRepository(AppDbContext db) : ISkillRepository
{
    public async Task<IReadOnlyList<SkillCategory>> GetCategoriesAsync(CancellationToken ct = default) =>
        await db.SkillCategories.OrderBy(c => c.Name).ToListAsync(ct);

    public Task<SkillCategory?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default) =>
        db.SkillCategories.FindAsync([id], ct).AsTask();

    public async Task AddUserSkillAsync(UserSkill skill, CancellationToken ct = default) =>
        await db.UserSkills.AddAsync(skill, ct);

    public async Task DeleteUserSkillAsync(Guid skillId, Guid userId, CancellationToken ct = default)
    {
        var skill = await db.UserSkills
            .FirstOrDefaultAsync(s => s.Id == skillId && s.UserId == userId, ct);
        if (skill is not null) db.UserSkills.Remove(skill);
    }

    public async Task<IReadOnlyList<UserSkill>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await db.UserSkills.Include(s => s.Category)
            .Where(s => s.UserId == userId).ToListAsync(ct);
}

// ── ExchangeRepository ────────────────────────────────────────────────────────

public class ExchangeRepository(AppDbContext db) : IExchangeRepository
{
    public Task<Exchange?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Exchanges
            .Include(e => e.UserA)
            .Include(e => e.UserB)
            .Include(e => e.Messages.OrderBy(m => m.SentAt))
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Exchange>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await db.Exchanges
            .Include(e => e.UserA)
            .Include(e => e.UserB)
            .Where(e => e.UserAId == userId || e.UserBId == userId)
            .OrderByDescending(e => e.ProposedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Exchange exchange, CancellationToken ct = default) =>
        await db.Exchanges.AddAsync(exchange, ct);

    public Task UpdateAsync(Exchange exchange, CancellationToken ct = default)
    {
        db.Exchanges.Update(exchange);
        return Task.CompletedTask;
    }
}

// ── MessageRepository ─────────────────────────────────────────────────────────

public class MessageRepository(AppDbContext db) : IMessageRepository
{
    public async Task<IReadOnlyList<Message>> GetByExchangeIdAsync(
        Guid exchangeId, int limit, CancellationToken ct = default) =>
        await db.Messages
            .Where(m => m.ExchangeId == exchangeId)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .OrderBy(m => m.SentAt)   // re-order ascending after limit
            .ToListAsync(ct);

    public async Task AddAsync(Message message, CancellationToken ct = default) =>
        await db.Messages.AddAsync(message, ct);
}

// ── ReviewRepository ──────────────────────────────────────────────────────────

public class ReviewRepository(AppDbContext db) : IReviewRepository
{
    public Task<bool> ExistsAsync(Guid exchangeId, Guid reviewerId, CancellationToken ct = default) =>
        db.Reviews.AnyAsync(r => r.ExchangeId == exchangeId && r.ReviewerId == reviewerId, ct);

    public async Task AddAsync(Review review, CancellationToken ct = default) =>
        await db.Reviews.AddAsync(review, ct);

    public async Task<IReadOnlyList<Review>> GetByRevieweeIdAsync(Guid revieweeId, CancellationToken ct = default) =>
        await db.Reviews.Where(r => r.RevieweeId == revieweeId).ToListAsync(ct);
}

// ── UnitOfWork ────────────────────────────────────────────────────────────────

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
