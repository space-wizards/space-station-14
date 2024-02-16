using Content.Server.Explosion.EntitySystems;
using Content.Shared.Explosion;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Explosion.Components;

/// <summary>
/// Component that provides entities with explosion resistance.
/// By default this is applied when worn, but to solely protect the entity itself and
/// not the wearer use <c>worn: false</c>.
/// </summary>
/// <remarks>
///     This is desirable over just using damage modifier sets, given that equipment like bomb-suits need to
///     significantly reduce the damage, but shouldn't be silly overpowered in regular combat.
/// </remarks>
[RegisterComponent]
[Access(typeof(ExplosionSystem))]
public sealed partial class ExplosionResistanceComponent : Component
{
    /// <summary>
    ///     The explosive resistance coefficient, This fraction is multiplied into the total resistance.
    /// </summary>
    [DataField("damageCoefficient")]
    public float DamageCoefficient = 1;

    /// <summary>
    /// When true, resistances will be applied to the entity wearing this item.
    /// When false, only this entity will get th resistance.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Worn = true;

    /// <summary>
    /// Examine string for explosion resistance.
    /// Passed <c>value</c> from 0 to 100.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public LocId Examine = "explosion-resistance-coefficient-value";

    /// <summary>
    ///     Modifiers specific to each explosion type for more customizability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("modifiers", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, ExplosionPrototype>))]
    public Dictionary<string, float> Modifiers = new();
}
