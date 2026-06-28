using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Bullets that do not travel in a constant velocity straight line.
/// </summary>
public partial class SharedGunSystem
{
    public void InitializeSpecial()
    {

    }

    private void UpdateAcceleratingProjectiles()
    {
        var query = EntityQueryEnumerator<AcceleratingProjectileComponent, ProjectileComponent>();

        while (query.MoveNext(out var uid, out var accProj, out var projectile))
        {
            var direction = (TransformSystem.GetWorldRotation(uid) - projectile.Angle).ToWorldVec();
            var currentSpeed = accProj.CurrentSpeed(Timing.CurTime);
            var velocity = direction * currentSpeed;

            Log.Debug("lord its physicsling");
            Physics.SetLinearVelocity(uid, velocity);

            if (accProj.DeletionTime(Timing.CurTime) && !Timing.InPrediction && !Timing.ApplyingState)
            {
                Log.Debug("fuicing diye");
                RemComp(uid, accProj);
            }
        }
    }

    public void UpdateSpecial(float dt)
    {
        UpdateAcceleratingProjectiles();
    }
}
