using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fakebook.Server.Domain;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(64)]
    public string Username { get; set; } = "";

    [MaxLength(256)]
    public string Email { get; set; } = "";

    [MaxLength(256)]
    public string PasswordHash { get; set; } = "";

    [MaxLength(128)]
    public string DisplayName { get; set; } = "";

    [MaxLength(2048)]
    public string? AvatarUrl { get; set; }

    [MaxLength(1024)]
    public string? Bio { get; set; }

    public DateOnly? BirthDate { get; set; }

    [MaxLength(16)]
    public string? Gender { get; set; }

    [MaxLength(256)]
    public string? Location { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Post>          Posts          { get; set; } = new List<Post>();
    public ICollection<Comment>       Comments       { get; set; } = new List<Comment>();
    public ICollection<Reaction>      Reactions      { get; set; } = new List<Reaction>();
    public ICollection<Share>         Shares         { get; set; } = new List<Share>();
    public ICollection<Activity>      Activities     { get; set; } = new List<Activity>();
    public ICollection<RefreshToken>  RefreshTokens  { get; set; } = new List<RefreshToken>();
}

public class Friendship
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RequesterId { get; set; }
    public User? Requester { get; set; }

    public Guid AddresseeId { get; set; }
    public User? Addressee { get; set; }

    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
}

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AuthorId { get; set; }
    public User? Author { get; set; }

    [MaxLength(10_000)]
    public string Content { get; set; } = "";

    [MaxLength(2048)]
    public string? ImageUrl { get; set; }

    public PostPrivacy Privacy { get; set; } = PostPrivacy.Public;

    // If this post is a share of another post, point at the original.
    public Guid? OriginalPostId { get; set; }
    public Post? OriginalPost { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<Comment>  Comments  { get; set; } = new List<Comment>();
    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
    public ICollection<Share>    Shares    { get; set; } = new List<Share>();
}

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public Guid AuthorId { get; set; }
    public User? Author { get; set; }

    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }

    [MaxLength(4096)]
    public string Content { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
    public ICollection<Comment>  Replies   { get; set; } = new List<Comment>();
}

public class Reaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    // Exactly one of PostId / CommentId is set.
    public Guid? PostId { get; set; }
    public Post? Post { get; set; }

    public Guid? CommentId { get; set; }
    public Comment? Comment { get; set; }

    public ReactionType Type { get; set; } = ReactionType.Like;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Share
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    [MaxLength(2048)]
    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public ActivityType Type { get; set; }

    [MaxLength(512)]
    public string? Summary { get; set; }

    public Guid? TargetPostId    { get; set; }
    public Guid? TargetCommentId { get; set; }
    public Guid? TargetUserId    { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    [MaxLength(512)]
    public string Token { get; set; } = "";

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
}
