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
    public NewsArticle[] Articles;
    public bool ShareAvalible;

    public NewsWriterBoundUserInterfaceState(NewsArticle[] articles, bool shareAvalible)
    {
        Articles = articles;
        ShareAvalible = shareAvalible;
    }
}

[Serializable, NetSerializable]
public sealed class NewsWriterShareMessage : BoundUserInterfaceMessage
{
    public NewsArticle Article;

    public NewsWriterShareMessage(NewsArticle article)
    {
        Article = article;
    }
}

[Serializable, NetSerializable]
public sealed class NewsWriterDeleteMessage : BoundUserInterfaceMessage
{
    public int ArticleNum;

    public NewsWriterDeleteMessage(int num)
    {
        ArticleNum = num;
    }
}

[Serializable, NetSerializable]
public sealed class NewsWriterArticlesRequestMessage : BoundUserInterfaceMessage
{
    public NewsWriterArticlesRequestMessage()
    {
    }
}
