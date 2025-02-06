using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Armor;

/// <summary>
/// Used for clothing that reduces damage when worn with specific helmet.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ArmorWithHelmetSystem))]
public sealed partial class ArmorWithHelmetComponent : Component
{
    /// <summary>
    /// The damage reduction
    /// </summary>
    [DataField(required: true)]
    public DamageModifierSet Modifiers = default!;
}
