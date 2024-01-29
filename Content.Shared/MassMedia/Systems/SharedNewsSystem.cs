using Robust.Shared.Serialization;

namespace Content.Shared.MassMedia.Systems;

public abstract class SharedNewsSystem : EntitySystem
{
    public const int MaxNameLength = 25;
    public const int MaxArticleLength = 2048;
}

[Serializable, NetSerializable]
public struct NewsArticle
{
    public string Name;
    public string Content;
    public string? Author;
    public ICollection<(NetEntity, uint)>? AuthorStationRecordKeyIds;
    public TimeSpan ShareTime;
}
