using Content.Shared.Trigger.Components;
using Robust.Shared.Timing;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Popups;
namespace Content.Shared.Trigger.Systems;

public sealed class MultiTimerQueueSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MultiTimerQueueComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<MultiTimerQueueComponent> ent, ref TriggerEvent args)
    {
        if(args.Key == null || !ent.Comp.KeysIn.Contains(args.Key))
            return;
        if (!_entityManager.TryGetComponent(
                ent, out TimerTriggerComponent? timerTrigger))
            return;
        if (ent.Comp.Queue.Count == 0)
        {
            _entityManager.RemoveComponent<MultiTimerQueueComponent>(ent.Owner);
            return;
        }
        var key = ent.Comp.Queue.Dequeue();
        timerTrigger.KeyOut = key;
        if (ent.Comp.QueueDelays.Count != 0)
        {
            var delay = ent.Comp.QueueDelays.Dequeue();
            if (delay != TimeSpan.Zero)
                timerTrigger.Delay = delay;
        }
        _triggerSystem.RestartTimer(
            new Entity<TimerTriggerComponent>(ent.Owner, timerTrigger));
        if (ent.Comp.Queue.Count == 0)
            _entityManager.RemoveComponent<MultiTimerQueueComponent>(ent.Owner);
    }
}
