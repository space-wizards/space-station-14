using Content.Shared.Maps;

namespace Content.Shared.Movement.Events;

/// <summary>
///     This event is used to modify or override the tileDef an entity is currently standing on.
///     Because this is used to bulldoze values you should only call this if you know what you're doing.
/// </summary>
[ByRefEvent]
public record struct MoverTileDefEvent
{
    /// <summary>
    ///     Overrides a tile's MobFriction modifier
    /// </summary>
    public float? MobFriction;

    /// <summary>
    ///     Overrides a tile's Friction modifier
    /// </summary>
    public float? Friction;

    /// <summary>
    ///     Overrides a tile's MobAcceleration modifier
    /// </summary>
    public float? MobAcceleration;
}
