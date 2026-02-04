using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class ExplodeOnTriggerSystem : XOnTriggerSystem<ExplodeOnTriggerComponent>
{
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;

    protected override void OnTrigger(Entity<ExplodeOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _explosion.TriggerExplosive(target, user: args.User);
        args.Handled = true;
    }
}

public sealed class ExplosionOnTriggerSystem : XOnTriggerSystem<ExplosionOnTriggerComponent>
{
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;

    protected override void OnTrigger(Entity<ExplosionOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _explosion.QueueExplosion(target,
            ent.Comp.ExplosionType,
            ent.Comp.TotalIntensity,
            ent.Comp.IntensitySlope,
            ent.Comp.MaxTileIntensity,
            ent.Comp.TileBreakScale,
            ent.Comp.MaxTileBreak,
            ent.Comp.CanCreateVacuum,
            args.User);
        args.Handled = true;
    }
}
