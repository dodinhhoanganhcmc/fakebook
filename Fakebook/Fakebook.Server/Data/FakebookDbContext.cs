using Fakebook.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fakebook.Server.Data;

public class FakebookDbContext(DbContextOptions<FakebookDbContext> options) : DbContext(options)
{
    public DbSet<User>          Users          => Set<User>();
    public DbSet<Friendship>    Friendships    => Set<Friendship>();
    public DbSet<Post>          Posts          => Set<Post>();
    public DbSet<Comment>       Comments       => Set<Comment>();
    public DbSet<Reaction>      Reactions      => Set<Reaction>();
    public DbSet<Share>         Shares         => Set<Share>();
    public DbSet<Activity>      Activities     => Set<Activity>();
    public DbSet<RefreshToken>  RefreshTokens  => Set<RefreshToken>();
    public DbSet<Listing>       Listings       => Set<Listing>();
    public DbSet<Bid>           Bids           => Set<Bid>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Friendship>(e =>
        {
            e.HasOne(x => x.Requester)
                .WithMany()
                .HasForeignKey(x => x.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Addressee)
                .WithMany()
                .HasForeignKey(x => x.AddresseeId)
                .OnDelete(DeleteBehavior.Restrict);

            // One pending/active record per directed pair.
            e.HasIndex(x => new { x.RequesterId, x.AddresseeId }).IsUnique();
        });

        b.Entity<Post>(e =>
        {
            e.HasOne(x => x.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.OriginalPost)
                .WithMany()
                .HasForeignKey(x => x.OriginalPostId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.AuthorId, x.CreatedAt });
        });

        b.Entity<Comment>(e =>
        {
            e.HasOne(x => x.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(x => x.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.PostId, x.CreatedAt });
        });

        b.Entity<Reaction>(e =>
        {
            e.HasOne(x => x.User)
                .WithMany(u => u.Reactions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Post)
                .WithMany(p => p.Reactions)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Comment)
                .WithMany(c => c.Reactions)
                .HasForeignKey(x => x.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            // One reaction per (user, post) and (user, comment) — toggle/replace in code.
            e.HasIndex(x => new { x.UserId, x.PostId }).IsUnique()
                .HasFilter("\"PostId\" IS NOT NULL");
            e.HasIndex(x => new { x.UserId, x.CommentId }).IsUnique()
                .HasFilter("\"CommentId\" IS NOT NULL");
        });

        b.Entity<Share>(e =>
        {
            e.HasOne(x => x.User)
                .WithMany(u => u.Shares)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Post)
                .WithMany(p => p.Shares)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Activity>(e =>
        {
            e.HasOne(x => x.User)
                .WithMany(u => u.Activities)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.UserId, x.CreatedAt });
        });

        b.Entity<RefreshToken>(e =>
        {
            e.HasOne(x => x.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.Token).IsUnique();
        });

        b.Entity<Listing>(e =>
        {
            e.HasOne(x => x.Seller)
                .WithMany(u => u.Listings)
                .HasForeignKey(x => x.SellerId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Buyer)
                .WithMany()
                .HasForeignKey(x => x.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.Status, x.Category });
        });

        b.Entity<Bid>(e =>
        {
            e.HasOne(x => x.Listing)
                .WithMany(l => l.Bids)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Bidder)
                .WithMany(u => u.Bids)
                .HasForeignKey(x => x.BidderId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.ListingId, x.Amount });
        });
    }
}
