using Content.Shared.MassMedia.Systems;

namespace Content.Shared.MassMedia.Components;

/// <summary>
/// Stores published news articles, reaction state, and sequential IDs for a station.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedNewsSystem))]
public sealed partial class StationNewsComponent : Component
{
    /// <summary>
    /// List of articles published on this station.
    /// </summary>
    [DataField]
    public List<NewsArticle> Articles = new();

    /// <summary>
    /// Sequential counter used to assign a unique <see cref="NewsArticle.ArticleId"/> to each published article.
    /// </summary>
    [DataField]
    public int NextArticleId = 1;

    /// <summary>
    /// Sequential counter used to assign a unique <see cref="NewsComment.CommentId"/> to each posted comment.
    /// </summary>
    [DataField]
    public int NextCommentId = 1;

    /// <summary>
    /// Tracks which reactions each PDA (identified by its <see cref="NetEntity"/>) has active on a given article ID.
    /// </summary>
    [ViewVariables]
    public Dictionary<(int ArticleId, NetEntity LoaderUid), HashSet<NewsReactionType>> PdaReactions = new();
}


