namespace Content.Shared.Shuttles.UI.MapObjects;

public record struct GridMapObject : IMapObject
{
    public string Name { get; set; }
    public bool HideButton { get; init; }
    public EntityUid Entity;
}
