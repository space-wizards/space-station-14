using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.Organs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class WoundableOrganComponent : Component
{
    /// <summary>
    /// The weight this organ has in being targeted by an attack on its body.
    /// </summary>
    [DataField]
    public float Weight;

    /// <summary>
    /// The total damages accumulated on this organ.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();
}

/// <summary>
/// Raised on a body to get a by-weight dictionary of woundable organs
/// </summary>
[ByRefEvent]
public readonly record struct WoundableOrganWeightsEvent(Dictionary<Entity<WoundableOrganComponent>, float> Weights);

/// <summary>
/// Raised on an organ when its wound damage changes
/// </summary>
[ByRefEvent]
public readonly record struct WoundableOrganDamageChanged(DamageSpecifier NewDamage);
