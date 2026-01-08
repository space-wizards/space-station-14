using Content.Shared.Projectiles;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Execution;

public sealed partial class SharedExecutionSystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;

    private void InitialiseGun()
    {
        SubscribeLocalEvent<GunComponent, BeforeExecutionEvent>(OnBeforeExecutionGun);
    }

    private void OnBeforeExecutionGun(Entity<GunComponent> weapon, ref BeforeExecutionEvent args)
    {
        args.Sound = weapon.Comp.SoundEmpty;
        args.Handled = true;

        // if we early return here the execution will make an empty gun click and do no damage

        var fromCoordinates = Transform(args.Attacker).Coordinates;
        // take ammo will handle expending the ammo for us
        var ev = new TakeAmmoEvent(1, [], fromCoordinates, args.Attacker);
        RaiseLocalEvent(weapon.Owner, ev);

        // did we get an IShootable from the gun?
        DebugTools.Assert(ev.Ammo.Count >= 0);
        if (ev.Ammo.Count == 0)
            return;
        DebugTools.Assert(ev.Ammo.Count == 1);
        var (shootEntity, shootable) = ev.Ammo[0];

        // rather not nice pattern matching on IShootable
        // but that's just how the gun system is

        if (shootable is CartridgeAmmoComponent cartridge)
        {
            var bullet = Spawn(cartridge.Prototype);
            if (!TryComp<ProjectileComponent>(bullet, out var projectile))
                return;
            args.Damage = projectile.Damage;
            args.Sound = weapon.Comp.SoundGunshot;
            Del(bullet);
        }
        else if (shootable is HitscanAmmoComponent)
        {
            if (!TryComp<HitscanBasicDamageComponent>(shootEntity, out var hitscanBasicDamage))
                return;
            args.Damage = hitscanBasicDamage.Damage;
            args.Sound = weapon.Comp.SoundGunshot;
        }
    }
}
