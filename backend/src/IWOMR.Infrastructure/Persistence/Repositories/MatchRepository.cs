using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;
using IWOMR.Application.Common;
using IWOMR.Application.Common.Interfaces;

namespace IWOMR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Uses raw Dapper + PostGIS for the geo-matching query.
/// EF Core's spatial support works, but Dapper gives us full control
/// over the scoring SQL without fighting the query translator.
/// </summary>
public class MatchRepository(IConfiguration config) : IMatchRepository
{
    private readonly string _connectionString =
        config.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' not configured.");

    public async Task<IReadOnlyList<MatchResult>> FindNearbyAsync(
        GeoPoint origin,
        Guid categoryId,
        double radiusKm,
        int limit,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                u.id              AS UserId,
                u.username        AS Username,
                u.avatar_key      AS AvatarKey,
                u.bio             AS Bio,
                u.reputation_score AS ReputationScore,
                u.exchanges_count  AS ExchangesCount,
                ROUND(
                    ST_Distance(
                        ST_SetSRID(ST_MakePoint(u.longitude, u.latitude), 4326)::geography,
                        ST_SetSRID(ST_MakePoint(@Lng, @Lat), 4326)::geography
                    ) / 1000.0, 2
                )                  AS DistanceKm,
                us.description     AS SkillDescription,
                us.level           AS SkillLevel,
                -- Combined score: 50% reputation (0-5 scale), 50% inverse distance
                -- Both components normalised to [0,1] before weighting
                (
                    (u.reputation_score / 5.0) * 0.5 +
                    (1.0 - LEAST(
                        ST_Distance(
                            ST_SetSRID(ST_MakePoint(u.longitude, u.latitude), 4326)::geography,
                            ST_SetSRID(ST_MakePoint(@Lng, @Lat), 4326)::geography
                        ) / (@RadiusM), 1.0
                    )) * 0.5
                ) AS CombinedScore
            FROM users u
            INNER JOIN user_skills us
                ON us.user_id = u.id
                AND us.type = 'Offer'
                AND us.category_id = @CategoryId
            WHERE
                u.is_active = true
                AND u.latitude IS NOT NULL
                AND u.longitude IS NOT NULL
                AND ST_DWithin(
                    ST_SetSRID(ST_MakePoint(u.longitude, u.latitude), 4326)::geography,
                    ST_SetSRID(ST_MakePoint(@Lng, @Lat), 4326)::geography,
                    @RadiusM
                )
            ORDER BY CombinedScore DESC
            LIMIT @Limit;
            """;

        await using var conn = new NpgsqlConnection(_connectionString);

        var rows = await conn.QueryAsync<MatchResultRow>(
            new CommandDefinition(sql, new
            {
                Lat        = origin.Latitude,
                Lng        = origin.Longitude,
                CategoryId = categoryId,
                RadiusM    = radiusKm * 1000,
                Limit      = limit
            }, cancellationToken: ct));

        return rows.Select(r => new MatchResult(
            r.UserId,
            r.Username,
            r.AvatarKey,
            r.Bio,
            r.ReputationScore,
            r.ExchangesCount,
            r.DistanceKm,
            r.SkillDescription ?? "",
            r.SkillLevel
        )).ToList();
    }

    // Dapper projection — flat row matching the SELECT columns
    private record MatchResultRow(
        Guid UserId,
        string Username,
        string? AvatarKey,
        string? Bio,
        decimal ReputationScore,
        int ExchangesCount,
        double DistanceKm,
        string? SkillDescription,
        int SkillLevel,
        double CombinedScore);
}
