using Content.Server.Physics.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Random;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    [SubscribeLocalEvent]
    public void OnProjectileShot(Entity<TargetAssignComponent> entity, ref AmmoShotEvent args)
    {
        if (entity.Comp.Target is null || TerminatingOrDeleted(entity.Comp.Target))
            return;

        if (args.FiredProjectiles.Count == 0)
            return;

        foreach (var projectile in args.FiredProjectiles)
        {
            var projectileChasingWalkComp = Comp<ChasingWalkComponent>(projectile);

            projectileChasingWalkComp.ChasingEntity = entity.Comp.Target;
            projectileChasingWalkComp.NextChangeVectorTime = TimeSpan.MaxValue;

            var targetedProjectile = EnsureComp<TargetedProjectileComponent>(projectile);
            targetedProjectile.Target = entity.Comp.Target.Value;
        }
    }
}
