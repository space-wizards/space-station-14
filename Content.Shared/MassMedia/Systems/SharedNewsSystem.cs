using Robust.Shared.Serialization;

namespace Content.Shared.MassMedia.Systems;

[Serializable, NetSerializable]
public struct NewsArticle
{
    public string Name;
    public string Content;
    public string? Author;
    public ICollection<(NetEntity, uint)>? AuthorStationRecordKeyIds;
    public TimeSpan ShareTime;
}
