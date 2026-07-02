using Robust.Shared.Map;

namespace Content.Shared.Atmos;

/// <summary>
/// Raised on entities that have an AtmosExposedComponent when AtmosphereSystem updates.
/// Exposure events are raised by AtmosphereSystem at some fixed interval.
/// </summary>
/// <param name="coordinates">The coordinates of the entity that is being exposed.</param>
/// <param name="mixture">The gas mixture that the entity is exposed to.</param>
/// <param name="transform">The xform of the entity that is being exposed.</param>
[ByRefEvent]
public readonly struct AtmosExposedUpdateEvent(
    EntityCoordinates coordinates,
    GasMixture mixture,
    TransformComponent transform)
{
    public readonly EntityCoordinates Coordinates = coordinates;
    public readonly GasMixture GasMixture = mixture;
    public readonly TransformComponent Transform = transform;
}

/// <summary>
/// Event that tries to query the mixture a certain entity is exposed to.
/// This is mainly intended for use with entities inside of containers.
/// This event is not raised for entities that are directly parented to the grid.
/// </summary>
[ByRefEvent]
public struct AtmosExposedGetAirEvent
{
    /// <summary>
    /// The entity we want to query this for.
    /// </summary>
    public readonly Entity<TransformComponent> Entity;

    /// <summary>
    /// The mixture that the entity is exposed to. Output parameter.
    /// </summary>
    public GasMixture? Gas = null;

    /// <summary>
    /// Whether to excite the mixture, if possible.
    /// </summary>
    public readonly bool Excite = false;

    /// <summary>
    /// Whether this event has been handled or not.
    /// Check this before changing anything.
    /// </summary>
    public bool Handled = false;

    public AtmosExposedGetAirEvent(Entity<TransformComponent> entity, bool excite = false)
    {
        Entity = entity;
        Excite = excite;
    }
}
