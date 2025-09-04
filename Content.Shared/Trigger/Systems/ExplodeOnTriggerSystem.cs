using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class ExplodeOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExplodeOnTriggerComponent, TriggerEvent>(OnExplodeTrigger);
        SubscribeLocalEvent<ExplosionOnTriggerComponent, TriggerEvent>(OnQueueExplosionTrigger);
    }

    private void OnExplodeTrigger(Entity<ExplodeOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        _explosion.TriggerExplosive(target.Value, user: args.User);
        args.Handled = true;
    }

    private void OnQueueExplosionTrigger(Entity<ExplosionOnTriggerComponent> ent, ref TriggerEvent args)
    {
        var (uid, comp) = ent;
        if (args.Key != null && !comp.KeysIn.Contains(args.Key))
            return;

        var target = comp.TargetUser ? args.User : uid;

        if (target == null)
            return;

        _explosion.QueueExplosion(target.Value,
                                    comp.ExplosionType,
                                    comp.TotalIntensity,
                                    comp.IntensitySlope,
                                    comp.MaxTileIntensity,
                                    comp.TileBreakScale,
                                    comp.MaxTileBreak,
                                    comp.CanCreateVacuum,
                                    args.User);
        args.Handled = true;
    }
}
