using Robust.Shared.Timing;
using Content.Shared.Trigger.Components;

namespace Content.Shared.Trigger.Systems;

public sealed class TwoStageTriggerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TwoStageTriggerComponent, TriggerEvent>(OnTrigger);
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

        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var enumerator = EntityQueryEnumerator<ActiveTwoStageTriggerComponent, TwoStageTriggerComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var component))
        {
            if (curTime < component.NextTriggerTime)
                continue;

            RemComp<ActiveTwoStageTriggerComponent>(uid);
            _triggerSystem.Trigger(uid, component.User, component.KeyOut);
        }
    }
}
