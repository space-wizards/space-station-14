using Content.Shared.MassMedia.Systems;

namespace Content.Server.MassMedia.Components;

[RegisterComponent]
public sealed class StationNewsComponent : Component
{
    [DataField("articles")]
    public List<NewsArticle> Articles = new();
}
