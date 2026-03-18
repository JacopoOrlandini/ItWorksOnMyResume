using IWOMR.Application.Common.Interfaces;
using IWOMR.Domain.Entities;

namespace IWOMR.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", Register).AllowAnonymous();
        group.MapPost("/login",    Login).AllowAnonymous();
    }

    // ── POST /api/auth/register ───────────────────────────────────────────────
    record RegisterRequest(string Username, string Email, string Password);

    private static async Task<IResult> Register(
        RegisterRequest req,
        IUserRepository users,
        IPasswordService passwords,
        ITokenService tokens,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length < 3)
            return Results.BadRequest("Username must be at least 3 characters.");

        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8)
            return Results.BadRequest("Password must be at least 8 characters.");

        if (await users.GetByEmailAsync(req.Email, ct) is not null)
            return Results.Conflict("Email already registered.");

        if (await users.GetByUsernameAsync(req.Username, ct) is not null)
            return Results.Conflict("Username already taken.");

        var hash = passwords.Hash(req.Password);
        var user = User.Create(req.Username, req.Email, hash);

        await users.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            accessToken = tokens.GenerateAccessToken(user),
            user        = MapUser(user)
        });
    }

    // ── POST /api/auth/login ──────────────────────────────────────────────────
    record LoginRequest(string Email, string Password);

    private static async Task<IResult> Login(
        LoginRequest req,
        IUserRepository users,
        IPasswordService passwords,
        ITokenService tokens,
        CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(req.Email, ct);

        if (user is null || !passwords.Verify(req.Password, user.PasswordHash))
            return Results.Unauthorized();

        if (!user.IsActive)
            return Results.Forbid();

        return Results.Ok(new
        {
            accessToken = tokens.GenerateAccessToken(user),
            user        = MapUser(user)
        });
    }

    internal static object MapUser(User u) => new
    {
        id             = u.Id,
        username       = u.Username,
        email          = u.Email,
        bio            = u.Bio,
        avatarKey      = u.AvatarKey,
        reputationScore = u.ReputationScore,
        exchangesCount = u.ExchangesCount,
        createdAt      = u.CreatedAt
    };
}
