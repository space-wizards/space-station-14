using Robust.Shared.Serialization;
using Content.Shared.MassMedia.Systems;

namespace Content.Shared.CartridgeLoader.Cartridges;

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
