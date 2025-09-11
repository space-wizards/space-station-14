using Content.Shared.Projectiles;
using Content.Shared.Cover.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Cover;

public sealed class SharedCoverSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _time = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoverComponent, ProjectileMissCoverAttemptEvent>(OnMissCover);
    }

    private void OnMissCover(Entity<CoverComponent> ent, ref ProjectileMissCoverAttemptEvent args)
    {
        if (TryMissCover(ent, args.Component, ent))
            args.Cancelled = true;
    }

    private bool TryMissCover(EntityUid projectile, ProjectileComponent comp, Entity<CoverComponent> cover)
    {
        // This naiive distance leaves open the possibility of some rediculous bullet curve shinanigans, but travel time counting is way more finnicky.
        var distance = comp.Origin != null ?
            Math.Clamp((comp.Origin.Value.Position - _xform.GetMapCoordinates(cover).Position).Length(), 0, cover.Comp.MaxDistance) :
            cover.Comp.MaxDistance;

        if (distance < cover.Comp.MinDistance) // we are too close and could shoot over easily
            return true;

        var coverPctAdjusted = 1 - (cover.Comp.MaxDistance - distance) / cover.Comp.MaxDistance;

        if (!_random.Prob(coverPctAdjusted)) // we are too far to reach over easily
        {
            // don't need to consider penetration. Things that pen can miss cover, or hit it and let pen figure it out.
            return true;
        }

        return false;
    }

}
