using Microsoft.EntityFrameworkCore;
using IWOMR.Domain.Entities;

namespace IWOMR.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User>          Users          => Set<User>();
    public DbSet<SkillCategory> SkillCategories => Set<SkillCategory>();
    public DbSet<UserSkill>     UserSkills     => Set<UserSkill>();
    public DbSet<Exchange>      Exchanges      => Set<Exchange>();
    public DbSet<Message>       Messages       => Set<Message>();
    public DbSet<Review>        Reviews        => Set<Review>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ── User ──────────────────────────────────────────────────────────────
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(x => x.Bio).HasColumnName("bio");
            e.Property(x => x.AvatarKey).HasColumnName("avatar_key");
            e.Property(x => x.Latitude).HasColumnName("latitude");
            e.Property(x => x.Longitude).HasColumnName("longitude");
            e.Property(x => x.ReputationScore).HasColumnName("reputation_score").HasPrecision(4, 2);
            e.Property(x => x.ExchangesCount).HasColumnName("exchanges_count");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });

        // ── SkillCategory ─────────────────────────────────────────────────────
        b.Entity<SkillCategory>(e =>
        {
            e.ToTable("skill_categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
            e.Property(x => x.Icon).HasColumnName("icon").HasMaxLength(50);

            e.HasIndex(x => x.Slug).IsUnique();
        });

        // ── UserSkill ─────────────────────────────────────────────────────────
        b.Entity<UserSkill>(e =>
        {
            e.ToTable("user_skills");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.CategoryId).HasColumnName("category_id");
            e.Property(x => x.Type).HasColumnName("type")
                .HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.Level).HasColumnName("level");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasOne(x => x.User)
                .WithMany(u => u.Skills)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Category)
                .WithMany(c => c.UserSkills)
                .HasForeignKey(x => x.CategoryId);
        });

        // ── Exchange ──────────────────────────────────────────────────────────
        b.Entity<Exchange>(e =>
        {
            e.ToTable("exchanges");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserAId).HasColumnName("user_a_id");
            e.Property(x => x.UserBId).HasColumnName("user_b_id");
            e.Property(x => x.SkillA).HasColumnName("skill_a").IsRequired();
            e.Property(x => x.SkillB).HasColumnName("skill_b").IsRequired();
            e.Property(x => x.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ProposedAt).HasColumnName("proposed_at");
            e.Property(x => x.AcceptedAt).HasColumnName("accepted_at");
            e.Property(x => x.CompletedAt).HasColumnName("completed_at");

            e.HasOne(x => x.UserA)
                .WithMany(u => u.ExchangesAsA)
                .HasForeignKey(x => x.UserAId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UserB)
                .WithMany(u => u.ExchangesAsB)
                .HasForeignKey(x => x.UserBId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Message ───────────────────────────────────────────────────────────
        b.Entity<Message>(e =>
        {
            e.ToTable("messages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ExchangeId).HasColumnName("exchange_id");
            e.Property(x => x.SenderId).HasColumnName("sender_id");
            e.Property(x => x.Body).HasColumnName("body").IsRequired();
            e.Property(x => x.SentAt).HasColumnName("sent_at");

            e.HasOne(x => x.Exchange)
                .WithMany(ex => ex.Messages)
                .HasForeignKey(x => x.ExchangeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Sender)
                .WithMany()
                .HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Review ────────────────────────────────────────────────────────────
        b.Entity<Review>(e =>
        {
            e.ToTable("reviews");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ExchangeId).HasColumnName("exchange_id");
            e.Property(x => x.ReviewerId).HasColumnName("reviewer_id");
            e.Property(x => x.RevieweeId).HasColumnName("reviewee_id");
            e.Property(x => x.Score).HasColumnName("score");
            e.Property(x => x.Comment).HasColumnName("comment");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            e.HasIndex(x => new { x.ExchangeId, x.ReviewerId }).IsUnique();

            e.HasOne(x => x.Exchange)
                .WithMany(ex => ex.Reviews)
                .HasForeignKey(x => x.ExchangeId);

            e.HasOne(x => x.Reviewer)
                .WithMany()
                .HasForeignKey(x => x.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Reviewee)
                .WithMany(u => u.ReviewsReceived)
                .HasForeignKey(x => x.RevieweeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
