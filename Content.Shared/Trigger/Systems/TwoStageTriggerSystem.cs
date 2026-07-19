using Robust.Shared.Timing;
using Content.Shared.Trigger.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TwoStageTriggerSystem : EntitySystem
{
    private static readonly EntityTimerId StageTwoTimer = new("stage-two");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TriggerSystem _triggerSystem = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TwoStageTriggerComponent, TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<TwoStageTriggerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<TwoStageTriggerComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnTrigger(Entity<TwoStageTriggerComponent> ent, ref TriggerEvent args)
    {
        if (ent.Comp.Triggered)
            return; // already triggered

        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        EntityManager.AddComponents(ent, ent.Comp.Components);
        EnsureComp<ActiveTwoStageTriggerComponent>(ent);
        ent.Comp.Triggered = true;
        ent.Comp.NextTriggerTime = _timing.CurTime + ent.Comp.TriggerDelay;
        ent.Comp.User = args.User;
        Dirty(ent);
        _timers.SetTimerAt(ent, StageTwoTimer, ent.Comp.NextTriggerTime.Value);

        args.Handled = true;
    }

    private void OnHandleState(Entity<TwoStageTriggerComponent> ent, ref ComponentHandleState args)
    {
        if (ent.Comp.NextTriggerTime is {} deadline && HasComp<ActiveTwoStageTriggerComponent>(ent))
            _timers.SetTimerAt(ent, StageTwoTimer, deadline);
        else
            _timers.CancelTimer<TwoStageTriggerComponent>(ent, StageTwoTimer);
    }

    private void OnTimer(Entity<TwoStageTriggerComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != StageTwoTimer || !RemComp<ActiveTwoStageTriggerComponent>(ent))
            return;

        _triggerSystem.Trigger(ent, ent.Comp.User, ent.Comp.KeyOut);
    }
}
