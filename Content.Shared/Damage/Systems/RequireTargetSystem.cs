using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Damage.Components;

public sealed class RequireTargetSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RequireTargetComponent, PreventCollideEvent>(PreventCollide);
    }
    private void PreventCollide(Entity<RequireTargetComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
          return;

        var other = args.OtherEntity;
        if (HasComp<ProjectileComponent>(other) &&
            CompOrNull<TargetedProjectileComponent>(other)?.Target != ent.Owner)
        {
            args.Cancelled = true;
        }
    }
}
