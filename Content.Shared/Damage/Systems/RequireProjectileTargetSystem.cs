using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Standing;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Damage.Components;

public sealed class RequireProjectileTargetSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RequireProjectileTargetComponent, PreventCollideEvent>(PreventCollide);
        SubscribeLocalEvent<RequireProjectileTargetComponent, StoodEvent>(StandingBulletHit);
        SubscribeLocalEvent<RequireProjectileTargetComponent, DownedEvent>(LayingBulletPass);
    }

    private void PreventCollide(EntityUid uid, RequireProjectileTargetComponent component, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
          return;

        if (!component.Active)
            return;

        var other = args.OtherEntity;
        if (HasComp<ProjectileComponent>(other) &&
            CompOrNull<TargetedProjectileComponent>(other)?.Target != uid)
        {
            args.Cancelled = true;
        }
    }

    private void StandingBulletHit(EntityUid uid, RequireProjectileTargetComponent component, ref StoodEvent args)
    {
        component.Active = false;
    }

    private void LayingBulletPass(EntityUid uid, RequireProjectileTargetComponent component, ref DownedEvent args)
    {
        component.Active = true;
    }
}
