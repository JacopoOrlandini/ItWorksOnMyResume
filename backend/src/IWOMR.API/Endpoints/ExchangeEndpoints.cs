using System.Security.Claims;
using IWOMR.Application.Common.Interfaces;
using IWOMR.Domain.Entities;

namespace IWOMR.API.Endpoints;

public static class ExchangeEndpoints
{
    public static void MapExchangeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/exchanges").WithTags("Exchanges").RequireAuthorization();

        group.MapGet("/",              GetMyExchanges);
        group.MapGet("/{id}",          GetById);
        group.MapPost("/",             Propose);
        group.MapPut("/{id}/accept",   Accept);
        group.MapPut("/{id}/complete", Complete);
        group.MapPut("/{id}/cancel",   Cancel);
        group.MapPost("/{id}/reviews", PostReview);
    }

    // ── GET /api/exchanges ────────────────────────────────────────────────────
    private static async Task<IResult> GetMyExchanges(
        ClaimsPrincipal principal,
        IExchangeRepository exchanges,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        var list   = await exchanges.GetByUserIdAsync(userId, ct);
        return Results.Ok(list.Select(MapExchange));
    }

    // ── GET /api/exchanges/{id} ───────────────────────────────────────────────
    private static async Task<IResult> GetById(
        Guid id,
        ClaimsPrincipal principal,
        IExchangeRepository exchanges,
        CancellationToken ct)
    {
        var userId   = GetUserId(principal);
        var exchange = await exchanges.GetByIdAsync(id, ct);

        if (exchange is null) return Results.NotFound();
        if (exchange.UserAId != userId && exchange.UserBId != userId)
            return Results.Forbid();

        return Results.Ok(MapExchange(exchange));
    }

    // ── POST /api/exchanges ───────────────────────────────────────────────────
    record ProposeRequest(Guid TargetUserId, string MySkill, string TheirSkill);

    private static async Task<IResult> Propose(
        ProposeRequest req,
        ClaimsPrincipal principal,
        IExchangeRepository exchanges,
        IUserRepository users,
        INotificationService notify,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (userId == req.TargetUserId)
            return Results.BadRequest("Cannot propose an exchange with yourself.");

        if (await users.GetByIdAsync(req.TargetUserId, ct) is null)
            return Results.NotFound("Target user not found.");

        Exchange exchange;
        try { exchange = Exchange.Create(userId, req.TargetUserId, req.MySkill, req.TheirSkill); }
        catch (Exception ex) { return Results.BadRequest(ex.Message); }

        await exchanges.AddAsync(exchange, ct);
        await uow.SaveChangesAsync(ct);

        // Push notification to target user
        await notify.SendAsync(req.TargetUserId,
            "New exchange proposal",
            "Someone wants to exchange skills with you!", ct);

        return Results.Created($"/api/exchanges/{exchange.Id}", MapExchange(exchange));
    }

    // ── PUT /api/exchanges/{id}/accept ────────────────────────────────────────
    private static async Task<IResult> Accept(
        Guid id,
        ClaimsPrincipal principal,
        IExchangeRepository exchanges,
        INotificationService notify,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        var userId   = GetUserId(principal);
        var exchange = await exchanges.GetByIdAsync(id, ct);

        if (exchange is null) return Results.NotFound();
        if (exchange.UserBId != userId) return Results.Forbid();   // only B can accept

        try { exchange.Accept(); }
        catch (InvalidOperationException ex) { return Results.BadRequest(ex.Message); }

        await uow.SaveChangesAsync(ct);

        await notify.SendAsync(exchange.UserAId,
            "Exchange accepted!",
            "Your exchange proposal was accepted. Start chatting!", ct);

        return Results.Ok(MapExchange(exchange));
    }

    // ── PUT /api/exchanges/{id}/complete ──────────────────────────────────────
    private static async Task<IResult> Complete(
        Guid id,
        ClaimsPrincipal principal,
        IExchangeRepository exchanges,
        IUserRepository users,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        var userId   = GetUserId(principal);
        var exchange = await exchanges.GetByIdAsync(id, ct);

        if (exchange is null) return Results.NotFound();
        if (exchange.UserAId != userId && exchange.UserBId != userId)
            return Results.Forbid();

        try { exchange.Complete(); }
        catch (InvalidOperationException ex) { return Results.BadRequest(ex.Message); }

        await uow.SaveChangesAsync(ct);

        return Results.Ok(MapExchange(exchange));
    }

    // ── PUT /api/exchanges/{id}/cancel ────────────────────────────────────────
    private static async Task<IResult> Cancel(
        Guid id,
        ClaimsPrincipal principal,
        IExchangeRepository exchanges,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        var userId   = GetUserId(principal);
        var exchange = await exchanges.GetByIdAsync(id, ct);

        if (exchange is null) return Results.NotFound();
        if (exchange.UserAId != userId && exchange.UserBId != userId)
            return Results.Forbid();

        try { exchange.Cancel(); }
        catch (InvalidOperationException ex) { return Results.BadRequest(ex.Message); }

        await uow.SaveChangesAsync(ct);
        return Results.Ok(MapExchange(exchange));
    }

    // ── POST /api/exchanges/{id}/reviews ──────────────────────────────────────
    record ReviewRequest(int Score, string? Comment);

    private static async Task<IResult> PostReview(
        Guid id,
        ReviewRequest req,
        ClaimsPrincipal principal,
        IExchangeRepository exchanges,
        IReviewRepository reviews,
        IUserRepository users,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        var userId   = GetUserId(principal);
        var exchange = await exchanges.GetByIdAsync(id, ct);

        if (exchange is null) return Results.NotFound();
        if (exchange.Status != ExchangeStatus.Completed)
            return Results.BadRequest("Can only review a completed exchange.");
        if (exchange.UserAId != userId && exchange.UserBId != userId)
            return Results.Forbid();
        if (await reviews.ExistsAsync(id, userId, ct))
            return Results.Conflict("You already reviewed this exchange.");

        var revieweeId = exchange.UserAId == userId ? exchange.UserBId : exchange.UserAId;

        Review review;
        try { review = Review.Create(id, userId, revieweeId, req.Score, req.Comment); }
        catch (ArgumentOutOfRangeException ex) { return Results.BadRequest(ex.Message); }

        await reviews.AddAsync(review, ct);

        // Recalculate Bayesian reputation for reviewee
        var reviewee    = await users.GetByIdAsync(revieweeId, ct);
        var allReviews  = await reviews.GetByRevieweeIdAsync(revieweeId, ct);
        reviewee?.RecalculateReputation(allReviews);

        await uow.SaveChangesAsync(ct);

        return Results.Created($"/api/exchanges/{id}/reviews", new { reviewId = review.Id });
    }

    private static object MapExchange(Exchange e) => new
    {
        id          = e.Id,
        userA       = new { id = e.UserAId, username = e.UserA?.Username },
        userB       = new { id = e.UserBId, username = e.UserB?.Username },
        skillA      = e.SkillA,
        skillB      = e.SkillB,
        status      = e.Status.ToString(),
        proposedAt  = e.ProposedAt,
        acceptedAt  = e.AcceptedAt,
        completedAt = e.CompletedAt,
        messages    = e.Messages.Select(m => new
        {
            id       = m.Id,
            senderId = m.SenderId,
            body     = m.Body,
            sentAt   = m.SentAt
        })
    };

    private static Guid GetUserId(ClaimsPrincipal p) =>
        Guid.Parse(p.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? p.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim missing."));
}
