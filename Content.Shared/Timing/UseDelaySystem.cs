using Content.Shared.Cooldown;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Timing;

public sealed class UseDelaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly HashSet<Entity<UseDelayComponent>> _activeDelays = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseDelayComponent, AfterAutoHandleStateEvent>(OnHandleState);

        SubscribeLocalEvent<UseDelayComponent, EntityPausedEvent>(OnPaused);
        SubscribeLocalEvent<UseDelayComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnPaused(Entity<UseDelayComponent> delay, ref EntityPausedEvent args)
    {
        // This entity just got paused, but wasn't before
        if (delay.Comp.DelayEndTime != null)
            delay.Comp.RemainingDelay = _gameTiming.CurTime - delay.Comp.DelayEndTime;

        _activeDelays.Remove(delay);
        Dirty(delay);
    }

    private void OnUnpaused(Entity<UseDelayComponent> delay, ref EntityUnpausedEvent args)
    {
        if (delay.Comp.RemainingDelay == null)
            return;

        // We got unpaused, resume the delay/cooldown. Currently this takes for granted that ItemCooldownComponent
        // handles the pausing on its own. I'm not even gonna check, because I CBF fixing it if it doesn't.
        delay.Comp.DelayEndTime = _gameTiming.CurTime + delay.Comp.RemainingDelay;
        Dirty(delay);
        _activeDelays.Add(delay);
    }

    private void OnHandleState(Entity<UseDelayComponent> delay, ref AfterAutoHandleStateEvent args)
    {
        if (delay.Comp.DelayEndTime == null)
            _activeDelays.Remove(delay);
        else
            _activeDelays.Add(delay);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toRemove = new RemQueue<Entity<UseDelayComponent>>();
        var curTime = _gameTiming.CurTime;
        var mQuery = EntityManager.GetEntityQuery<MetaDataComponent>();

        // TODO refactor this to use active components
        foreach (var delay in _activeDelays)
        {
            if (delay.Comp.DelayEndTime == null ||
                curTime > delay.Comp.DelayEndTime ||
                Deleted(delay.Owner, mQuery))
            {
                toRemove.Add(delay);
            }
        }

        foreach (var delay in toRemove)
        {
            delay.Comp.DelayEndTime = null;
            _activeDelays.Remove(delay);
            Dirty(delay);
        }
    }

    public void BeginDelay(EntityUid uid, UseDelayComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.ActiveDelay)
            return;

        var delay = new Entity<UseDelayComponent>(uid, component);
        DebugTools.Assert(!_activeDelays.Contains(delay));
        _activeDelays.Add(delay);

        var currentTime = _gameTiming.CurTime;
        component.LastUseTime = currentTime;
        component.DelayEndTime = currentTime + component.Delay;
        Dirty(delay);

        // TODO just merge these components?
        var cooldown = EnsureComp<ItemCooldownComponent>(delay);
        cooldown.CooldownStart = currentTime;
        cooldown.CooldownEnd = component.DelayEndTime;
    }

    public bool ActiveDelay(EntityUid uid, UseDelayComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.ActiveDelay;
    }

    public void Cancel(Entity<UseDelayComponent> delay)
    {
        delay.Comp.DelayEndTime = null;
        _activeDelays.Remove(delay);
        Dirty(delay);

        if (TryComp<ItemCooldownComponent>(delay.Owner, out var cooldown))
        {
            cooldown.CooldownEnd = _gameTiming.CurTime;
        }
    }
}
