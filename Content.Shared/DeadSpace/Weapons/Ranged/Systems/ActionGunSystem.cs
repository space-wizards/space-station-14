#pragma warning disable IDE0130
using Content.Shared.Actions.Components;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed partial class ActionGunSystem
{
    private void OnShoot(Entity<ActionGunComponent> ent, ref ActionGunShootEvent args)
    {
        if (ent.Comp.Gun is not { } gunUid || !TryComp<GunComponent>(gunUid, out var gun))
            return;

        args.Handled = _gun.AttemptShoot(ent, (gunUid, gun), args.Target, args.Entity);

        if (!args.Handled ||
            ent.Comp.ActionEntity is not { } actionUid ||
            !TryComp<ActionComponent>(actionUid, out var action) ||
            action.UseDelay != null ||
            !TryComp<RechargeBasicEntityAmmoComponent>(gunUid, out var recharge))
        {
            return;
        }

        _actions.SetUseDelay((actionUid, action), TimeSpan.FromSeconds(recharge.RechargeCooldown));
    }
}
