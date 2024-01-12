using Robust.Shared.Serialization;
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
    [ViewVariables(VVAccess.ReadWrite)]
    public string Name;

    [ViewVariables(VVAccess.ReadWrite)]
    public string Content;

    [ViewVariables(VVAccess.ReadWrite)]
    public string? Author;

    [ViewVariables]
    public ICollection<(NetEntity, uint)>? AuthorStationRecordKeyIds;

    [ViewVariables]
    public TimeSpan ShareTime;
}

[ByRefEvent]
public record struct NewsArticlePublishedEvent(NewsArticle Article);

[ByRefEvent]
public record struct NewsArticleDeletedEvent;
