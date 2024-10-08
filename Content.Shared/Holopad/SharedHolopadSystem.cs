using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Holopad;

public abstract class SharedHolopadSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    public bool IsHolopadControlLocked(Entity<HolopadComponent> holopad, EntityUid? user = null)
    {
        if (holopad.Comp.ControlLockoutStartTime == TimeSpan.Zero)
            return false;

        var time = _net.IsServer ? _timing.RealTime : _timing.ServerTime;

        if (holopad.Comp.ControlLockoutStartTime + TimeSpan.FromSeconds(holopad.Comp.ControlLockoutDuration) < time)
            return false;

        if (user != null && holopad.Comp.ControlLockoutInitiator == user)
            return false;

        return true;
    }

    public TimeSpan GetHolopadControlLockedPeriod(Entity<HolopadComponent> holopad)
    {
        var time = _net.IsServer ? _timing.RealTime : _timing.ServerTime;

        return holopad.Comp.ControlLockoutStartTime + TimeSpan.FromSeconds(holopad.Comp.ControlLockoutDuration) - time;
    }

    public bool IsHolopadBroadcastOnCoolDown(Entity<HolopadComponent> holopad)
    {
        if (holopad.Comp.ControlLockoutStartTime == TimeSpan.Zero)
            return false;

        var time = _net.IsServer ? _timing.RealTime : _timing.ServerTime;

        if (holopad.Comp.ControlLockoutStartTime + TimeSpan.FromSeconds(holopad.Comp.ControlLockoutCoolDown) < time)
            return false;

        return true;
    }

    public TimeSpan GetHolopadBroadcastCoolDown(Entity<HolopadComponent> holopad)
    {
        var time = _net.IsServer ? _timing.RealTime : _timing.ServerTime;

        return holopad.Comp.ControlLockoutStartTime + TimeSpan.FromSeconds(holopad.Comp.ControlLockoutCoolDown) - time;
    }
}
