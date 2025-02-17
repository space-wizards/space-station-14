using Content.Shared.Emp;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Holopad;

public abstract class SharedHolopadSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolopadComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);
        SubscribeLocalEvent<HolopadComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
    }

    private void OnGettingPickedUpAttempt(EntityUid uid, HolopadComponent component, GettingPickedUpAttemptEvent args)
    {
        if (!component.Portable)
            return;

        if (component.Deployed)
            args.Cancel();
    }

    private void OnUIOpenAttempt(Entity<HolopadComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (HasComp<EmpDisabledComponent>(ent))
        {
            args.Cancel();

            if (_net.IsServer)
                _popup.PopupEntity("Голопад временно не работает. Попробуйте позже.", ent, args.User);
        }
    }

    public bool IsHolopadControlLocked(Entity<HolopadComponent> entity, EntityUid? user = null)
    {
        if (entity.Comp.ControlLockoutStartTime == TimeSpan.Zero)
            return false;

        if (entity.Comp.ControlLockoutStartTime + TimeSpan.FromSeconds(entity.Comp.ControlLockoutDuration) < _timing.CurTime)
            return false;

        if (entity.Comp.ControlLockoutOwner == null || entity.Comp.ControlLockoutOwner == user)
            return false;

        return true;
    }

    public TimeSpan GetHolopadControlLockedPeriod(Entity<HolopadComponent> entity)
    {
        return entity.Comp.ControlLockoutStartTime + TimeSpan.FromSeconds(entity.Comp.ControlLockoutDuration) - _timing.CurTime;
    }

    public bool IsHolopadBroadcastOnCoolDown(Entity<HolopadComponent> entity)
    {
        if (entity.Comp.ControlLockoutStartTime == TimeSpan.Zero)
            return false;

        if (entity.Comp.ControlLockoutStartTime + TimeSpan.FromSeconds(entity.Comp.ControlLockoutCoolDown) < _timing.CurTime)
            return false;

        return true;
    }

    public TimeSpan GetHolopadBroadcastCoolDown(Entity<HolopadComponent> entity)
    {
        return entity.Comp.ControlLockoutStartTime + TimeSpan.FromSeconds(entity.Comp.ControlLockoutCoolDown) - _timing.CurTime;
    }
}
