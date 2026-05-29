using Fakebook.Server.Auth;
using Fakebook.Server.Data;
using Fakebook.Server.Domain;
using Fakebook.Server.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fakebook.Server.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUsers(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/users").WithTags("Users").RequireAuthorization();

        g.MapGet("/me", async (HttpContext http, FakebookDbContext db) =>
        {
            var id = CurrentUser.Id(http.User);
            return await LoadProfile(db, id) is { } p ? Results.Ok(p) : Results.NotFound();
        });

        g.MapPut("/me", async (UpdateProfileRequest req, HttpContext http, FakebookDbContext db) =>
        {
            var id = CurrentUser.Id(http.User);
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            if (req.DisplayName is not null) user.DisplayName = req.DisplayName;
            if (req.Bio        is not null) user.Bio        = req.Bio;
            if (req.BirthDate  is not null) user.BirthDate  = req.BirthDate;
            if (req.Gender     is not null) user.Gender     = req.Gender;
            if (req.Location   is not null) user.Location   = req.Location;
            user.UpdatedAt = DateTime.UtcNow;

            db.Activities.Add(new Activity { UserId = id, Type = ActivityType.ProfileUpdated, Summary = "Updated profile" });
            await db.SaveChangesAsync();
            return Results.Ok(await LoadProfile(db, id));
        });

        g.MapPut("/me/avatar", async (UpdateAvatarRequest req, HttpContext http, FakebookDbContext db) =>
        {
            var id = CurrentUser.Id(http.User);
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            user.AvatarUrl = req.AvatarUrl;
            user.UpdatedAt = DateTime.UtcNow;
            db.Activities.Add(new Activity { UserId = id, Type = ActivityType.AvatarUpdated, Summary = "Updated avatar" });
            await db.SaveChangesAsync();
            return Results.Ok(await LoadProfile(db, id));
        });

        g.MapGet("/{userId:guid}", async (Guid userId, FakebookDbContext db) =>
            await LoadProfile(db, userId) is { } p ? Results.Ok(p) : Results.NotFound());

        g.MapGet("/search", async ([FromQuery] string? q, [FromQuery] int? take, FakebookDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(q)) return Results.Ok(Array.Empty<UserSummary>());
            var n = (take ?? 0) <= 0 ? 20 : Math.Min(take!.Value, 100);
            var key = q.Trim().ToLowerInvariant();
            var matches = await db.Users
                .Where(u => u.Username.ToLower().Contains(key) || u.DisplayName.ToLower().Contains(key))
                .OrderBy(u => u.Username)
                .Take(n)
                .Select(u => new UserSummary(u.Id, u.Username, u.DisplayName, u.AvatarUrl))
                .ToListAsync();
            return Results.Ok(matches);
        });

        g.MapGet("/me/activities", async (HttpContext http, FakebookDbContext db, [FromQuery] int? take) =>
        {
            var id = CurrentUser.Id(http.User);
            var n = (take ?? 0) <= 0 ? 50 : Math.Min(take!.Value, 200);
            var list = await db.Activities
                .Where(a => a.UserId == id)
                .OrderByDescending(a => a.CreatedAt)
                .Take(n)
                .Select(a => new ActivityDto(a.Id, a.Type, a.Summary, a.TargetPostId, a.TargetCommentId, a.TargetUserId, a.CreatedAt))
                .ToListAsync();
            return Results.Ok(list);
        });

        return api;
    }

    private static async Task<UserProfile?> LoadProfile(FakebookDbContext db, Guid id)
    {
        var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return null;

        var friendCount = await db.Friendships.CountAsync(f =>
            f.Status == FriendshipStatus.Accepted && (f.RequesterId == id || f.AddresseeId == id));
        var postCount = await db.Posts.CountAsync(p => p.AuthorId == id && p.DeletedAt == null);

        return new UserProfile(u.Id, u.Username, u.Email, u.DisplayName, u.AvatarUrl, u.Bio,
            u.BirthDate, u.Gender, u.Location, u.CreatedAt, friendCount, postCount);
    }
}
