using Content.Shared.Chemistry.Reagent;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Shared._Starlight.Chemistry.Components;

[RegisterComponent]
public sealed partial class InjectOnCollideComponent : Component
{
    /// <summary>
    /// Reagent(s) to inject on collision.
    /// </summary>
    [DataField("reagents")]
    public List<ReagentQuantity> Reagents;

    /// <summary>
    /// Limit of reagents that can be injected. When null, no limit is applied.
    /// </summary>
    [DataField("limit")]
    public float? ReagentLimit;

    /// <summary>
    /// A blacklist of entities that should be ignored by this component's speed modifiers.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
    
    
    /// <summary>
    /// A whitelist of entities that should be targeted by this component's speed modifiers.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;
}