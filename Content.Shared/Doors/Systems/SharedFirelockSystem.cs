using Content.Shared.Access.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;

namespace Content.Shared.Doors.Systems;

public abstract class SharedFirelockSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FirelockComponent, DoorStateChangedEvent>(OnUpdateState);

        SubscribeLocalEvent<FirelockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<FirelockComponent, GetPryTimeModifierEvent>(OnDoorGetPryTimeModifier);

        // Visuals
        SubscribeLocalEvent<FirelockComponent, MapInitEvent>(UpdateVisuals);
        SubscribeLocalEvent<FirelockComponent, ComponentStartup>(UpdateVisuals);
    }

    private void OnDoorGetPryTimeModifier(EntityUid uid, FirelockComponent component, ref GetPryTimeModifierEvent args)
    {
        if (component.Fire)
        {
            _popupSystem.PopupClient(Loc.GetString("firelock-component-is-holding-fire-message"),
                uid, args.User, PopupType.MediumCaution);
        }
        else if (component.Pressure)
        {
            _popupSystem.PopupClient(Loc.GetString("firelock-component-is-holding-pressure-message"),
                uid, args.User, PopupType.MediumCaution);
        }

        if (component.IsLocked)
            args.PryTimeModifier *= component.LockedPryTimeModifier;
    }

    private void OnBeforeDoorOpened(EntityUid uid, FirelockComponent component, BeforeDoorOpenedEvent args)
        {
            // Give the Door remote the ability to force a firelock open even if it is holding back dangerous gas
            var overrideAccess = (args.User != null) && _accessReaderSystem.IsAllowed(args.User.Value, uid);

            if (!component.Powered || (!overrideAccess && component.IsLocked))
                args.Cancel();
        }

        public bool EmergencyPressureStop(EntityUid uid, FirelockComponent? firelock = null, DoorComponent? door = null)
        {
            if (!Resolve(uid, ref firelock, ref door))
                return false;

            if (door.State == DoorState.Open)
            {
                if (_doorSystem.TryClose(uid, door))
                {
                    return _doorSystem.OnPartialClose(uid, door);
                }
            }
            return false;
        }


        #region Visuals

        protected void UpdateVisuals(EntityUid uid, FirelockComponent component, EntityEventArgs args) => UpdateVisuals(uid, component);

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

        private void OnUpdateState(EntityUid uid, FirelockComponent component, DoorStateChangedEvent args)
        {
            var ev = new BeforeDoorAutoCloseEvent();
            RaiseLocalEvent(uid, ev);
            UpdateVisuals(uid, component, args);
            if (ev.Cancelled)
            {
                return;
            }

            _doorSystem.SetNextStateChange(uid, component.AutocloseDelay);
        }
}


