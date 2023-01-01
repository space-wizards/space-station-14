using Content.Shared.Teleportation.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

/// <summary>
///     Marks an entity as being a 'portal' which teleports entities sent through it to linked entities.
///     Relies on <see cref="LinkedEntityComponent"/> being set up.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(PortalSystem))]
public sealed class PortalComponent : Component
{
    /// <summary>
    ///     If no portals are linked, the subject will be teleported a random distance at maximum this far away.
    /// </summary>
    [DataField("MaxRandomRadius")]
    public float MaxRandomRadius = 15.0f;
}
