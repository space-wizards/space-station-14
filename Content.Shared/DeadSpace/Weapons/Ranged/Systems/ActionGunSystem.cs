#pragma warning disable IDE0130
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed partial class ActionGunSystem
{
    private void OnShoot(Entity<ActionGunComponent> ent, ref ActionGunShootEvent args)
    {
        if (TryComp<GunComponent>(ent.Comp.Gun, out var gun))
            _gun.AttemptShoot(ent, (ent.Comp.Gun.Value, gun), args.Target, args.Entity);
    }
}
