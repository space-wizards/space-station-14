using Content.Shared.Spawners.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Spawners.EntitySystems;

public abstract class SharedTimedDespawnSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
        SubscribeLocalEvent<TimedDespawnComponent, ComponentGetState>(OnDespawnGetState);
        SubscribeLocalEvent<TimedDespawnComponent, ComponentHandleState>(OnDespawnHandleState);
    }

    private void OnDespawnGetState(EntityUid uid, TimedDespawnComponent component, ref ComponentGetState args)
    {
        args.State = new TimedDespawnComponentState()
        {
            Lifetime = component.Lifetime,
        };
    }

    private void OnDespawnHandleState(EntityUid uid, TimedDespawnComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not TimedDespawnComponentState state)
            return;

        component.Lifetime = state.Lifetime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<TimedDespawnComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Lifetime -= frameTime;

            if (!CanDelete(uid))
                continue;

            if (comp.Lifetime <= 0)
            {
                var ev = new TimedDespawnEvent();
                RaiseLocalEvent(uid, ref ev);
                QueueDel(uid);
            }
        }
    }

    protected abstract bool CanDelete(EntityUid uid);

    [Serializable, NetSerializable]
    private sealed class TimedDespawnComponentState : ComponentState
    {
        public float Lifetime;
    }
}
