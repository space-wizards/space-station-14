using Content.Shared.Cooldown;
using Robust.Shared.Timing;

namespace Content.Shared.Timing;

public sealed class UseDelaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseDelayComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<UseDelayComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnUnpaused(EntityUid uid, UseDelayComponent component, ref EntityUnpausedEvent args)
    {
        // We got unpaused, resume the delay/cooldown. Currently this takes for granted that ItemCooldownComponent
        // handles the pausing on its own. I'm not even gonna check, because I CBF fixing it if it doesn't.
        component.DelayEndTime += args.PausedTime;
        Dirty(uid, component);
    }

    public bool TryUseDelay(EntityUid uid, UseDelayComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return true;

        var currentTime = _gameTiming.CurTime;
        var pausedTime = _metadata.GetPauseTime(uid);

        if (component.DelayEndTime + pausedTime > currentTime)
            return false;

        component.DelayEndTime = currentTime + component.Delay - pausedTime;
        Dirty(uid, component);

        // TODO just merge these components?
        var cooldown = EnsureComp<ItemCooldownComponent>(uid);
        cooldown.CooldownStart = currentTime;
        cooldown.CooldownEnd = component.DelayEndTime;
        Dirty(uid, cooldown);

        return true;
    }

    private void OnHandleState(EntityUid uid, UseDelayComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (component.DelayEndTime == null)
            _activeDelays.Remove(component);
        else
            _activeDelays.Add(component);
    }

    /// <summary>
    /// Returns true if the entity has a currently active UseDelay.
    /// </summary>
    public bool IsDelayed(EntityUid uid, UseDelayComponent? component = null)
    {
        return Resolve(uid, ref component, false) && GetDelayTime(uid, component) > _gameTiming.CurTime;
    }

    /// <summary>
    /// Cancels the current delay.
    /// </summary>
    public void Cancel(EntityUid uid, UseDelayComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || GetDelayTime(uid, component) <= _gameTiming.CurTime)
            return;

        component.DelayEndTime = _gameTiming.CurTime;
        Dirty(uid, component);

        if (TryComp<ItemCooldownComponent>(uid, out var cooldown))
        {
            cooldown.CooldownEnd = _gameTiming.CurTime;
        }
    }

    /// <summary>
    /// Resets the UseDelay entirely for this entity if possible.
    /// </summary>
    public void ResetDelay(EntityUid uid, UseDelayComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        cooldown.CooldownStart = currentTime;
        component.DelayEndTime = MathHelper.Max(_gameTiming.CurTime + component.Delay, component.DelayEndTime);
        // TODO just merge these components?
        var cooldown = EnsureComp<ItemCooldownComponent>(uid);
        cooldown.CooldownStart = _gameTiming.CurTime;
        cooldown.CooldownEnd = component.DelayEndTime;
        Dirty(uid, component);
    }

    private TimeSpan GetDelayTime(EntityUid uid, UseDelayComponent component)
    {
        var pauseTime = _metadata.GetPauseTime(uid);
        return component.DelayEndTime + pauseTime;
    }
}
