using Fakebook.Server.Auth;
using Fakebook.Server.Data;
using Fakebook.Server.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fakebook.Server.Endpoints;

public static class FeedEndpoints
{
    public static RouteGroupBuilder MapFeed(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/feed").WithTags("Feed").RequireAuthorization();

        g.MapGet("/", async (HttpContext http, FakebookDbContext db, [FromQuery] int? skip, [FromQuery] int? take) =>
        {
            var me = CurrentUser.Id(http.User);
            var friends = await FriendEndpoints.GetFriendIdsAsync(db, me);
            var visibleAuthors = friends.Append(me).ToList();

            var n = (take ?? 0) <= 0 ? 20 : Math.Min(take!.Value, 100);
            var s = Math.Max(skip ?? 0, 0);

            // Posts from me + friends, honoring privacy:
            //   - own posts: all privacy levels
            //   - friend posts: Public + FriendsOnly
            var ids = await db.Posts.AsNoTracking()
                .Where(p => p.DeletedAt == null && visibleAuthors.Contains(p.AuthorId))
                .Where(p => p.AuthorId == me
                            || p.Privacy == PostPrivacy.Public
                            || p.Privacy == PostPrivacy.FriendsOnly)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(s).Take(n)
                .Select(p => p.Id)
                .ToListAsync();

            return Results.Ok(await PostEndpoints.LoadPostsAsync(db, ids, me));
        });

        return api;
    }
}
