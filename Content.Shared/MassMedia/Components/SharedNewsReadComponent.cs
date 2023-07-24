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
    public int TargetNum, TotalNum;

    public NewsReadBoundUserInterfaceState(NewsArticle article, int targetNum, int totalNum)
    {
        Article = article;
        TargetNum = targetNum;
        TotalNum = totalNum;
    }
}

[Serializable, NetSerializable]
public sealed class NewsReadLeafMessage : BoundUserInterfaceMessage
{
    public bool IsNext;

    public NewsReadLeafMessage(bool isNext)
    {
        IsNext = isNext;
    }
}
