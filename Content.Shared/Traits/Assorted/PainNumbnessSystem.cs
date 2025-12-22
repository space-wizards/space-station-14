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
        SubscribeLocalEvent<PainNumbnessStatusEffectComponent, StatusEffectAppliedEvent>(OnEffectApplied);
        SubscribeLocalEvent<PainNumbnessStatusEffectComponent, StatusEffectRemovedEvent>(OnEffectRemoved);
        SubscribeLocalEvent<PainNumbnessStatusEffectComponent, StatusEffectRelayedEvent<BeforeForceSayEvent>>(OnChangeForceSay);
        SubscribeLocalEvent<PainNumbnessStatusEffectComponent, StatusEffectRelayedEvent<BeforeAlertSeverityCheckEvent>>(OnAlertSeverityCheck);
    }

    private void OnEffectApplied(Entity<PainNumbnessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!HasComp<MobThresholdsComponent>(args.Target))
            return;

        _mobThresholdSystem.VerifyThresholds(args.Target);
    }

    private void OnEffectRemoved(Entity<PainNumbnessStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (!HasComp<MobThresholdsComponent>(args.Target))
            return;

        _mobThresholdSystem.VerifyThresholds(args.Target);
    }

    private void OnChangeForceSay(Entity<PainNumbnessStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BeforeForceSayEvent> args)
    {
        if (ent.Comp.ForceSayNumbDataset != null)
            args.Args.Prefix = ent.Comp.ForceSayNumbDataset.Value;
    }

    private void OnAlertSeverityCheck(Entity<PainNumbnessStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BeforeAlertSeverityCheckEvent> args)
    {
        if (args.Args.CurrentAlert == "HumanHealth")
            args.Args.CancelUpdate = true;
    }
}
