using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee;

/// <summary>
/// Applies an entity effect on hit entities
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMeleeWeaponSystem))]
public sealed partial class EntityEffectMeleeComponent : Component
{
    /// <summary>
    /// List of entity effects to apply to hit entities.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects;
}
