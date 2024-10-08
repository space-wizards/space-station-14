using Robust.Shared.Timing;

namespace Content.Shared.Holopad;

public abstract class SharedHolopadSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public bool IsHolopadControlLocked(Entity<HolopadComponent> holopad, EntityUid? user = null)
    {
        if (holopad.Comp.ControlLockoutStartTime == TimeSpan.Zero)
            return false;

        if (holopad.Comp.ControlLockoutStartTime + TimeSpan.FromSeconds(holopad.Comp.ControlLockoutDuration) < _timing.ServerTime)
            return false;

        if (user != null && holopad.Comp.ControlLockoutInitiator == user)
            return false;

        return true;
    }

    public bool IsHolopadBroadcastOnCoolDown(Entity<HolopadComponent> holopad)
    {
        if (holopad.Comp.ControlLockoutStartTime == TimeSpan.Zero)
            return false;

        if (holopad.Comp.ControlLockoutStartTime + TimeSpan.FromSeconds(holopad.Comp.ControlLockoutCoolDown) < _timing.ServerTime)
            return false;

        return true;
    }
}
