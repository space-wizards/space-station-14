using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Doors.Components;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Doors.Systems
{
    public sealed class FirelockSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
        [Dependency] private readonly AtmosAlarmableSystem _atmosAlarmable = default!;
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;
        [Dependency] private readonly TransformSystem _xformSys = default!;
        [Dependency] private readonly AppearanceSystem _appearance = default!;

        private static float _visualUpdateInterval = 0.5f;
        private float _accumulatedFrameTime;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FirelockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
            SubscribeLocalEvent<FirelockComponent, DoorGetPryTimeModifierEvent>(OnDoorGetPryTimeModifier);
            SubscribeLocalEvent<FirelockComponent, DoorStateChangedEvent>(OnUpdateState);

            SubscribeLocalEvent<FirelockComponent, BeforeDoorAutoCloseEvent>(OnBeforeDoorAutoclose);
            SubscribeLocalEvent<FirelockComponent, AtmosAlarmEvent>(OnAtmosAlarm);

            // Visuals
            SubscribeLocalEvent<FirelockComponent, MapInitEvent>(UpdateVisuals);
        }

        #region Visuals
        private void UpdateVisuals(EntityUid uid, FirelockComponent component, EntityEventArgs args) => UpdateVisuals(uid);

        public override void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;
            if (_accumulatedFrameTime < _visualUpdateInterval)
                return;

            _accumulatedFrameTime -= _visualUpdateInterval;

            var powerQuery = GetEntityQuery<ApcPowerReceiverComponent>();

            foreach (var (_, door, appearance, xform) in EntityQuery<FirelockComponent, DoorComponent, AppearanceComponent, TransformComponent>())
            {
                UpdateVisuals(door.Owner, door, appearance, xform, powerQuery);
            }
        }

        private void UpdateVisuals(EntityUid uid,
            DoorComponent? door = null,
            AppearanceComponent? appearance = null,
            TransformComponent? xform = null,
            EntityQuery<ApcPowerReceiverComponent>? powerQuery = null)
        {
            if (!Resolve(uid, ref door, ref appearance, ref xform, false))
                return;

            // only bother to check pressure on doors that are some variation of closed.
            if (door.State != DoorState.Closed
                && door.State != DoorState.Welded
                && door.State != DoorState.Denying)
            {
                _appearance.SetData(uid, DoorVisuals.ClosedLights, false, appearance);
                return;
            }

            powerQuery ??= EntityManager.GetEntityQuery<ApcPowerReceiverComponent>();
            if (powerQuery.Value.TryGetComponent(uid, out var receiver) && !receiver.Powered)
            {
                _appearance.SetData(uid, DoorVisuals.ClosedLights, false, appearance);
                return;
            }

            _appearance.SetData(uid, DoorVisuals.ClosedLights, IsHoldingPressureOrFire(uid, xform), appearance);
        }
        #endregion

        public bool EmergencyPressureStop(EntityUid uid, FirelockComponent? firelock = null, DoorComponent? door = null)
        {
            if (!Resolve(uid, ref firelock, ref door))
                return false;

            if (door.State == DoorState.Open)
            {
                if (_doorSystem.TryClose(door.Owner, door))
                {
                    return _doorSystem.OnPartialClose(door.Owner, door);
                }
            }
            return false;
        }

        private void OnBeforeDoorOpened(EntityUid uid, FirelockComponent component, BeforeDoorOpenedEvent args)
        {
            if (!this.IsPowered(uid, EntityManager) || IsHoldingPressureOrFire(uid))
                args.Cancel();
        }

        private void OnDoorGetPryTimeModifier(EntityUid uid, FirelockComponent component, DoorGetPryTimeModifierEvent args)
        {
            var state = CheckPressureAndFire(uid);

            if (state.Fire)
            {
                _popupSystem.PopupEntity(Loc.GetString("firelock-component-is-holding-fire-message"),
                    uid, Filter.Pvs(uid, entityManager: EntityManager));
            }
            else if (state.Pressure)
            {
                _popupSystem.PopupEntity(Loc.GetString("firelock-component-is-holding-pressure-message"),
                    uid, Filter.Pvs(uid, entityManager: EntityManager));
            }

            if (state.Fire || state.Pressure)
                args.PryTimeModifier *= component.LockedPryTimeModifier;
        }

        private void OnUpdateState(EntityUid uid, FirelockComponent component, DoorStateChangedEvent args)
        {
            var ev = new BeforeDoorAutoCloseEvent();
            RaiseLocalEvent(uid, ev);
            if (ev.Cancelled)
            {
                return;
            }

            _doorSystem.SetNextStateChange(uid, component.AutocloseDelay);
            UpdateVisuals(uid, component, args);
        }

        private void OnBeforeDoorAutoclose(EntityUid uid, FirelockComponent component, BeforeDoorAutoCloseEvent args)
        {
            if (!this.IsPowered(uid, EntityManager))
                args.Cancel();

            // Make firelocks autoclose, but only if the last alarm type it
            // remembers was a danger. This is to prevent people from
            // flooding hallways with endless bad air/fire.
            if (component.AlarmAutoClose &&
                (_atmosAlarmable.TryGetHighestAlert(uid, out var alarm) && alarm != AtmosAlarmType.Danger || alarm == null))
                args.Cancel();
        }

        private void OnAtmosAlarm(EntityUid uid, FirelockComponent component, AtmosAlarmEvent args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            if (!TryComp<DoorComponent>(uid, out var doorComponent))
                return;

            if (args.AlarmType == AtmosAlarmType.Normal || args.AlarmType == AtmosAlarmType.Warning)
            {
                if (doorComponent.State == DoorState.Closed)
                    _doorSystem.TryOpen(uid);
            }
            else if (args.AlarmType == AtmosAlarmType.Danger)
            {
                EmergencyPressureStop(uid, component, doorComponent);
            }
        }

        public bool IsHoldingPressureOrFire(EntityUid uid, TransformComponent? xform = null)
        {
            var result = CheckPressureAndFire(uid, xform);
            return result.Pressure || result.Fire;
        }

        public (bool Pressure, bool Fire) CheckPressureAndFire(EntityUid owner, TransformComponent? xform = null)
        {
            if (!Resolve(owner, ref xform))
                return (false, false);

            float threshold = 20;
            var position = _xformSys.GetGridOrMapTilePosition(owner, xform);
            if (xform.GridUid is not {} gridUid)
                return (false, false);
            var minMoles = float.MaxValue;
            var maxMoles = 0f;

            return (IsHoldingPressure(), IsHoldingFire());

            bool IsHoldingPressure()
            {
                foreach (var adjacent in _atmosSystem.GetAdjacentTileMixtures(gridUid, position))
                {
                    var moles = adjacent.TotalMoles;
                    if (moles < minMoles)
                        minMoles = moles;
                    if (moles > maxMoles)
                        maxMoles = moles;
                }

                return (maxMoles - minMoles) > threshold;
            }

            bool IsHoldingFire()
            {
                if (_atmosSystem.GetTileMixture(gridUid, null, position) == null)
                    return false;

                if (_atmosSystem.IsHotspotActive(gridUid, position))
                    return true;

                foreach (var adjacent in _atmosSystem.GetAdjacentTiles(gridUid, position))
                {
                    if (_atmosSystem.IsHotspotActive(gridUid, adjacent))
                        return true;
                }
                return false;
            }
        }
    }
}
