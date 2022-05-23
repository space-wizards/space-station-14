using Content.Server.Projectiles.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Weapon.Ranged;

public sealed class NewGunSystem : SharedNewGunSystem
{
    // TODO: Move to ballistic partial
    public override void ManualCycle(BallisticAmmoProviderComponent component, MapCoordinates coordinates)
    {
        EntityUid? ent = null;

        if (component.Cycled)
        {
            // TODO: Combine with TakeAmmo
            if (component.Entities.TryPop(out var existing))
            {
                component.Container.Remove(existing);
                EnsureComp<NewAmmoComponent>(existing);
            }
            else if (component.UnspawnedCount > 0)
            {
                component.UnspawnedCount--;
                ent = Spawn(component.FillProto, coordinates);
                EnsureComp<NewAmmoComponent>(ent.Value);
            }
        }

        component.Cycled = component.AutoCycle;

        if (ent != null)
        {
            EjectCartridge(ent.Value);
        }
    }

    public override void Shoot(List<IShootable> ammo, EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, EntityUid? user = null)
    {
        // TODO recoil / spread
        var direction = (toCoordinates.ToMapPos(EntityManager) - fromCoordinates.ToMapPos(EntityManager));

        // I must be high because this was getting tripped even when true.
        // DebugTools.Assert(direction != Vector2.Zero);

        foreach (var shootable in ammo)
        {
            switch (shootable)
            {
                // Cartridge shoots something itself
                case CartridgeAmmoComponent cartridge:
                    if (!cartridge.Spent)
                    {
                        var uid = Spawn(cartridge.Prototype, fromCoordinates);
                        ShootProjectile(uid, direction, user);

                        if (TryComp<AppearanceComponent>(cartridge.Owner, out var appearance))
                        {
                            appearance.SetData(AmmoVisuals.Spent, true);
                        }

                        cartridge.Spent = true;
                    }

                    EjectCartridge(cartridge.Owner);
                    Dirty(cartridge);
                    break;
                // Ammo shoots itself
                case NewAmmoComponent newAmmo:
                    ShootProjectile(newAmmo.Owner, direction, user);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void ShootProjectile(EntityUid uid, Vector2 direction, EntityUid? user = null)
    {
        var physics = EnsureComp<PhysicsComponent>(uid);
        physics.BodyStatus = BodyStatus.InAir;
        physics.LinearVelocity = direction.Normalized * 30f;

        if (user != null)
        {
            var projectile = EnsureComp<ProjectileComponent>(uid);
            projectile.IgnoreEntity(user.Value);
        }

        Transform(uid).WorldRotation = direction.ToWorldAngle();
    }

    protected override void PlaySound(NewGunComponent gun, string? sound, EntityUid? user = null)
    {
        if (sound == null) return;

        SoundSystem.Play(Filter.Pvs(gun.Owner).RemoveWhereAttachedEntity(e => e == user), sound, gun.Owner);
    }

    protected override void Popup(string message, NewGunComponent gun, EntityUid? user) {}
}
