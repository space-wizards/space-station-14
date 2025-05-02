using Content.Shared.Maps;

namespace Content.Shared.Movement.Events;

/// <summary>
///     This event is used to modify or override the tileDef an entity is currently standing on.
/// </summary>
[ByRefEvent]
public record struct MoverTileDefEvent
{
    public ContentTileDefinition? TileDef;
}
