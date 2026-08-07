using Content.Server.Physics.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Random;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    [Dependency] private IRobustRandom _random = default!;

    [SubscribeLocalEvent]
    public void OnProjectileShot(Entity<TargetAssignComponent> entity, ref AmmoShotEvent args)
    {
        if (entity.Comp.Target is null)
            return;

        if (args.FiredProjectiles.Count == 0)
            return;

        foreach (var projectile in args.FiredProjectiles)
        {
            if (!TryComp<ChasingWalkComponent>(projectile, out var projectileChasingWalkComp))
                continue;

            projectileChasingWalkComp.ChasingEntity = entity.Comp.Target;
            projectileChasingWalkComp.NextChangeVectorTime = TimeSpan.MaxValue;

            var targetedProjectile = EnsureComp<TargetedProjectileComponent>(projectile);
            targetedProjectile.Target = entity.Comp.Target.Value;
        }
    }
}
