using Content.Shared.Chemistry.Reagent;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SolutionAppearanceRelay;

/// <summary>
/// Relays the visuals of a solution on this entity to the containing entity.
/// </summary>
[RegisterComponent]
public sealed partial class SolutionAppearanceRelayComponent : Component
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
}

[Serializable, NetSerializable]
public enum SolutionAppearanceRelayedVisuals : byte
{
    HasRelay
}
