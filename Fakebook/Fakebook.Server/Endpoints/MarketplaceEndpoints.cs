using Fakebook.Server.Auth;
using Fakebook.Server.Data;
using Fakebook.Server.Domain;
using Fakebook.Server.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fakebook.Server.Endpoints;

public static class MarketplaceEndpoints
{
    // Minimum increment between bids (auctions).
    private const decimal BidIncrement = 1m;

    public static RouteGroupBuilder MapMarketplace(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/marketplace").WithTags("Marketplace").RequireAuthorization();

        // Browse active listings, optionally filtered by category / type / search term.
        g.MapGet("/", async (FakebookDbContext db,
            [FromQuery] string? category, [FromQuery] string? q, [FromQuery] string? type,
            [FromQuery] int? skip, [FromQuery] int? take) =>
        {
            var n = (take ?? 0) <= 0 ? 24 : Math.Min(take!.Value, 100);
            var s = Math.Max(skip ?? 0, 0);

            var query = db.Listings.AsNoTracking()
                .Where(l => l.DeletedAt == null && l.Status == ListingStatus.Active);

            if (Enum.TryParse<ListingCategory>(category, true, out var cat))
                query = query.Where(l => l.Category == cat);
            if (Enum.TryParse<ListingType>(type, true, out var t))
                query = query.Where(l => l.Type == t);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(l =>
                    EF.Functions.ILike(l.Title, $"%{term}%") ||
                    EF.Functions.ILike(l.Description, $"%{term}%"));
            }

            var ids = await query.OrderByDescending(l => l.CreatedAt).Skip(s).Take(n)
                .Select(l => l.Id).ToListAsync();
            return Results.Ok(await LoadListingsAsync(db, ids));
        });

        // The signed-in user's own listings (any status).
        g.MapGet("/mine", async (HttpContext http, FakebookDbContext db) =>
        {
            var me = CurrentUser.Id(http.User);
            var ids = await db.Listings.AsNoTracking()
                .Where(l => l.SellerId == me && l.DeletedAt == null)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => l.Id).ToListAsync();
            return Results.Ok(await LoadListingsAsync(db, ids));
        });

        g.MapGet("/{id:guid}", async (Guid id, HttpContext http, FakebookDbContext db) =>
        {
            var me = CurrentUser.Id(http.User);
            var dto = await LoadDetailAsync(db, id, me);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        g.MapPost("/", async (CreateListingRequest req, HttpContext http, FakebookDbContext db) =>
        {
            var me = CurrentUser.Id(http.User);
            if (string.IsNullOrWhiteSpace(req.Title))
                return Results.BadRequest(new { error = "Title is required" });
            if (req.Price < 0)
                return Results.BadRequest(new { error = "Price cannot be negative" });

            var listing = new Listing
            {
                SellerId    = me,
                Title       = req.Title.Trim(),
                Description = req.Description?.Trim() ?? "",
                ImageUrl    = string.IsNullOrWhiteSpace(req.ImageUrl) ? null : req.ImageUrl.Trim(),
                Category    = req.Category,
                Location    = string.IsNullOrWhiteSpace(req.Location) ? null : req.Location.Trim(),
                Type        = req.Type,
                Price       = decimal.Round(req.Price, 2),
                AuctionEndsAt = req.Type == ListingType.Auction
                    ? DateTime.UtcNow.AddDays(Math.Clamp(req.AuctionDays ?? 7, 1, 30))
                    : null
            };
            db.Listings.Add(listing);
            db.Activities.Add(new Activity { UserId = me, Type = ActivityType.ListingCreated, Summary = $"Listed \"{listing.Title}\"" });
            await db.SaveChangesAsync();
            return Results.Created($"/api/marketplace/{listing.Id}", await LoadDetailAsync(db, listing.Id, me));
        });

        // Place a bid on an auction.
        g.MapPost("/{id:guid}/bids", async (Guid id, PlaceBidRequest req, HttpContext http, FakebookDbContext db) =>
        {
            var me = CurrentUser.Id(http.User);
            var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == id && l.DeletedAt == null);
            if (listing is null) return Results.NotFound();
            if (listing.Type != ListingType.Auction)
                return Results.BadRequest(new { error = "This listing is not an auction" });
            if (listing.SellerId == me)
                return Results.BadRequest(new { error = "You cannot bid on your own listing" });
            if (listing.Status != ListingStatus.Active ||
                (listing.AuctionEndsAt is { } end && end <= DateTime.UtcNow))
                return Results.BadRequest(new { error = "This auction has ended" });

            var highest = await db.Bids.Where(b => b.ListingId == id).MaxAsync(b => (decimal?)b.Amount);
            var min = highest is null ? listing.Price : highest.Value + BidIncrement;
            var amount = decimal.Round(req.Amount, 2);
            if (amount < min)
                return Results.BadRequest(new { error = $"Bid must be at least {min:0.00}" });

            db.Bids.Add(new Bid { ListingId = id, BidderId = me, Amount = amount });
            db.Activities.Add(new Activity { UserId = me, Type = ActivityType.BidPlaced, Summary = $"Bid {amount:0.00} on \"{listing.Title}\"" });
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetailAsync(db, id, me));
        });

        // Buy a fixed-price listing outright.
        g.MapPost("/{id:guid}/buy", async (Guid id, HttpContext http, FakebookDbContext db) =>
        {
            var me = CurrentUser.Id(http.User);
            var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == id && l.DeletedAt == null);
            if (listing is null) return Results.NotFound();
            if (listing.Type != ListingType.FixedPrice)
                return Results.BadRequest(new { error = "Auctions cannot be bought directly" });
            if (listing.SellerId == me)
                return Results.BadRequest(new { error = "You cannot buy your own listing" });
            if (listing.Status != ListingStatus.Active)
                return Results.BadRequest(new { error = "This item is no longer available" });

            listing.Status    = ListingStatus.Sold;
            listing.BuyerId   = me;
            listing.UpdatedAt = DateTime.UtcNow;
            db.Activities.Add(new Activity { UserId = me, Type = ActivityType.ListingSold, Summary = $"Bought \"{listing.Title}\"" });
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetailAsync(db, id, me));
        });

        // Cancel (soft-delete) a listing the user owns.
        g.MapDelete("/{id:guid}", async (Guid id, HttpContext http, FakebookDbContext db) =>
        {
            var me = CurrentUser.Id(http.User);
            var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == id && l.DeletedAt == null);
            if (listing is null) return Results.NotFound();
            if (listing.SellerId != me) return Results.Forbid();

            listing.Status    = ListingStatus.Cancelled;
            listing.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return api;
    }

    private static UserSummary ToSummary(User u) => new(u.Id, u.Username, u.DisplayName, u.AvatarUrl);

    // An active auction whose end time has passed is reported as Ended (settled lazily at read time).
    private static ListingStatus EffectiveStatus(Listing l) =>
        l.Status == ListingStatus.Active && l.Type == ListingType.Auction &&
        l.AuctionEndsAt is { } end && end <= DateTime.UtcNow
            ? ListingStatus.Ended
            : l.Status;

    private static async Task<List<ListingDto>> LoadListingsAsync(FakebookDbContext db, IReadOnlyList<Guid> ids)
    {
        if (ids.Count == 0) return new List<ListingDto>();

        var listings = await db.Listings.AsNoTracking()
            .Where(l => ids.Contains(l.Id))
            .Include(l => l.Seller)
            .ToListAsync();

        var agg = await db.Bids
            .Where(b => ids.Contains(b.ListingId))
            .GroupBy(b => b.ListingId)
            .Select(grp => new { grp.Key, Max = grp.Max(b => b.Amount), Count = grp.Count() })
            .ToDictionaryAsync(x => x.Key, x => x);

        ListingDto ToDto(Listing l)
        {
            var hasBids = agg.TryGetValue(l.Id, out var a) && a!.Count > 0;
            var current = l.Type == ListingType.Auction && hasBids ? a!.Max : l.Price;
            var count = hasBids ? a!.Count : 0;
            return new ListingDto(
                l.Id, ToSummary(l.Seller!), l.Title, l.ImageUrl, l.Category, l.Location,
                l.Type, l.Price, current, count, l.AuctionEndsAt, EffectiveStatus(l), l.CreatedAt);
        }

        var byId = listings.ToDictionary(l => l.Id);
        return ids.Where(byId.ContainsKey).Select(id => ToDto(byId[id])).ToList();
    }

    private static async Task<ListingDetailDto?> LoadDetailAsync(FakebookDbContext db, Guid id, Guid me)
    {
        var l = await db.Listings.AsNoTracking()
            .Include(x => x.Seller)
            .Include(x => x.Buyer)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
        if (l is null) return null;

        var bids = await db.Bids.AsNoTracking()
            .Where(b => b.ListingId == id)
            .Include(b => b.Bidder)
            .OrderByDescending(b => b.Amount).ThenByDescending(b => b.CreatedAt)
            .Select(b => new BidDto(
                b.Id,
                new UserSummary(b.Bidder!.Id, b.Bidder.Username, b.Bidder.DisplayName, b.Bidder.AvatarUrl),
                b.Amount, b.CreatedAt))
            .ToListAsync();

        var count = bids.Count;
        var isAuction = l.Type == ListingType.Auction;
        var current = isAuction && count > 0 ? bids[0].Amount : l.Price;
        var highest = count > 0 ? bids[0].Bidder : null;
        var minNext = isAuction ? (count == 0 ? l.Price : current + BidIncrement) : 0m;
        var status = EffectiveStatus(l);

        // Winner: explicit buyer (fixed-price sale) or the top bidder of an ended auction.
        var buyer = l.Buyer is not null ? ToSummary(l.Buyer)
            : status == ListingStatus.Ended ? highest
            : null;

        return new ListingDetailDto(
            l.Id, ToSummary(l.Seller!), l.Title, l.Description, l.ImageUrl, l.Category, l.Location,
            l.Type, l.Price, current, minNext, count, l.AuctionEndsAt, status,
            highest, buyer, l.SellerId == me, l.CreatedAt, bids);
    }
}
