using Robust.Shared.Serialization;
using Content.Shared.MassMedia.Systems;

namespace Content.Shared.MassMedia.Components;

[Serializable, NetSerializable]
public enum NewsReadUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class NewsReadBoundUserInterfaceState : BoundUserInterfaceState
{
    public NewsArticle Article;
    public int TargetNum;
    public int TotalNum;

    public NewsReadBoundUserInterfaceState(NewsArticle article, int targetNum, int totalNum)
    {
        Article = article;
        TargetNum = targetNum;
        TotalNum = totalNum;
    }
}

[Serializable, NetSerializable]
public sealed class NewsReadEmptyBoundUserInterfaceState : BoundUserInterfaceState
{
    public NewsReadEmptyBoundUserInterfaceState()
    {
    }
}

[Serializable, NetSerializable]
public sealed class NewsReadNextMessage : BoundUserInterfaceMessage
{
    public NewsReadNextMessage()
    {
    }
}

[Serializable, NetSerializable]
public sealed class NewsReadPrevMessage : BoundUserInterfaceMessage
{
    public NewsReadPrevMessage()
    {
    }
}

[Serializable, NetSerializable]
public sealed class NewsReadArticleRequestMessage : BoundUserInterfaceMessage
{
    public NewsReadArticleRequestMessage()
    {
    }
}
