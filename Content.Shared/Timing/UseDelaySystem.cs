using Robust.Shared.Timing;

namespace Content.Shared.Timing;

public sealed class UseDelaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<UseDelayComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnUnpaused(Entity<UseDelayComponent> ent, ref EntityUnpausedEvent args)
    {
        // We got unpaused, resume the delay
        ent.Comp.DelayStartTime += args.PausedTime;
        ent.Comp.DelayEndTime += args.PausedTime;
        Dirty(ent);
    }

    public void SetDelay(Entity<UseDelayComponent> ent, TimeSpan delay)
    {
        if (ent.Comp.Delay == delay)
            return;

        ent.Comp.Delay = delay;
        Dirty(ent);
    }

    /// <summary>
    /// Returns true if the entity has a currently active UseDelay.
    /// </summary>
    public bool IsDelayed(Entity<UseDelayComponent> ent)
    {
        return ent.Comp.DelayEndTime >= _gameTiming.CurTime;
    }

    /// <summary>
    /// Cancels the current delay.
    /// </summary>
    public void CancelDelay(Entity<UseDelayComponent> ent)
    {
        ent.Comp.DelayEndTime = _gameTiming.CurTime;
        Dirty(ent);
    }

    /// <summary>
    /// Resets the UseDelay entirely for this entity if possible.
    /// </summary>
    /// <param name="checkDelayed">Check if the entity has an ongoing delay, return false if it does, return true if it does not.</param>
    public bool TryResetDelay(Entity<UseDelayComponent> ent, bool checkDelayed = false)
    {
        if (checkDelayed && IsDelayed(ent))
            return false;

        var curTime = _gameTiming.CurTime;
        ent.Comp.DelayStartTime = curTime;
        ent.Comp.DelayEndTime = curTime - _metadata.GetPauseTime(ent) + ent.Comp.Delay;
        Dirty(ent);
        return true;
    }
}
