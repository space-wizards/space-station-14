using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

/// <summary>
/// Configures portal destinations linked through <see cref="LinkedEntityComponent"/>.
/// Without links, <see cref="RandomTeleport"/> allows a random nearby destination.
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

    /// <summary>
    /// Limits users of a portal with no linked destinations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? UnlinkedUserWhitelist;

    /// <summary>
    /// Blocks matching users when the portal has no linked destinations.
    /// Takes priority over <see cref="UnlinkedUserWhitelist"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? UnlinkedUserBlacklist;
}
