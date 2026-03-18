using System.Security.Claims;
using IWOMR.Application.Common;
using IWOMR.Application.Common.Interfaces;

namespace IWOMR.API.Endpoints;

public static class MatchEndpoints
{
    public static void MapMatchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/matches").WithTags("Matches").RequireAuthorization();

        group.MapGet("/nearby", GetNearby);
    }

    // ── GET /api/matches/nearby?lat=&lng=&categoryId=&radiusKm=&limit= ────────
    private static async Task<IResult> GetNearby(
        double lat,
        double lng,
        Guid categoryId,
        IMatchRepository matches,
        IStorageService storage,
        double radiusKm = 10,
        int limit       = 20,
        CancellationToken ct = default)
    {
        if (lat is < -90 or > 90 || lng is < -180 or > 180)
            return Results.BadRequest("Invalid coordinates.");

        if (radiusKm is <= 0 or > 100)
            return Results.BadRequest("radiusKm must be between 1 and 100.");

        limit = Math.Clamp(limit, 1, 50);

        var origin  = new GeoPoint(lat, lng);
        var results = await matches.FindNearbyAsync(origin, categoryId, radiusKm, limit, ct);

        // Resolve avatar URLs from MinIO keys
        var items = new List<object>(results.Count);
        foreach (var r in results)
        {
            var avatarUrl = r.AvatarKey is not null
                ? await storage.GetUrlAsync(r.AvatarKey, ct: ct)
                : null;

            items.Add(new
            {
                userId         = r.UserId,
                username       = r.Username,
                avatarUrl,
                bio            = r.Bio,
                reputationScore = r.ReputationScore,
                exchangesCount = r.ExchangesCount,
                distanceKm     = r.DistanceKm,
                skill          = new
                {
                    description = r.SkillDescription,
                    level       = r.SkillLevel
                }
            });
        }

        return Results.Ok(new { results = items, count = items.Count });
    }
}
