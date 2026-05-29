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
    }
}
