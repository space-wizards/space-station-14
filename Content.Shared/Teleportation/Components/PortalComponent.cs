using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

/// <summary>
///     Marks an entity as being a 'portal' which teleports entities sent through it to linked entities.
///     Relies on <see cref="LinkedEntityComponent"/> being set up.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class PortalComponent : Component
{
    /// <summary>
    ///     Sound played on arriving to this portal, centered on the destination.
    ///     The arrival sound of the entered portal will play if the destination is not a portal.
    /// </summary>
    [DataField("arrivalSound")]
    public SoundSpecifier ArrivalSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    /// <summary>
    ///     Sound played on departing from this portal, centered on the original portal.
    /// </summary>
    [DataField("departureSound")]
    public SoundSpecifier DepartureSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");

    /// <summary>
    ///     If no portals are linked, the subject will be teleported a random distance at maximum this far away.
    /// </summary>
    [DataField("maxRandomRadius")]
    public float MaxRandomRadius = 7.0f;
}
