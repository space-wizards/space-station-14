using Content.Server.Gatherable.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Physics.Events;

namespace Content.Server.Gatherable;

public sealed partial class GatherableSystem
{
    private void InitializeProjectile()
    {
        SubscribeLocalEvent<GatheringProjectileComponent, StartCollideEvent>(OnProjectileCollide);
        SubscribeLocalEvent<GatheringProjectileComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnProjectileCollide(Entity<GatheringProjectileComponent> gathering, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard ||
            args.OurFixtureId != SharedProjectileSystem.ProjectileFixture ||
            gathering.Comp.Amount <= 0 ||
            !TryComp<GatherableComponent>(args.OtherEntity, out var gatherable))
        {
            return;
        }

        Gather(args.OtherEntity, gathering, gatherable);
        gathering.Comp.Amount--;

        if (gathering.Comp.Amount <= 0)
            QueueDel(gathering);
    }

    // DS14-start: hitscan emitter bolts still gather the same targets as physical gathering projectiles.
    private void OnHitscanHit(Entity<GatheringProjectileComponent> gathering, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity is not { } hit ||
            gathering.Comp.Amount <= 0 ||
            !TryComp<GatherableComponent>(hit, out var gatherable))
        {
            return;
        }

        Gather(hit, gathering, gatherable);
        gathering.Comp.Amount--;

        if (gathering.Comp.Amount <= 0)
            QueueDel(gathering);
    }
    // DS14-end
}
