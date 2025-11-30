using Content.Shared.Damage.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Traits.Assorted;

public sealed class PainNumbnessSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PainNumbnessComponent, StatusEffectAppliedEvent>(OnEffectApplied);
        SubscribeLocalEvent<PainNumbnessComponent, StatusEffectRemovedEvent>(OnEffectRemoved);
        SubscribeLocalEvent<PainNumbnessComponent, StatusEffectRelayedEvent<BeforeForceSayEvent>>(OnChangeForceSay);
        SubscribeLocalEvent<PainNumbnessComponent, StatusEffectRelayedEvent<BeforeAlertSeverityCheckEvent>>(OnAlertSeverityCheck);
    }

    private void OnEffectApplied(EntityUid uid, PainNumbnessComponent component, StatusEffectAppliedEvent args)
    {
        if (!HasComp<MobThresholdsComponent>(args.Target))
            return;

        _mobThresholdSystem.VerifyThresholds(args.Target);
    }

    private void OnEffectRemoved(EntityUid uid, PainNumbnessComponent component, StatusEffectRemovedEvent args)
    {
        if (!HasComp<MobThresholdsComponent>(args.Target))
            return;

        _mobThresholdSystem.VerifyThresholds(args.Target);
    }

    private void OnChangeForceSay(Entity<PainNumbnessComponent> ent, ref StatusEffectRelayedEvent<BeforeForceSayEvent> args)
    {
        if (ent.Comp.ForceSayNumbDataset != null)
            args.Args.Prefix = ent.Comp.ForceSayNumbDataset.Value;
    }

    private void OnAlertSeverityCheck(Entity<PainNumbnessComponent> ent, ref StatusEffectRelayedEvent<BeforeAlertSeverityCheckEvent> args)
    {
        if (args.Args.CurrentAlert == "HumanHealth")
            args.Args.CancelUpdate = true;
    }
}
