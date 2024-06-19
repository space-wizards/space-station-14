using Content.Shared.Access.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Doors.Systems;

public abstract class SharedFirelockSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Access/Prying
        SubscribeLocalEvent<FirelockComponent, BeforePryEvent>(OnBeforePry);
        SubscribeLocalEvent<FirelockComponent, GetPryTimeModifierEvent>(OnDoorGetPryTimeModifier);
        SubscribeLocalEvent<FirelockComponent, PriedEvent>(OnAfterPried);

        SubscribeLocalEvent<FirelockComponent, ExaminedEvent>(OnExamined);
    }

    public bool EmergencyPressureStop(EntityUid uid, FirelockComponent? firelock = null, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref firelock, ref door))
            return false;

        if (door.State != DoorState.Open
            || firelock.EmergencyCloseCooldown != null
            && _gameTiming.CurTime < firelock.EmergencyCloseCooldown)
            return false;

        if (!_doorSystem.TryClose(uid, door))
            return false;

        return _doorSystem.OnPartialClose(uid, door);
    }

    #region Prying

    private void OnBeforePry(EntityUid uid, FirelockComponent component, ref BeforePryEvent args)
    {
        if (args.Cancelled)
            return;

        if (!component.Powered || args.ToolUsed || args.PryPowered)
            return;

        args.Cancelled = true;
    }

    private void OnDoorGetPryTimeModifier(EntityUid uid, FirelockComponent component, ref GetPryTimeModifierEvent args)
    {
        WarnPlayer((uid, component), args.User);

        if (component.IsLocked)
            args.PryTimeModifier *= component.LockedPryTimeModifier;
    }

    private void WarnPlayer(Entity<FirelockComponent> ent, EntityUid user)
    {
        if (ent.Comp.IsLocked)
        {
            _popupSystem.PopupClient(Loc.GetString("firelock-component-locked"),
                ent.Owner,
                user,
                PopupType.MediumCaution);
        }
    }

    private void OnAfterPried(EntityUid uid, FirelockComponent component, ref PriedEvent args)
    {
        component.EmergencyCloseCooldown = _gameTiming.CurTime + component.EmergencyCloseCooldownDuration;
    }

    #endregion

    private void OnExamined(Entity<FirelockComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.IsLocked)
            args.PushMarkup(Loc.GetString("firelock-component-examine-locked"));
    }
}
