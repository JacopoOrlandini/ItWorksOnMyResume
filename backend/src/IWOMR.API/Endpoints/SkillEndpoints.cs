using System.Security.Claims;
using IWOMR.Application.Common.Interfaces;
using IWOMR.Domain.Entities;

namespace IWOMR.API.Endpoints;

public static class SkillEndpoints
{
    public static void MapSkillEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/skills").WithTags("Skills");

        group.MapGet("/categories",    GetCategories).AllowAnonymous();
        group.MapGet("/mine",          GetMySkills).RequireAuthorization();
        group.MapPost("/mine",         AddSkill).RequireAuthorization();
        group.MapDelete("/mine/{id}",  DeleteSkill).RequireAuthorization();
    }

    // ── GET /api/skills/categories ────────────────────────────────────────────
    private static async Task<IResult> GetCategories(
        ISkillRepository skills,
        CancellationToken ct)
    {
        var categories = await skills.GetCategoriesAsync(ct);
        return Results.Ok(categories.Select(c => new
        {
            id   = c.Id,
            name = c.Name,
            slug = c.Slug,
            icon = c.Icon
        }));
    }

    // ── GET /api/skills/mine ──────────────────────────────────────────────────
    private static async Task<IResult> GetMySkills(
        ClaimsPrincipal principal,
        ISkillRepository skills,
        CancellationToken ct)
    {
        var userId    = GetUserId(principal);
        var mySkills  = await skills.GetByUserIdAsync(userId, ct);
        return Results.Ok(mySkills.Select(MapSkill));
    }

    // ── POST /api/skills/mine ─────────────────────────────────────────────────
    record AddSkillRequest(Guid CategoryId, string Type, int Level, string? Description);

    private static async Task<IResult> AddSkill(
        AddSkillRequest req,
        ClaimsPrincipal principal,
        ISkillRepository skills,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        if (!Enum.TryParse<SkillType>(req.Type, ignoreCase: true, out var skillType))
            return Results.BadRequest("Type must be 'Offer' or 'Seek'.");

        if (await skills.GetCategoryByIdAsync(req.CategoryId, ct) is null)
            return Results.NotFound("Category not found.");

        var userId = GetUserId(principal);

        UserSkill skill;
        try
        {
            skill = UserSkill.Create(userId, req.CategoryId, skillType, req.Level, req.Description);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(ex.Message);
        }

        await skills.AddUserSkillAsync(skill, ct);
        await uow.SaveChangesAsync(ct);

        return Results.Created($"/api/skills/mine", MapSkill(skill));
    }

    // ── DELETE /api/skills/mine/{id} ──────────────────────────────────────────
    private static async Task<IResult> DeleteSkill(
        Guid id,
        ClaimsPrincipal principal,
        ISkillRepository skills,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        await skills.DeleteUserSkillAsync(id, userId, ct);
        await uow.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static object MapSkill(UserSkill s) => new
    {
        id          = s.Id,
        categoryId  = s.CategoryId,
        category    = s.Category?.Name,
        type        = s.Type.ToString(),
        level       = s.Level,
        description = s.Description,
        createdAt   = s.CreatedAt
    };

    private static Guid GetUserId(ClaimsPrincipal p) =>
        Guid.Parse(p.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? p.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim missing."));
}
