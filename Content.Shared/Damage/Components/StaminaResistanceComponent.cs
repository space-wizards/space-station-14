using Content.Shared.Damage.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Component that provides entities with stamina resistance.
/// By default this is applied when worn, but to solely protect the entity itself and
/// not the wearer use <c>worn: false</c>.
/// </summary>
/// <remarks>
/// This is desirable over just using damage modifier sets, given that equipment like bomb-suits need to
/// significantly reduce the damage, but shouldn't be silly overpowered in regular combat.
/// </remarks>
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
public sealed partial class StaminaResistanceComponent : Component
{
    /// <summary>
    /// The stamina resistance coefficient, This fraction is multiplied into the total resistance.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DamageCoefficient = 1;

    /// <summary>
    /// When true, resistances will be applied to the entity wearing this item.
    /// When false, only this entity will get the resistance.
    /// </summary>
    [DataField]
    public bool Worn = true;

    /// <summary>
    /// Examine string for stamina resistance.
    /// Passed <c>value</c> from 0 to 100.
    /// </summary>
    [DataField]
    public LocId Examine = "stamina-resistance-coefficient-value";
}
