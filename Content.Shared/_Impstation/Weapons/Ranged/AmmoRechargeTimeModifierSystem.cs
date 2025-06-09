using Content.Shared._Impstation.Weapons.Ranged.Events;
using Content.Shared._Impstation.Weapons.Ranged.Upgrades.Components;

namespace Content.Shared._Impstation.Weapons.Ranged;

/// <summary>
/// This handles...
/// </summary>
public sealed class AmmoRechargeTimeModifierSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GunUpgradeAmmoRechargeTimeComponent, GetAmmoRechargeTimeEvent>(ModifyRechargeTime);
    }

    private void ModifyRechargeTime(Entity<GunUpgradeAmmoRechargeTimeComponent> ent, ref GetAmmoRechargeTimeEvent args)
    {
        args.Time *= ent.Comp.Coefficient;
    }
}
