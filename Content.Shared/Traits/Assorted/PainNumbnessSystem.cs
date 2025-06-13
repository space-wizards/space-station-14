using Content.Shared.Damage.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Events;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.Traits.Assorted;

public sealed class PainNumbnessSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PainNumbnessComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PainNumbnessComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<PainNumbnessComponent, BeforeForceSayEvent>(OnChangeForceSay);
        SubscribeLocalEvent<PainNumbnessComponent, BeforeAlertSeverityCheckEvent>(OnAlertSeverityCheck);
    }

    private void OnComponentRemove(EntityUid uid, PainNumbnessComponent component, ComponentRemove args)
    {
        if (!HasComp<MobThresholdsComponent>(uid))
            return;

        _mobThresholdSystem.VerifyThresholds(uid);
    }

    private void OnComponentInit(EntityUid uid, PainNumbnessComponent component, ComponentInit args)
    {
        if (!HasComp<MobThresholdsComponent>(uid))
            return;

        _mobThresholdSystem.VerifyThresholds(uid);
    }

    private void OnChangeForceSay(Entity<PainNumbnessComponent> ent, ref BeforeForceSayEvent args)
    {
        args.Prefix = ent.Comp.ForceSayNumbDataset;
    }

    private void OnAlertSeverityCheck(Entity<PainNumbnessComponent> ent, ref BeforeAlertSeverityCheckEvent args)
    {
        if (args.CurrentAlert == "HumanHealth")
            args.CancelUpdate = true;
    }
}
