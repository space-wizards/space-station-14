namespace Content.Shared.MassMedia.Systems;

[Serializable]
public struct NewsArticle
{
    public string Name;
    public string Content;
    public string? Author;
    public int? AuthorStationRecordKeyId;
    public TimeSpan ShareTime;
}
