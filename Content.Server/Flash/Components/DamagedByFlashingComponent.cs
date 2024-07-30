using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Server.Flash.Components;

[RegisterComponent, Access(typeof(DamagedByFlashingSystem))]
public sealed partial class DamagedByFlashingComponent : Component
{
    /// <summary>
    /// damage from flashing
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier FlashDamage = new();
}
