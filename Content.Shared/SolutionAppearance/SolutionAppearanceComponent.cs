using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.SolutionAppearance;

/// <summary>
/// Used to relay the visuals of a solution to the containing entity. Using <see cref="SolutionItemSlotAppearanceSystem" />
/// </summary>
[RegisterComponent]
public sealed partial class SolutionAppearanceComponent : Component
{
    /// <summary>
    /// Whitelist for entities that the solution appearance will be relayed to.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist for entities that the solution appearance will be relayed to.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Cached container of the entity that the solution appearance will be relayed to.
    /// </summary>
    public BaseContainer? CachedContainer;
}

[Serializable, NetSerializable]
public enum SolutionAppearanceRelayedVisuals : byte
{
    HasRelay
}
