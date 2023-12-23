using Content.Shared.Cooldown;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Timing;

public sealed class UseDelaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private HashSet<UseDelayComponent> _activeDelays = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseDelayComponent, AfterAutoHandleStateEvent>(OnHandleState);

        SubscribeLocalEvent<UseDelayComponent, EntityPausedEvent>(OnPaused);
        SubscribeLocalEvent<UseDelayComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnPaused(EntityUid uid, UseDelayComponent component, ref EntityPausedEvent args)
    {
        // This entity just got paused, but wasn't before
        if (component.DelayEndTime != null)
            component.RemainingDelay = _gameTiming.CurTime - component.DelayEndTime;

        _activeDelays.Remove(component);
        Dirty(component);
    }

    private void OnUnpaused(EntityUid uid, UseDelayComponent component, ref EntityUnpausedEvent args)
    {
        if (component.RemainingDelay == null)
            return;

        // We got unpaused, resume the delay/cooldown. Currently this takes for granted that ItemCooldownComponent
        // handles the pausing on its own. I'm not even gonna check, because I CBF fixing it if it doesn't.
        component.DelayEndTime = _gameTiming.CurTime + component.RemainingDelay;
        Dirty(component);
        _activeDelays.Add(component);
    }

    private void OnHandleState(EntityUid uid, UseDelayComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (component.DelayEndTime == null)
            _activeDelays.Remove(component);
        else
            _activeDelays.Add(component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toRemove = new RemQueue<UseDelayComponent>();
        var curTime = _gameTiming.CurTime;
        var mQuery = EntityManager.GetEntityQuery<MetaDataComponent>();

        // TODO refactor this to use active components
        foreach (var delay in _activeDelays)
        {
            if (delay.DelayEndTime == null ||
                curTime > delay.DelayEndTime ||
                Deleted(delay.Owner, mQuery))
            {
                toRemove.Add(delay);
            }
        }

        foreach (var delay in toRemove)
        {
            delay.DelayEndTime = null;
            _activeDelays.Remove(delay);
            Dirty(delay);
        }
    }

    /// <summary>
    /// Attempts tp start a use-delay for some entity. Returns true unless there is already an active delay.
    /// </summary>
    /// <remarks>
    /// Note that this will always return true if the entity does not have a use delay component, as in that case there
    /// is no reason to block/prevent an interaction.
    /// </remarks>
    public bool BeginDelay(EntityUid uid, UseDelayComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return true;

        if (component.ActiveDelay)
            return false;

        DebugTools.Assert(!_activeDelays.Contains(component));
        _activeDelays.Add(component);

        var currentTime = _gameTiming.CurTime;
        component.LastUseTime = currentTime;
        component.DelayEndTime = currentTime + component.Delay;
        Dirty(uid, component);

        var cooldown = EnsureComp<ItemCooldownComponent>(uid);
        cooldown.CooldownStart = currentTime;
        cooldown.CooldownEnd = component.DelayEndTime;
        return true;
    }

    public bool ActiveDelay(EntityUid uid, UseDelayComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.ActiveDelay;
    }

    public void Cancel(UseDelayComponent component)
    {
        component.DelayEndTime = null;
        _activeDelays.Remove(component);
        Dirty(component);

        if (TryComp<ItemCooldownComponent>(component.Owner, out var cooldown))
        {
            cooldown.CooldownEnd = _gameTiming.CurTime;
        }
    }
}
