namespace IWOMR.Domain.Entities;

// ─── Enums ────────────────────────────────────────────────────────────────────

public enum SkillType     { Offer, Seek }
public enum ExchangeStatus { Pending, Accepted, Completed, Cancelled }

// ─── User ─────────────────────────────────────────────────────────────────────

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string? Bio { get; private set; }
    public string? AvatarKey { get; private set; }   // MinIO object key
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public decimal ReputationScore { get; private set; } = 0m;
    public int ExchangesCount { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public ICollection<UserSkill> Skills { get; private set; } = new List<UserSkill>();
    public ICollection<Exchange> ExchangesAsA { get; private set; } = new List<Exchange>();
    public ICollection<Exchange> ExchangesAsB { get; private set; } = new List<Exchange>();
    public ICollection<Review> ReviewsReceived { get; private set; } = new List<Review>();

    private User() { }

    public static User Create(string username, string email, string passwordHash) => new()
    {
        Username     = username.Trim().ToLowerInvariant(),
        Email        = email.Trim().ToLowerInvariant(),
        PasswordHash = passwordHash
    };

    public void UpdateProfile(string? bio, double? latitude, double? longitude)
    {
        Bio       = bio;
        Latitude  = latitude;
        Longitude = longitude;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetAvatar(string minioKey)
    {
        AvatarKey = minioKey;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Bayesian average: (v*n + m*C) / (n + C)
    /// Prevents a new user with 1 perfect review beating a veteran with 50.
    /// </summary>
    public void RecalculateReputation(IEnumerable<Review> allReviews)
    {
        const decimal C = 10m;   // smoothing constant
        const decimal m = 3m;    // global prior mean

        var reviews = allReviews.ToList();
        if (reviews.Count == 0) { ReputationScore = 0m; return; }

        decimal n = reviews.Count;
        decimal v = (decimal)reviews.Average(r => r.Score);
        ReputationScore = Math.Round((v * n + m * C) / (n + C), 2);
        ExchangesCount  = reviews.Count;
        UpdatedAt       = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive  = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

// ─── SkillCategory ────────────────────────────────────────────────────────────

public class SkillCategory
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Icon { get; private set; }

    public ICollection<UserSkill> UserSkills { get; private set; } = new List<UserSkill>();

    private SkillCategory() { }
}

// ─── UserSkill ────────────────────────────────────────────────────────────────

public class UserSkill
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public SkillType Type { get; private set; }       // Offer | Seek
    public int Level { get; private set; }             // 1–5
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public User User { get; private set; } = default!;
    public SkillCategory Category { get; private set; } = default!;

    private UserSkill() { }

    public static UserSkill Create(Guid userId, Guid categoryId, SkillType type, int level, string? description)
    {
        if (level is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(level), "Level must be between 1 and 5.");

        return new UserSkill
        {
            UserId      = userId,
            CategoryId  = categoryId,
            Type        = type,
            Level       = level,
            Description = description?.Trim()
        };
    }
}

// ─── Exchange ─────────────────────────────────────────────────────────────────

public class Exchange
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserAId { get; private set; }
    public Guid UserBId { get; private set; }
    public string SkillA { get; private set; } = default!;   // what A offers
    public string SkillB { get; private set; } = default!;   // what B offers
    public ExchangeStatus Status { get; private set; } = ExchangeStatus.Pending;
    public DateTimeOffset ProposedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public User UserA { get; private set; } = default!;
    public User UserB { get; private set; } = default!;
    public ICollection<Message> Messages { get; private set; } = new List<Message>();
    public ICollection<Review> Reviews { get; private set; } = new List<Review>();

    private Exchange() { }

    public static Exchange Create(Guid userAId, Guid userBId, string skillA, string skillB)
    {
        if (userAId == userBId)
            throw new InvalidOperationException("A user cannot exchange with themselves.");

        return new Exchange
        {
            UserAId = userAId,
            UserBId = userBId,
            SkillA  = skillA.Trim(),
            SkillB  = skillB.Trim()
        };
    }

    public void Accept()
    {
        if (Status != ExchangeStatus.Pending)
            throw new InvalidOperationException($"Cannot accept exchange in status {Status}.");
        Status     = ExchangeStatus.Accepted;
        AcceptedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        if (Status != ExchangeStatus.Accepted)
            throw new InvalidOperationException($"Cannot complete exchange in status {Status}.");
        Status      = ExchangeStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == ExchangeStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed exchange.");
        Status = ExchangeStatus.Cancelled;
    }
}

// ─── Message ──────────────────────────────────────────────────────────────────

public class Message
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ExchangeId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Body { get; private set; } = default!;
    public DateTimeOffset SentAt { get; private set; } = DateTimeOffset.UtcNow;

    public Exchange Exchange { get; private set; } = default!;
    public User Sender { get; private set; } = default!;

    private Message() { }

    public static Message Create(Guid exchangeId, Guid senderId, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Message body cannot be empty.", nameof(body));

        return new Message { ExchangeId = exchangeId, SenderId = senderId, Body = body.Trim() };
    }
}

// ─── Review ───────────────────────────────────────────────────────────────────

public class Review
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ExchangeId { get; private set; }
    public Guid ReviewerId { get; private set; }
    public Guid RevieweeId { get; private set; }
    public int Score { get; private set; }       // 1–5
    public string? Comment { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public Exchange Exchange { get; private set; } = default!;
    public User Reviewer { get; private set; } = default!;
    public User Reviewee { get; private set; } = default!;

    private Review() { }

    public static Review Create(Guid exchangeId, Guid reviewerId, Guid revieweeId, int score, string? comment)
    {
        if (score is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 1 and 5.");

        return new Review
        {
            ExchangeId = exchangeId,
            ReviewerId = reviewerId,
            RevieweeId = revieweeId,
            Score      = score,
            Comment    = comment?.Trim()
        };
    }
}
