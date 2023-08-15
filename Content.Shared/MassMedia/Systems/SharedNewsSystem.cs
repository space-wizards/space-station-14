using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared.MassMedia.Systems;

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
    public ICollection<StationRecordKey>? AuthorStationRecordKeyIds;

    [ViewVariables]
    public TimeSpan ShareTime;
}
