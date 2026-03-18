using System.Security.Claims;
using IWOMR.Application.Common.Interfaces;

namespace IWOMR.API.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization();

        group.MapGet("/me",            GetMe);
        group.MapPut("/me",            UpdateProfile);
        group.MapPut("/me/location",   UpdateLocation);
        group.MapPost("/me/avatar",    UploadAvatar).DisableAntiforgery();
        group.MapGet("/{username}",    GetByUsername).AllowAnonymous();
    }

    // ── GET /api/users/me ─────────────────────────────────────────────────────
    private static async Task<IResult> GetMe(
        ClaimsPrincipal principal,
        IUserRepository users,
        IStorageService storage,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        var user   = await users.GetByIdAsync(userId, ct);
        if (user is null) return Results.NotFound();

        return Results.Ok(AuthEndpoints.MapUser(user));
    }

    // ── PUT /api/users/me ─────────────────────────────────────────────────────
    record UpdateProfileRequest(string? Bio);

    private static async Task<IResult> UpdateProfile(
        UpdateProfileRequest req,
        ClaimsPrincipal principal,
        IUserRepository users,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        var user   = await users.GetByIdAsync(userId, ct);
        if (user is null) return Results.NotFound();

        user.UpdateProfile(req.Bio, user.Latitude, user.Longitude);
        await uow.SaveChangesAsync(ct);

        return Results.Ok(AuthEndpoints.MapUser(user));
    }

    // ── PUT /api/users/me/location ────────────────────────────────────────────
    record LocationRequest(double Latitude, double Longitude);

    private static async Task<IResult> UpdateLocation(
        LocationRequest req,
        ClaimsPrincipal principal,
        IUserRepository users,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        if (req.Latitude is < -90 or > 90 || req.Longitude is < -180 or > 180)
            return Results.BadRequest("Invalid coordinates.");

        var userId = GetUserId(principal);
        var user   = await users.GetByIdAsync(userId, ct);
        if (user is null) return Results.NotFound();

        user.UpdateProfile(user.Bio, req.Latitude, req.Longitude);
        await uow.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    // ── POST /api/users/me/avatar ─────────────────────────────────────────────
    private static async Task<IResult> UploadAvatar(
        IFormFile file,
        ClaimsPrincipal principal,
        IUserRepository users,
        IStorageService storage,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        if (file.Length > 5 * 1024 * 1024)
            return Results.BadRequest("Avatar must be under 5 MB.");

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType))
            return Results.BadRequest("Only JPEG, PNG, and WebP are allowed.");

        var userId = GetUserId(principal);
        var user   = await users.GetByIdAsync(userId, ct);
        if (user is null) return Results.NotFound();

        await using var stream = file.OpenReadStream();
        var ext       = Path.GetExtension(file.FileName);
        var objectKey = await storage.UploadAsync(stream, $"avatar{ext}", file.ContentType, ct);
        var url       = await storage.GetUrlAsync(objectKey, ct: ct);

        user.SetAvatar(objectKey);
        await uow.SaveChangesAsync(ct);

        return Results.Ok(new { avatarUrl = url });
    }

    // ── GET /api/users/{username} ─────────────────────────────────────────────
    private static async Task<IResult> GetByUsername(
        string username,
        IUserRepository users,
        CancellationToken ct)
    {
        var user = await users.GetByUsernameAsync(username, ct);
        return user is null ? Results.NotFound() : Results.Ok(AuthEndpoints.MapUser(user));
    }

    private static Guid GetUserId(ClaimsPrincipal p) =>
        Guid.Parse(p.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? p.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim missing."));
}
