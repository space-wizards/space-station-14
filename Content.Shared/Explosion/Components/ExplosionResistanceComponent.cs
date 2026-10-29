using Content.Shared.Explosion.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Explosion.Components;

/// <summary>
/// Component that provides entities with explosion resistance.
/// By default this is applied when worn, but to solely protect the entity itself and
/// not the wearer use <c>worn: false</c>.
/// </summary>
/// <remarks>
///     This is desirable over just using damage modifier sets, given that equipment like bomb-suits need to
///     significantly reduce the damage, but shouldn't be silly overpowered in regular combat.
/// </remarks>
[NetworkedComponent, RegisterComponent]
[Access(typeof(SharedExplosionSystem))]
public sealed partial class ExplosionResistanceComponent : Component
{
    /// <summary>
    ///     The explosive resistance coefficient, This fraction is multiplied into the total resistance.
    /// </summary>
    [DataField]
    public float DamageCoefficient = 1;

    /// <summary>
    /// When true, resistances will be applied to the entity wearing this item.
    /// When false, only this entity will get th resistance.
    /// </summary>
    [DataField]
    public bool Worn = true;

    /// <summary>
    /// Examine string for explosion resistance.
    /// Passed <c>value</c> from 0 to 100.
    /// </summary>
    [DataField]
    public LocId Examine = "explosion-resistance-coefficient-value";

    /// <summary>
    ///     Modifiers specific to each explosion type for more customizability.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ExplosionPrototype>, float> Modifiers = new();
}
