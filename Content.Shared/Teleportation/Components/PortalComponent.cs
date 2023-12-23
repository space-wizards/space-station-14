using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

/// <summary>
///     Marks an entity as being a 'portal' which teleports entities sent through it to linked entities.
///     Relies on <see cref="LinkedEntityComponent"/> being set up.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PortalComponent : Component
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
    [DataField("maxRandomRadius"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxRandomRadius = 7.0f;

    /// <summary>
    ///     If false, this portal will fail to teleport and fizzle out if attempting to send an entity to a different map
    /// </summary>
    /// <remarks>
    ///     Shouldn't be able to teleport people to centcomm or the eshuttle from the station
    /// </remarks>
    [DataField("canTeleportToOtherMaps"), ViewVariables(VVAccess.ReadWrite)]
    public bool CanTeleportToOtherMaps = false;

    /// <summary>
    ///     Maximum distance that portals can teleport to, in all cases. Mostly this matters for linked portals.
    ///     Null means no restriction on distance.
    /// </summary>
    /// <remarks>
    ///     Obviously this should strictly be larger than <see cref="MaxRandomRadius"/> (or null)
    /// </remarks>
    [DataField("maxTeleportRadius"), ViewVariables(VVAccess.ReadWrite)]
    public float? MaxTeleportRadius;

    /// <summary>
    /// Should we teleport randomly if nothing is linked.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool RandomTeleport = true;
}
