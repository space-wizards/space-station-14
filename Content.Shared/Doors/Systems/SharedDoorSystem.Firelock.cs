using Content.Shared.Access.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Doors.Systems;

public abstract partial class SharedDoorSystem
{
    private static void OnBeforePry(EntityUid uid, FirelockComponent component, ref BeforePryEvent args)
    {
        if (args.Cancelled || !component.Powered || args.StrongPry || args.PryPowered)
            return;

        args.Cancelled = true;
    }

    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    private void InitializeFirelock()
    {
        // Access/Prying
        SubscribeLocalEvent<FirelockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<FirelockComponent, BeforePryEvent>(OnBeforePry);
        SubscribeLocalEvent<FirelockComponent, GetPryTimeModifierEvent>(OnDoorGetPryTimeModifier);
        SubscribeLocalEvent<FirelockComponent, PriedEvent>(OnAfterPried);

        // Visuals
        SubscribeLocalEvent<FirelockComponent, MapInitEvent>(UpdateVisuals);
        SubscribeLocalEvent<FirelockComponent, ComponentStartup>(UpdateVisuals);

        SubscribeLocalEvent<FirelockComponent, ExaminedEvent>(OnExamined);
    }

    public bool EmergencyPressureStop(Entity<FirelockComponent, DoorComponent> firelock)
    {
        if (firelock.Comp2.State != DoorState.Open
            || firelock.Comp1.EmergencyCloseCooldown != null
            && _gameTiming.CurTime < firelock.Comp1.EmergencyCloseCooldown)
            return false;

        return TryClose((firelock, firelock.Comp2)) && OnPartialClose((firelock, firelock.Comp2));
    }

    #region Access/Prying

    private void OnBeforeDoorOpened(Entity<FirelockComponent> firelock, ref BeforeDoorOpenedEvent args)
    {
        // Give the Door remote the ability to force a firelock open even if it is holding back dangerous gas
        var overrideAccess = (args.User != null) && _accessReaderSystem.IsAllowed(args.User.Value, firelock);

        if (!firelock.Comp.Powered || (!overrideAccess && firelock.Comp.IsLocked))
            args.Cancel();
        else if (args.User != null)
            WarnPlayer(firelock, args.User.Value);
    }

    private void OnDoorGetPryTimeModifier(Entity<FirelockComponent> firelock, ref GetPryTimeModifierEvent args)
    {
        WarnPlayer(firelock, args.User);

        if (firelock.Comp.IsLocked)
            args.PryTimeModifier *= firelock.Comp.LockedPryTimeModifier;
    }

    private void WarnPlayer(Entity<FirelockComponent> firelock, EntityUid user)
    {
        if (firelock.Comp.Temperature)
        {
            _popupSystem.PopupClient(Loc.GetString("firelock-component-is-holding-fire-message"),
                firelock.Owner,
                user,
                PopupType.MediumCaution);
        }
        else if (firelock.Comp.Pressure)
        {
            _popupSystem.PopupClient(Loc.GetString("firelock-component-is-holding-pressure-message"),
                firelock.Owner,
                user,
                PopupType.MediumCaution);
        }
    }

    private void OnAfterPried(Entity<FirelockComponent> firelock, ref PriedEvent args)
    {
        firelock.Comp.EmergencyCloseCooldown = _gameTiming.CurTime + firelock.Comp.EmergencyCloseCooldownDuration;
    }

    #endregion

    #region Visuals

    private void UpdateVisuals(EntityUid uid, FirelockComponent component, EntityEventArgs args) =>
        UpdateVisuals(uid, component);

    private void UpdateVisuals(EntityUid uid,
        FirelockComponent? firelock = null,
        DoorComponent? door = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref door, ref appearance, false))
            return;

        // only bother to check pressure on doors that are some variation of closed.
        if (door.State != DoorState.Closed
            && door.State != DoorState.WeldedClosed
            && door.State != DoorState.Denying)
        {
            _appearance.SetData(uid, DoorVisuals.ClosedLights, false, appearance);
            return;
        }

        if (!Resolve(uid, ref firelock, ref appearance, false))
            return;

        _appearance.SetData(uid, DoorVisuals.ClosedLights, firelock.IsLocked, appearance);
    }

    #endregion

    private void OnExamined(Entity<FirelockComponent> firelock, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(FirelockComponent)))
        {
            if (firelock.Comp.Pressure)
                args.PushMarkup(Loc.GetString("firelock-component-examine-pressure-warning"));
            if (firelock.Comp.Temperature)
                args.PushMarkup(Loc.GetString("firelock-component-examine-temperature-warning"));
        }
    }
}
