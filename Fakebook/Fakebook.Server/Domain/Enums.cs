namespace Fakebook.Server.Domain;

public enum PostPrivacy
{
    Public = 0,
    FriendsOnly = 1,
    Private = 2
}

public enum FriendshipStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Blocked = 3
}

public enum ReactionType
{
    Like = 0,
    Love = 1,
    Haha = 2,
    Wow = 3,
    Sad = 4,
    Angry = 5
}

public enum ActivityType
{
    PostCreated = 0,
    PostEdited = 1,
    PostDeleted = 2,
    CommentCreated = 3,
    Reacted = 4,
    Shared = 5,
    FriendRequestSent = 6,
    FriendRequestAccepted = 7,
    ProfileUpdated = 8,
    AvatarUpdated = 9
}
