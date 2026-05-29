using Fakebook.Server.Auth;
using Fakebook.Server.Data;
using Fakebook.Server.Domain;
using Fakebook.Server.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Fakebook.Server.Endpoints;

public static class FriendEndpoints
{
    public static RouteGroupBuilder MapFriends(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/friends").WithTags("Friends").RequireAuthorization();

        g.MapGet("/", async (HttpContext http, FakebookDbContext db) =>
        {
            var me = CurrentUser.Id(http.User);
            var list = await db.Friendships
                .Where(f => f.Status == FriendshipStatus.Accepted && (f.RequesterId == me || f.AddresseeId == me))
                .Select(f => new
                {
                    f.Id,
                    Other = f.RequesterId == me ? f.Addressee : f.Requester,
                    f.RespondedAt,
                    f.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(list
                .Where(x => x.Other is not null)
                .Select(x => new FriendDto(
                    x.Id,
                    new UserSummary(x.Other!.Id, x.Other.Username, x.Other.DisplayName, x.Other.AvatarUrl),
                    x.RespondedAt ?? x.CreatedAt)));
        });

        g.MapGet("/requests/incoming", async (HttpContext http, FakebookDbContext db) =>
        {
            var me = CurrentUser.Id(http.User);
            var list = await db.Friendships
                .Where(f => f.AddresseeId == me && f.Status == FriendshipStatus.Pending)
                .Include(f => f.Requester)
                .Select(f => new FriendRequestDto(
                    f.Id,
                    new UserSummary(f.Requester!.Id, f.Requester.Username, f.Requester.DisplayName, f.Requester.AvatarUrl),
                    f.CreatedAt))
                .ToListAsync();
            return Results.Ok(list);
        });

        g.MapGet("/requests/outgoing", async (HttpContext http, FakebookDbContext db) =>
        {
            var me = CurrentUser.Id(http.User);
            var list = await db.Friendships
                .Where(f => f.RequesterId == me && f.Status == FriendshipStatus.Pending)
                .Include(f => f.Addressee)
                .Select(f => new FriendRequestDto(
                    f.Id,
                    new UserSummary(f.Addressee!.Id, f.Addressee.Username, f.Addressee.DisplayName, f.Addressee.AvatarUrl),
                    f.CreatedAt))
                .ToListAsync();
            return Results.Ok(list);
        });

        g.MapPost("/requests", async (SendFriendRequest req, HttpContext http, FakebookDbContext db) =>
        {
            var me = CurrentUser.Id(http.User);
            if (req.TargetUserId == me) return Results.BadRequest(new { error = "Cannot friend yourself" });

            var target = await db.Users.FindAsync(req.TargetUserId);
            if (target is null) return Results.NotFound(new { error = "User not found" });

            var existing = await db.Friendships.FirstOrDefaultAsync(f =>
                (f.RequesterId == me && f.AddresseeId == req.TargetUserId) ||
                (f.RequesterId == req.TargetUserId && f.AddresseeId == me));

            if (existing is not null)
                return Results.Conflict(new { error = "Friendship already exists", status = existing.Status.ToString() });

            var f = new Friendship { RequesterId = me, AddresseeId = req.TargetUserId };
            db.Friendships.Add(f);
            db.Activities.Add(new Activity
            {
                UserId = me, Type = ActivityType.FriendRequestSent,
                Summary = $"Sent friend request to {target.DisplayName}", TargetUserId = req.TargetUserId
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { friendshipId = f.Id });
        });

        g.MapPost("/requests/{friendshipId:guid}/accept", async (Guid friendshipId, HttpContext http, FakebookDbContext db) =>
        {
            var me = CurrentUser.Id(http.User);
            var f = await db.Friendships.Include(x => x.Requester).FirstOrDefaultAsync(x => x.Id == friendshipId);
            if (f is null || f.AddresseeId != me) return Results.NotFound();
            if (f.Status != FriendshipStatus.Pending) return Results.BadRequest(new { error = "Not pending" });

            f.Status = FriendshipStatus.Accepted;
            f.RespondedAt = DateTime.UtcNow;
            db.Activities.Add(new Activity
            {
                UserId = me, Type = ActivityType.FriendRequestAccepted,
                Summary = $"Accepted friend request from {f.Requester?.DisplayName}", TargetUserId = f.RequesterId
            });
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapPost("/requests/{friendshipId:guid}/decline", async (Guid friendshipId, HttpContext http, FakebookDbContext db) =>
        {
            var me = CurrentUser.Id(http.User);
            var f = await db.Friendships.FindAsync(friendshipId);
            if (f is null || f.AddresseeId != me) return Results.NotFound();
            if (f.Status != FriendshipStatus.Pending) return Results.BadRequest(new { error = "Not pending" });

            f.Status = FriendshipStatus.Declined;
            f.RespondedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapDelete("/{friendshipId:guid}", async (Guid friendshipId, HttpContext http, FakebookDbContext db) =>
        {
            var me = CurrentUser.Id(http.User);
            var f = await db.Friendships.FindAsync(friendshipId);
            if (f is null) return Results.NotFound();
            if (f.RequesterId != me && f.AddresseeId != me) return Results.Forbid();

            db.Friendships.Remove(f);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return api;
    }

    // Shared friend-id list helper, used by feed + privacy checks.
    public static async Task<HashSet<Guid>> GetFriendIdsAsync(FakebookDbContext db, Guid userId)
    {
        var pairs = await db.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted && (f.RequesterId == userId || f.AddresseeId == userId))
            .Select(f => new { f.RequesterId, f.AddresseeId })
            .ToListAsync();
        return pairs.Select(p => p.RequesterId == userId ? p.AddresseeId : p.RequesterId).ToHashSet();
    }
}
