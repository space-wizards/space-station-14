using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

/// <summary>
/// Resolves teleport requests to destinations linked through <see cref="LinkedEntityComponent"/>.
/// When no destinations are linked, can choose a random nearby destination if <see cref="RandomTeleport"/> is enabled.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PortalComponent : Component
{
    /// <summary>
    ///     If no portals are linked, the subject will be teleported a random distance at maximum this far away.
    /// </summary>
    [DataField]
    public float MaxRandomRadius = 7.0f;

    /// <summary>
    ///     If false, this portal will fail to teleport and fizzle out if attempting to send an entity to a different map
    /// </summary>
    /// <remarks>
    ///     Shouldn't be able to teleport people to centcomm or the eshuttle from the station
    /// </remarks>
    [DataField]
    public bool CanTeleportToOtherMaps = false;

    /// <summary>
    ///     Maximum distance that portals can teleport to, in all cases. Mostly this matters for linked portals.
    ///     Null means no restriction on distance.
    /// </summary>
    /// <remarks>
    ///     Obviously this should strictly be larger than <see cref="MaxRandomRadius"/> (or null)
    /// </remarks>
    [DataField]
    public float? MaxTeleportRadius;

    /// <summary>
    /// Should we teleport randomly if nothing is linked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RandomTeleport = true;
}
