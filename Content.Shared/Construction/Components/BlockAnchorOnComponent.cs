using Content.Shared.Construction.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Construction.Components;

/// <summary>
///     Will not allow anchoring if there is an anchored item in the same tile that fails the whitelist checks.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(BlockAnchorOnSystem))]
public sealed partial class BlockAnchorOnComponent : Component
{
    /// <summary>
    ///     Entities that match this whitelist are allowed (If null, ignore)
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    ///     Entities that match this blacklist are not allowed (If null, ignore)
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
