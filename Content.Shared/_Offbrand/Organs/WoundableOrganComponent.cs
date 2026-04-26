using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.Organs;

[RegisterComponent, NetworkedComponent]
public sealed partial class WoundableOrganComponent : Component
{
    /// <summary>
    /// The weight this organ has in being targeted by an attack on its body.
    /// </summary>
    [DataField]
    public float Weight;
}

/// <summary>
/// Raised on a body to get a by-weight dictionary of woundable organs
/// </summary>
[ByRefEvent]
public readonly record struct WoundableOrganWeightsEvent(Dictionary<Entity<WoundableOrganComponent>, float> Weights);
