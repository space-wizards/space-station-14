using Content.Shared.Damage.Components;
using Content.Shared.Magic.Events;
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
        SubscribeLocalEvent<ForbidExecutionInstakillComponent, AttemptExecutionInstakillEvent>(OnAttemptInstakill);
    }

    private void OnAttemptInstakill(Entity<ForbidExecutionInstakillComponent> entity, ref AttemptExecutionInstakillEvent args)
    {
        args.Cancelled = true;
    }

    private void OnBeforeExecutionGun(Entity<GunComponent> weapon, ref BeforeExecutionEvent args)
    {
        args.Sound = weapon.Comp.SoundEmpty;
        args.Handled = true;

        // if we early return here the execution will make an empty gun click and do no damage

        var fromCoordinates = Transform(args.Attacker).Coordinates;
        // take ammo will handle expending the ammo for us
        var ammoEv = new TakeAmmoEvent(1, [], fromCoordinates, args.Attacker);
        RaiseLocalEvent(weapon.Owner, ammoEv);
        _gun.UpdateAmmoCount(weapon.Owner);

        // did we get an IShootable from the gun?
        DebugTools.Assert(ammoEv.Ammo.Count >= 0);
        if (ammoEv.Ammo.Count == 0)
            return;
        DebugTools.Assert(ammoEv.Ammo.Count == 1);
        var (shootEntity, shootable) = ammoEv.Ammo[0];

        // rather not nice pattern matching on IShootable
        // but that's just how the gun system is

        // we are a cartridge that shoots a projectile
        if (shootable is CartridgeAmmoComponent cartridge)
        {
            var bullet = Spawn(cartridge.Prototype);

            TryComp<ProjectileComponent>(bullet, out var projectile);
            var projectileDamage = projectile?.Damage;

            if (HasComp<StaminaDamageOnCollideComponent>(bullet))
                args.Stamcrit = true;

            var instakillEv = new AttemptExecutionInstakillEvent();
            RaiseLocalEvent(bullet, ref instakillEv);
            if (instakillEv.Cancelled)
                args.Instakill = false;

            Del(bullet);

            if (shootEntity is null || projectileDamage is null)
                return;

            args.Damage = projectileDamage;
            args.Sound = weapon.Comp.SoundGunshot;
            // don't forget to set the cartridge as spent
            _gun.SetCartridgeSpent(shootEntity.Value, cartridge, true);
        }
        // we are an actual projectile
        else if (shootable is AmmoComponent)
        {
            if (!TryComp<ProjectileComponent>(shootEntity, out var projectile))
                return;

            if (HasComp<StaminaDamageOnCollideComponent>(shootEntity))
                args.Stamcrit = true;

            var instakillEv = new AttemptExecutionInstakillEvent();
            RaiseLocalEvent(shootEntity.Value, ref instakillEv);
            if (instakillEv.Cancelled)
                args.Instakill = false;

            args.Damage = projectile.Damage;
            args.Sound = weapon.Comp.SoundGunshot;
            // don't forget to delete the projectile
            Del(shootEntity);
        }
        // we are a hitscan
        else if (shootable is HitscanAmmoComponent)
        {
            if (!TryComp<HitscanBasicDamageComponent>(shootEntity, out var hitscanBasicDamage))
                return;
            args.Damage = hitscanBasicDamage.Damage;
            args.Sound = weapon.Comp.SoundGunshot;
        }
    }
}

[ByRefEvent]
public record struct AttemptExecutionInstakillEvent(bool Cancelled = false);
