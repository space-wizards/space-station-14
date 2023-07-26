using Content.Shared.StationRecords;

namespace Content.Shared.MassMedia.Systems;

[Serializable]
public struct NewsArticle
{
    public string Name;
    public string Content;
    public string? Author;
    public ICollection<StationRecordKey>? AuthorStationRecordKeyIds;
    public TimeSpan ShareTime;
}
