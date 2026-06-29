using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Flash.Components;

/// <summary>
/// This entity will take damage from flashes.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(DamagedByFlashingSystem))]
public sealed partial class DamagedByFlashingComponent : Component
{
    /// <summary>
    /// How much damage it will take.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier FlashDamage = new();
}
