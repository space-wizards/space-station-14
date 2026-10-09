using Robust.Shared.Serialization;

namespace Content.Shared.MassMedia.Systems;

public abstract class SharedNewsSystem : EntitySystem
{
    public const int MaxTitleLength = 25;
    public const int MaxContentLength = 2048;
    public const int MaxCommentLength = 256;
    public const int MaxCommentsPerArticle = 50;
}

/// <summary>
/// Reaction types that crew members can toggle on a <see cref="NewsArticle"/> from their PDA.
/// </summary>
[Serializable, NetSerializable]
public enum NewsReactionType : byte
{
    Upvote,
    Downvote,
    Laugh,
    Shock,
    Heart
}

/// <summary>
/// Represents a single reader comment posted on a <see cref="NewsArticle"/>.
/// </summary>
[Serializable, NetSerializable]
public struct NewsComment
{
    /// <summary>
    /// Unique identifier for this comment on the station, used for moderation deletion.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int CommentId;

    /// <summary>
    /// Real name of the player who posted the comment, or null if unavailable.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string? Author;

    /// <summary>
    /// Body text of the comment.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string Content;

    /// <summary>
    /// Round duration timestamp when the comment was posted.
    /// </summary>
    [ViewVariables]
    public TimeSpan PostedTime;
}

/// <summary>
/// Represents a station news article published through a news writer console.
/// </summary>
[Serializable, NetSerializable]
public struct NewsArticle
{
    /// <summary>
    /// Unique sequential identifier for this article on the station.
    /// </summary>
    [ViewVariables]
    public int ArticleId;

    /// <summary>
    /// Headline of the article.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string Title;

    /// <summary>
    /// Main body content of the article (supports permissive rich text markup).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string Content;

    /// <summary>
    /// Author name and job title at the time of publication, or null if anonymous.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string? Author;

    /// <summary>
    /// Station record keys associated with the author, if any.
    /// </summary>
    [ViewVariables]
    public ICollection<(NetEntity, uint)>? AuthorStationRecordKeyIds;

    /// <summary>
    /// Round duration timestamp when the article was published.
    /// </summary>
    [ViewVariables]
    public TimeSpan ShareTime;

    /// <summary>
    /// Whether new reader comments are locked/disabled on this article.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CommentsLocked;

    /// <summary>
    /// Reader comments posted on this article.
    /// </summary>
    [ViewVariables]
    public List<NewsComment>? Comments;

    /// <summary>
    /// Aggregate reaction counts on this article by reaction type.
    /// </summary>
    [ViewVariables]
    public Dictionary<NewsReactionType, int>? ReactionCounts;
}

/// <summary>
/// Raised on <see cref="CartridgeLoader.Cartridges.NewsReaderCartridgeComponent"/> entities when a new article is published.
/// </summary>
[ByRefEvent]
public readonly record struct NewsArticlePublishedEvent(NewsArticle Article);

/// <summary>
/// Raised on <see cref="CartridgeLoader.Cartridges.NewsReaderCartridgeComponent"/> entities when an article is deleted.
/// </summary>
[ByRefEvent]
public readonly record struct NewsArticleDeletedEvent;

/// <summary>
/// Raised on <see cref="CartridgeLoader.Cartridges.NewsReaderCartridgeComponent"/> entities when an article's reactions, comments, or lock state change.
/// </summary>
[ByRefEvent]
public readonly record struct NewsArticleUpdatedEvent(int ArticleId);

