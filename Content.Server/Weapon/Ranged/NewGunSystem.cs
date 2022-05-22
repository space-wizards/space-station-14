using Content.Server.Projectiles.Components;
using Content.Shared.Weapons.Ranged;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Weapon.Ranged;

public sealed class NewGunSystem : SharedNewGunSystem
{
    protected override void Shoot(EntityUid user, List<EntityUid> ammo, EntityCoordinates coordinates, float velocity)
    {
        List<Angle>? sprayAngleChange = null;
        if (ammo.Count > 1)
        {
            evenSpreadAngle *= component.SpreadRatio;
            sprayAngleChange = Linspace(-evenSpreadAngle / 2, evenSpreadAngle / 2, count);
        }

        foreach (var ent in ammo)
        {
            Angle projectileAngle;

            if (sprayAngleChange != null)
            {
                projectileAngle = angle + sprayAngleChange[i];
            }
            else
            {
                projectileAngle = angle;
            }

            var physics = EnsureComp<PhysicsComponent>(ent);
            physics.BodyStatus = BodyStatus.InAir;
            physics.LinearVelocity = projectileAngle.ToVec() * velocity;

            var projectile = EnsureComp<ProjectileComponent>(ent);
            projectile.IgnoreEntity(user);

            Transform(ent).WorldRotation = new Angle(projectileAngle.ToWorldVec());
        }
    }

    protected override void PlaySound(NewGunComponent gun, string? sound, EntityUid? user = null)
    {
        if (sound == null) return;

        SoundSystem.Play(Filter.Pvs(gun.Owner).RemoveWhereAttachedEntity(e => e == user), sound, gun.Owner);
    }

    protected override void Popup(string message, NewGunComponent gun, EntityUid? user) {}
}
