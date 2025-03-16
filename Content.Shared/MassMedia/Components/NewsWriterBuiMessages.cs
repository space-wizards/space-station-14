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

    public NewsWriterBoundUserInterfaceState(NewsArticle[] articles, bool publishEnabled, TimeSpan nextPublish, string draftTitle, string draftContent)
    {
        Articles = articles;
        PublishEnabled = publishEnabled;
        NextPublish = nextPublish;
        DraftTitle = draftTitle;
        DraftContent = draftContent;
    }
}

[Serializable, NetSerializable]
public sealed class NewsWriterPublishMessage : BoundUserInterfaceMessage
{
    public readonly string Title;
    public readonly string Content;


    public NewsWriterPublishMessage(string title, string content)
    {
        Title = title;
        Content = content;
    }
}

[Serializable, NetSerializable]
public sealed class NewsWriterDeleteMessage : BoundUserInterfaceMessage
{
    public readonly int ArticleNum;

    public NewsWriterDeleteMessage(int num)
    {
        ArticleNum = num;
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
