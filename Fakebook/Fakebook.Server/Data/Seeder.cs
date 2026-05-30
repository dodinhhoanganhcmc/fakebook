using Fakebook.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fakebook.Server.Data;

public static class Seeder
{
    public static async Task SeedAsync(FakebookDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        var pwd = BCrypt.Net.BCrypt.HashPassword("Password123!");

        var alice = new User { Username = "alice", Email = "alice@fakebook.local", DisplayName = "Alice Nguyen", PasswordHash = pwd, Bio = "Front-end developer.", Location = "Hanoi" };
        var bob   = new User { Username = "bob",   Email = "bob@fakebook.local",   DisplayName = "Bob Tran",    PasswordHash = pwd, Bio = "Coffee + code.",      Location = "Saigon" };
        var carol = new User { Username = "carol", Email = "carol@fakebook.local", DisplayName = "Carol Le",    PasswordHash = pwd, Bio = "Photographer.",       Location = "Da Nang" };
        var dave  = new User { Username = "dave",  Email = "dave@fakebook.local",  DisplayName = "Dave Pham",   PasswordHash = pwd, Bio = "Backend engineer.",   Location = "Hue"    };
        db.Users.AddRange(alice, bob, carol, dave);
        await db.SaveChangesAsync();

        // Friendships: alice<->bob, alice<->carol accepted; dave->alice pending.
        db.Friendships.AddRange(
            new Friendship { RequesterId = alice.Id, AddresseeId = bob.Id,   Status = FriendshipStatus.Accepted, RespondedAt = DateTime.UtcNow },
            new Friendship { RequesterId = alice.Id, AddresseeId = carol.Id, Status = FriendshipStatus.Accepted, RespondedAt = DateTime.UtcNow },
            new Friendship { RequesterId = bob.Id,   AddresseeId = carol.Id, Status = FriendshipStatus.Accepted, RespondedAt = DateTime.UtcNow },
            new Friendship { RequesterId = dave.Id,  AddresseeId = alice.Id, Status = FriendshipStatus.Pending }
        );

        var p1 = new Post { AuthorId = alice.Id, Content = "Hello Fakebook! First post on the new platform.",              Privacy = PostPrivacy.Public };
        var p2 = new Post { AuthorId = bob.Id,   Content = "Friends-only weekend plans — anyone up for hiking?",           Privacy = PostPrivacy.FriendsOnly };
        var p3 = new Post { AuthorId = carol.Id, Content = "Some shots from yesterday's photo walk. Loving the light here.", Privacy = PostPrivacy.Public };
        var p4 = new Post { AuthorId = alice.Id, Content = "Private note to self: refactor the auth flow tomorrow.",       Privacy = PostPrivacy.Private };
        db.Posts.AddRange(p1, p2, p3, p4);
        await db.SaveChangesAsync();

        db.Comments.AddRange(
            new Comment { PostId = p1.Id, AuthorId = bob.Id,   Content = "Welcome! 🎉" },
            new Comment { PostId = p1.Id, AuthorId = carol.Id, Content = "Nice to see you here." },
            new Comment { PostId = p3.Id, AuthorId = alice.Id, Content = "These are beautiful." }
        );

        db.Reactions.AddRange(
            new Reaction { UserId = bob.Id,   PostId = p1.Id, Type = ReactionType.Like  },
            new Reaction { UserId = carol.Id, PostId = p1.Id, Type = ReactionType.Love  },
            new Reaction { UserId = alice.Id, PostId = p3.Id, Type = ReactionType.Wow   },
            new Reaction { UserId = bob.Id,   PostId = p3.Id, Type = ReactionType.Like  }
        );

        await db.SaveChangesAsync();

        // ----- Marketplace: a mix of fixed-price + auction listings -----
        var l1 = new Listing { SellerId = bob.Id,   Title = "Trek Marlin 7 Mountain Bike",     Description = "2023 model, size L. Hydraulic disc brakes, barely ridden. Pickup in District 1.", ImageUrl = "https://picsum.photos/seed/fb-mk-bike/800/600",     Category = ListingCategory.SportingGoods, Location = "Saigon",  Type = ListingType.FixedPrice, Price = 420m };
        var l2 = new Listing { SellerId = carol.Id, Title = "Canon EOS 90D DSLR (body only)",   Description = "32MP, 7k shutter count. Comes with battery, charger and strap. No reserve auction.",  ImageUrl = "https://picsum.photos/seed/fb-mk-camera/800/600",   Category = ListingCategory.Electronics,   Location = "Da Nang", Type = ListingType.Auction,    Price = 300m, AuctionEndsAt = DateTime.UtcNow.AddDays(3) };
        var l3 = new Listing { SellerId = alice.Id, Title = "IKEA Bekant Standing Desk",        Description = "Electric sit/stand desk, 160x80cm, white. Works perfectly.",                          ImageUrl = "https://picsum.photos/seed/fb-mk-desk/800/600",     Category = ListingCategory.HomeGarden,    Location = "Hanoi",   Type = ListingType.FixedPrice, Price = 150m };
        var l4 = new Listing { SellerId = dave.Id,  Title = "Vintage Vinyl Records (lot of 40)", Description = "Mostly 70s/80s rock and jazz. A few rare pressings. Selling the whole lot.",          ImageUrl = "https://picsum.photos/seed/fb-mk-vinyl/800/600",    Category = ListingCategory.Hobbies,       Location = "Hue",     Type = ListingType.Auction,    Price = 25m,  AuctionEndsAt = DateTime.UtcNow.AddDays(5) };
        var l5 = new Listing { SellerId = bob.Id,   Title = "Leather Sofa, 3-seater",           Description = "Genuine leather, dark brown. Some wear on the armrests but very comfortable.",          ImageUrl = "https://picsum.photos/seed/fb-mk-sofa/800/600",     Category = ListingCategory.HomeGarden,    Location = "Saigon",  Type = ListingType.FixedPrice, Price = 300m };
        var l6 = new Listing { SellerId = carol.Id, Title = "Custom Mechanical Keyboard 65%",   Description = "Hot-swappable, gateron browns, PBT keycaps. Built it myself, typing is lovely.",       ImageUrl = "https://picsum.photos/seed/fb-mk-keeb/800/600",     Category = ListingCategory.Electronics,   Location = "Da Nang", Type = ListingType.Auction,    Price = 60m,  AuctionEndsAt = DateTime.UtcNow.AddDays(2) };
        var l7 = new Listing { SellerId = alice.Id, Title = "Patagonia Winter Jacket (M)",      Description = "Down-filled, worn one season. Warm and packs small. No tears.",                        ImageUrl = "https://picsum.photos/seed/fb-mk-jacket/800/600",   Category = ListingCategory.Clothing,      Location = "Hanoi",   Type = ListingType.FixedPrice, Price = 95m };
        var l8 = new Listing { SellerId = dave.Id,  Title = "Nintendo Switch OLED + 4 games",   Description = "White OLED model with dock, plus Zelda, Mario Kart, Odyssey and Smash.",               ImageUrl = "https://picsum.photos/seed/fb-mk-switch/800/600",   Category = ListingCategory.Toys,          Location = "Hue",     Type = ListingType.FixedPrice, Price = 230m };
        db.Listings.AddRange(l1, l2, l3, l4, l5, l6, l7, l8);
        await db.SaveChangesAsync();

        db.Bids.AddRange(
            new Bid { ListingId = l2.Id, BidderId = alice.Id, Amount = 310m, CreatedAt = DateTime.UtcNow.AddHours(-20) },
            new Bid { ListingId = l2.Id, BidderId = dave.Id,  Amount = 325m, CreatedAt = DateTime.UtcNow.AddHours(-6)  },
            new Bid { ListingId = l4.Id, BidderId = bob.Id,   Amount = 30m,  CreatedAt = DateTime.UtcNow.AddHours(-12) },
            new Bid { ListingId = l6.Id, BidderId = alice.Id, Amount = 65m,  CreatedAt = DateTime.UtcNow.AddHours(-3)  }
        );
        await db.SaveChangesAsync();
    }
}
