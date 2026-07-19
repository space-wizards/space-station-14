using Robust.Shared.Timing;

namespace Content.Shared.Holopad;

public abstract partial class SharedHolopadSystem : EntitySystem
{
    private static readonly EntityTimerId LockoutTimer = new("lockout");
    private static readonly EntityTimerId CooldownTimer = new("cooldown");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HolopadComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HolopadComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnStartup(Entity<HolopadComponent> ent, ref ComponentStartup args)
    {
        RegisterHolopadTimers(ent);
    }

    private void OnHandleState(Entity<HolopadComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RegisterHolopadTimers(ent);
    }

    protected void RegisterHolopadTimers(Entity<HolopadComponent> ent)
    {
        if (ent.Comp.ControlLockoutEndTime > _timing.CurTime)
            _timers.SetTimerAt(ent, LockoutTimer, ent.Comp.ControlLockoutEndTime);
        else
            _timers.CancelTimer<HolopadComponent>(ent, LockoutTimer);

        if (ent.Comp.ControlLockoutCoolDownEndTime > _timing.CurTime)
            _timers.SetTimerAt(ent, CooldownTimer, ent.Comp.ControlLockoutCoolDownEndTime);
        else
            _timers.CancelTimer<HolopadComponent>(ent, CooldownTimer);
    }

    public bool IsHolopadControlLocked(Entity<HolopadComponent> entity, EntityUid? user = null)
    {
        if (!_timers.TryGetTimer<HolopadComponent>(entity, LockoutTimer, out _))
            return false;

        if (entity.Comp.ControlLockoutOwner == null || entity.Comp.ControlLockoutOwner == user)
            return false;

        return true;
    }

    public TimeSpan GetHolopadControlLockedPeriod(Entity<HolopadComponent> entity)
    {
        return _timers.TryGetTimer<HolopadComponent>(entity, LockoutTimer, out var timer)
            ? timer.Remaining
            : TimeSpan.Zero;
    }

    public bool IsHolopadBroadcastOnCoolDown(Entity<HolopadComponent> entity)
    {
        return _timers.TryGetTimer<HolopadComponent>(entity, CooldownTimer, out _);
    }

    public TimeSpan GetHolopadBroadcastCoolDown(Entity<HolopadComponent> entity)
    {
        return _timers.TryGetTimer<HolopadComponent>(entity, CooldownTimer, out var timer)
            ? timer.Remaining
            : TimeSpan.Zero;
    }
}
