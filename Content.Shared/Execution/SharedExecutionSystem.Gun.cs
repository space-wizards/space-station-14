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
        if (args.Handled)
            return;

        args.Sound = weapon.Comp.SoundEmpty;
        args.Handled = true;

        // if we early return here the execution will make an empty gun click and do no damage

        var fromCoordinates = Transform(args.Attacker).Coordinates;
        // take ammo will handle expending the ammo for us
        var ammoEv = new TakeAmmoEvent(1, [], fromCoordinates, args.Attacker);
        RaiseLocalEvent(weapon.Owner, ammoEv);

        // did we get an IShootable from the gun?
        DebugTools.Assert(ammoEv.Ammo.Count >= 0);
        if (ammoEv.Ammo.Count == 0)
            return;
        DebugTools.Assert(ammoEv.Ammo.Count == 1);

        // if we have an ammo we will shoot so set the sound now
        args.Sound = weapon.Comp.SoundGunshot;
        _gun.UpdateAmmoCount(weapon.Owner);

        var (shootEntity, shootable) = ammoEv.Ammo[0];
        if (shootEntity is null)
            return;

        // rather not nice pattern matching on IShootable
        // but that's just how the gun system is
        if (shootable is CartridgeAmmoComponent cartridge)
        {
            HandleCartridge(shootEntity.Value, cartridge, ref args);
        }
        else if (shootable is AmmoComponent)
        {
            HandleProjectile(shootEntity.Value, ref args);
        }
        else if (shootable is HitscanAmmoComponent)
        {
            HandleHitscan(shootEntity.Value, ref args);
        }
    }

    private void HandleProjectile(EntityUid entity, ref BeforeExecutionEvent args)
    {
        if (TryComp<ProjectileComponent>(entity, out var projectile))
            args.Damage = projectile.Damage;
        if (HasComp<StaminaDamageOnCollideComponent>(entity))
            args.Stamcrit = true;

        var instakillEv = new AttemptExecutionInstakillEvent();
        RaiseLocalEvent(entity, ref instakillEv);
        if (instakillEv.Cancelled)
            args.Instakill = false;

        // we don't actually want to shoot anything - don't forget to clean up
        Del(entity);
    }

    private void HandleCartridge(EntityUid entity, CartridgeAmmoComponent cartridge, ref BeforeExecutionEvent args)
    {
        var bullet = Spawn(cartridge.Prototype);
        HandleProjectile(bullet, ref args);
        _gun.SetCartridgeSpent(entity, cartridge, true);
    }

    private void HandleHitscan(EntityUid entity, ref BeforeExecutionEvent args)
    {
        if (!TryComp<HitscanBasicDamageComponent>(entity, out var hitscanBasicDamage))
            return;
        args.Damage = hitscanBasicDamage.Damage;
    }
}

[ByRefEvent]
public record struct AttemptExecutionInstakillEvent(bool Cancelled = false);
