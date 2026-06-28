using System.Numerics;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using JetBrains.Annotations;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Bullets that do not travel in a constant velocity straight line.
/// </summary>
public partial class SharedGunSystem
{
    public bool AcceleratingDone(AcceleratingProjectileComponent comp)
    {
        return Timing.CurTime > comp.FireTime + comp.FullAccelerationTime;
    }

    private void UpdateAcceleratingProjectiles(float deltaTime)
    {
        var query = EntityQueryEnumerator<AcceleratingProjectileComponent, ProjectileComponent>();

        while (query.MoveNext(out var uid, out var accProj, out var projectile))
        {
            if (AcceleratingDone(accProj))
            {
                continue;
            }

            var currentVelocity = Physics.GetLinearVelocity(uid, Vector2.Zero);
            var direction = (TransformSystem.GetWorldRotation(uid) - projectile.Angle).ToWorldVec();
            var accelerationTarget = Math.Min(deltaTime,
                (float)(accProj.FullAccelerationTime - (Timing.CurTime - accProj.FireTime)).TotalSeconds);

            var deltaVelocity = direction * accelerationTarget;

            Physics.SetLinearVelocity(uid, currentVelocity + deltaVelocity);
        }
    }

    public void UpdateSpecial(float deltaTime)
    {
        UpdateAcceleratingProjectiles(deltaTime);
    }
}
