using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared._Offbrand.Wounds;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Organs;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WoundableOrganSystem), Other = AccessPermissions.ReadExecute)]
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
