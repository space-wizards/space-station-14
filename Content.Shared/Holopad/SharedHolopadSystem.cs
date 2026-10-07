using Content.Shared.Power.EntitySystems;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Telephone;
using Content.Shared.Verbs;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Holopad;

public abstract partial class SharedHolopadSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private SharedStationAiSystem _stationAi = default!;

    [SubscribeLocalEvent]
    private void AddToggleProjectorVerb(Entity<HolopadComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_powerReceiver.IsPowered(entity.Owner))
            return;

        if (HasComp<StationAiCoreComponent>(entity))
            return;

        // LinkedTelephones is not networked; EndingCall has already cleared its links.
        if (!TryComp<TelephoneComponent>(entity, out var entityTelephone) ||
            entityTelephone.CurrentState is TelephoneState.Calling or TelephoneState.Ringing or TelephoneState.InCall)
            return;

        var user = args.User;

        if (!HasComp<StationAiHeldComponent>(user))
            return;

        if (!_stationAi.TryGetCore(user, out var stationAiCore) ||
            stationAiCore.Comp is not { Remote: true, RemoteEntity: not null })
            return;

        var verb = new AlternativeVerb
        {
            Act = () => ActivateProjector(entity, user),
            Text = Loc.GetString("holopad-activate-projector-verb"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/vv.svg.192dpi.png")),
        };

        args.Verbs.Add(verb);
    }

    public bool IsHolopadControlLocked(Entity<HolopadComponent> entity, EntityUid? user = null)
    {
        if (_timing.CurTime > entity.Comp.ControlLockoutEndTime)
            return false;

        if (entity.Comp.ControlLockoutOwner is null || entity.Comp.ControlLockoutOwner == user)
            return false;

        return true;
    }

    public TimeSpan GetHolopadControlLockedPeriod(Entity<HolopadComponent> entity)
    {
        return entity.Comp.ControlLockoutEndTime - _timing.CurTime;
    }

    public bool IsHolopadBroadcastOnCoolDown(Entity<HolopadComponent> entity)
    {
        return !(_timing.CurTime > entity.Comp.ControlLockoutCoolDownEndTime);
    }

    public TimeSpan GetHolopadBroadcastCoolDown(Entity<HolopadComponent> entity)
    {
        return entity.Comp.ControlLockoutCoolDownEndTime - _timing.CurTime;
    }

    protected virtual void ActivateProjector(Entity<HolopadComponent> entity, EntityUid user) { }
}
