using Content.Shared.MassMedia.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.MassMedia.Components;

[Serializable, NetSerializable]
public enum NewsWriterUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed partial class NewsWriterBoundUserInterfaceState : BoundUserInterfaceState
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
public sealed partial class NewsWriterPublishMessage : BoundUserInterfaceMessage
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
public sealed partial class NewsWriterDeleteMessage : BoundUserInterfaceMessage
{
    public readonly int ArticleNum;

    public NewsWriterDeleteMessage(int num)
    {
        ArticleNum = num;
    }
}

[Serializable, NetSerializable]
public sealed partial class NewsWriterArticlesRequestMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed partial class NewsWriterSaveDraftMessage : BoundUserInterfaceMessage
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
public sealed partial class NewsWriterRequestDraftMessage : BoundUserInterfaceMessage
{
}

