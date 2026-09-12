using Content.Shared.Damage.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Traits.Assorted;

public sealed partial class PainNumbnessSystem : EntitySystem
{
    [Dependency] private MobThresholdSystem _threshold = default!;

    [SubscribeLocalEvent]
    private void OnEffectApplied(Entity<PainNumbnessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!HasComp<MobThresholdsComponent>(args.Target))
            return;

        _threshold.VerifyThresholds(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnEffectRemoved(Entity<PainNumbnessStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (!HasComp<MobThresholdsComponent>(args.Target))
            return;

        _threshold.VerifyThresholds(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnChangeForceSay(Entity<PainNumbnessStatusEffectComponent> ent, ref BeforeForceSayEvent args)
    {
        if (ent.Comp.ForceSayNumbDataset is { } dataset)
            args.Prefix = dataset;
    }

    [SubscribeLocalEvent]
    private void OnAlertSeverityCheck(Entity<PainNumbnessStatusEffectComponent> ent, ref BeforeAlertSeverityCheckEvent args)
    {
        if (args.CurrentAlert == "HumanHealth")
            args.CancelUpdate = true;
    }
}
