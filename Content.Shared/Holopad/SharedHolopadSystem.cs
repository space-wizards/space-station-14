using Robust.Shared.Timing;

namespace Content.Shared.Holopad;

public abstract class SharedHolopadSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public bool IsHolopadControlLocked(Entity<HolopadComponent> entity, EntityUid? user = null)
    {
        if (_timing.CurTime > entity.Comp.ControlLockoutEndTime)
            return false;

        if (entity.Comp.ControlLockoutOwner == null || entity.Comp.ControlLockoutOwner == user)
            return false;

        return true;
    }

    public TimeSpan GetHolopadControlLockedPeriod(Entity<HolopadComponent> entity)
    {
        return entity.Comp.ControlLockoutEndTime - _timing.CurTime;
    }

    public bool IsHolopadBroadcastOnCoolDown(Entity<HolopadComponent> entity)
    {
        if (_timing.CurTime > entity.Comp.ControlLockoutCoolDownEndTime)
            return false;

        return true;
    }

    public TimeSpan GetHolopadBroadcastCoolDown(Entity<HolopadComponent> entity)
    {
        return entity.Comp.ControlLockoutCoolDownEndTime - _timing.CurTime;
    }
}
