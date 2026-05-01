using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared.SolutionAppearanceRelay;

/// <summary>
/// Relays the visuals of a solution on this entity to the containing entity.
/// </summary>
[RegisterComponent]
public sealed partial class SolutionAppearanceRelayComponent : Component
{
    /// <summary>
    /// The ID of solution on this entity to relay appearance to other entities.
    /// </summary>
    [DataField(required: true)]
    public string Solution;

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
}

[Serializable, NetSerializable]
public enum SolutionAppearanceRelayedVisuals : byte
{
    HasRelay
}
