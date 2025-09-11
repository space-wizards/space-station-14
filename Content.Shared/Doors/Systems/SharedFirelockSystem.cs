using Content.Shared.Access.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Doors.Systems;

public abstract class SharedFirelockSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Access/Prying
        SubscribeLocalEvent<FirelockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<FirelockComponent, BeforePryEvent>(OnBeforePry);
        SubscribeLocalEvent<FirelockComponent, GetPryTimeModifierEvent>(OnDoorGetPryTimeModifier);
        SubscribeLocalEvent<FirelockComponent, PriedEvent>(OnAfterPried);

        // Visuals
        SubscribeLocalEvent<FirelockComponent, MapInitEvent>(UpdateVisuals);
        SubscribeLocalEvent<FirelockComponent, ComponentStartup>(OnComponentStartup);

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

    #region Access/Prying

    private void OnBeforeDoorOpened(EntityUid uid, FirelockComponent component, BeforeDoorOpenedEvent args)
    {
        // Give the Door remote the ability to force a firelock open even if it is holding back dangerous gas
        var overrideAccess = (args.User != null) && _accessReaderSystem.IsAllowed(args.User.Value, uid);

        if (!component.Powered || (!overrideAccess && component.IsLocked))
            args.Cancel();
        else if (args.User != null)
            WarnPlayer((uid, component), args.User.Value);
    }

    private void OnBeforePry(EntityUid uid, FirelockComponent component, ref BeforePryEvent args)
    {
        if (args.Cancelled || !component.Powered || args.StrongPry || args.PryPowered)
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
        if (ent.Comp.Temperature)
        {
            _popupSystem.PopupClient(Loc.GetString("firelock-component-is-holding-fire-message"),
                ent.Owner,
                user,
                PopupType.MediumCaution);
        }
        else if (ent.Comp.Pressure)
        {
            _popupSystem.PopupClient(Loc.GetString("firelock-component-is-holding-pressure-message"),
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

    #region Visuals

    protected virtual void OnComponentStartup(Entity<FirelockComponent> ent, ref ComponentStartup args)
    {
        UpdateVisuals(ent.Owner,ent.Comp, args);
    }

    private void UpdateVisuals(EntityUid uid, FirelockComponent component, EntityEventArgs args) => UpdateVisuals(uid, component);

    private void UpdateVisuals(EntityUid uid,
        FirelockComponent? firelock = null,
        DoorComponent? door = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref door, ref appearance, false))
            return;

        // only bother to check pressure on doors that are some variation of closed.
        if (door.State != DoorState.Closed
            && door.State != DoorState.Welded
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

    private void OnExamined(Entity<FirelockComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(FirelockComponent)))
        {
            if (ent.Comp.Pressure)
                args.PushMarkup(Loc.GetString("firelock-component-examine-pressure-warning"));
            if (ent.Comp.Temperature)
                args.PushMarkup(Loc.GetString("firelock-component-examine-temperature-warning"));
        }
    }
}

[Serializable, NetSerializable]
public enum FirelockVisuals : byte
{
    PressureWarning,
    TemperatureWarning,
}

[Serializable, NetSerializable]
public enum FirelockVisualLayersPressure : byte
{
    Base
}

[Serializable, NetSerializable]
public enum FirelockVisualLayersTemperature : byte
{
    Base
}
