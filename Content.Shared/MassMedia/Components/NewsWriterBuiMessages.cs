using Content.Shared.MassMedia.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.MassMedia.Components;

[Serializable, NetSerializable]
public enum NewsWriterUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class NewsWriterBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly NewsArticle[] Articles;
    public readonly bool PublishEnabled;
    public readonly TimeSpan NextPublish;
    public readonly string DraftTitle;
    public readonly string DraftContent;

    public NewsWriterBoundUserInterfaceState(
        NewsArticle[] articles,
        bool publishEnabled,
        TimeSpan nextPublish,
        string draftTitle,
        string draftContent)
    {
        Articles = articles;
        PublishEnabled = publishEnabled;
        NextPublish = nextPublish;
        DraftTitle = draftTitle;
        DraftContent = draftContent;
    }
}

/// <summary>
/// Sent from the news writer UI to publish a new article.
/// </summary>
[Serializable, NetSerializable]
public sealed class NewsWriterPublishMessage : BoundUserInterfaceMessage
{
    public readonly string Title;
    public readonly string Content;
    public readonly bool CommentsLocked;

    public NewsWriterPublishMessage(string title, string content, bool commentsLocked = false)
    {
        Title = title;
        Content = content;
        CommentsLocked = commentsLocked;
    }
}

/// <summary>
/// Sent from the news writer UI to delete an existing article by index.
/// </summary>
[Serializable, NetSerializable]
public sealed class NewsWriterDeleteMessage : BoundUserInterfaceMessage
{
    public readonly int ArticleNum;

    public NewsWriterDeleteMessage(int num)
    {
        ArticleNum = num;
    }
}

/// <summary>
/// Sent from the news writer UI to lock or unlock reader comments on an article.
/// </summary>
[Serializable, NetSerializable]
public sealed class NewsWriterToggleLockCommentsMessage : BoundUserInterfaceMessage
{
    public readonly int ArticleNum;

    public NewsWriterToggleLockCommentsMessage(int articleNum)
    {
        ArticleNum = articleNum;
    }
}

/// <summary>
/// Sent from the news writer UI to delete a specific reader comment on an article.
/// </summary>
[Serializable, NetSerializable]
public sealed class NewsWriterDeleteCommentMessage : BoundUserInterfaceMessage
{
    public readonly int ArticleNum;
    public readonly int CommentId;

    public NewsWriterDeleteCommentMessage(int articleNum, int commentId)
    {
        ArticleNum = articleNum;
        CommentId = commentId;
    }
}

[Serializable, NetSerializable]
public sealed class NewsWriterArticlesRequestMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class NewsWriterSaveDraftMessage : BoundUserInterfaceMessage
{
    public readonly string DraftTitle;
    public readonly string DraftContent;

    public NewsWriterSaveDraftMessage(string draftTitle, string draftContent)
    {
        DraftTitle = draftTitle;
        DraftContent = draftContent;
    }
}

[Serializable, NetSerializable]
public sealed class NewsWriterRequestDraftMessage : BoundUserInterfaceMessage
{
}


