using Robust.Shared.Serialization;
using Content.Shared.MassMedia.Systems;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NewsReaderBoundUserInterfaceState : BoundUserInterfaceState
{
    public NewsArticle Article;
    public int TargetNum;
    public int TotalNum;
    public bool NotificationOn;

    public NewsReaderBoundUserInterfaceState(NewsArticle article, int targetNum, int totalNum, bool notificationOn)
    {
        Article = article;
        TargetNum = targetNum;
        TotalNum = totalNum;
        NotificationOn = notificationOn;
    }
}

[Serializable, NetSerializable]
public sealed class NewsReaderEmptyBoundUserInterfaceState : BoundUserInterfaceState
{
    public bool NotificationOn;

    public NewsReaderEmptyBoundUserInterfaceState(bool notificationOn)
    {
        NotificationOn = notificationOn;
    }
}
