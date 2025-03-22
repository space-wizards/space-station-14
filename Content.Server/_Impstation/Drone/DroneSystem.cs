using Content.Server.Body.Systems;
using Content.Server._Impstation.Drone.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Popups;
using Content.Server.Tools.Innate;
using Content.Server.PowerCell;
using Content.Shared.Alert;
using Content.Shared.UserInterface;
using Content.Shared.Body.Components;
using Content.Shared._Impstation.Drone;
using Content.Shared.Emoting;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Timing;
using Robust.Server.GameObjects;

namespace Content.Server._Impstation.Drone
{
    public sealed class DroneSystem : SharedDroneSystem
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly InnateToolSystem _innateToolSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
        [Dependency] private readonly PowerCellSystem _powerCell = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly ItemToggleSystem _toggle = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly AlertsSystem _alerts = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DroneComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<DroneComponent, UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<DroneComponent, UserOpenActivatableUIAttemptEvent>(OnActivateUIAttempt);
            SubscribeLocalEvent<DroneComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<DroneComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<DroneComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<DroneComponent, MindRemovedMessage>(OnMindRemoved);
            SubscribeLocalEvent<DroneComponent, EmoteAttemptEvent>(OnEmoteAttempt);
            SubscribeLocalEvent<DroneComponent, ThrowAttemptEvent>(OnThrowAttempt);
            SubscribeLocalEvent<DroneComponent, PowerCellChangedEvent>(OnPowerCellChanged);
            SubscribeLocalEvent<DroneComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        }

        // imp. for the battery system
        private void OnMapInit(Entity<DroneComponent> ent, ref MapInitEvent args)
        {
            UpdateBatteryAlert((ent.Owner, ent.Comp));

            if (!TryComp<MindContainerComponent>(ent.Owner, out var mind) || !mind.HasMind)
                _powerCell.SetDrawEnabled(ent.Owner, false);
        }

        // Imp. this replaces OnInteractionAttempt from the upstream version of DroneSystem.
        private void OnUseAttempt(EntityUid uid, DroneComponent component, UseAttemptEvent args)
        {
            if (args.Used != null && NonDronesInRange(uid, component))
            {
                if (_whitelist.IsWhitelistPass(component.Whitelist, args.Used)) /// tag whitelist. sends proximity warning popup if the item isn't whitelisted. Doesn't prevent actions. Takes precedent over blacklist.
				{
                    if (_gameTiming.CurTime >= component.NextProximityAlert)
                    {
                        _popupSystem.PopupEntity(Loc.GetString("drone-too-close", ("being", component.NearestEnt)), uid, uid);
                        component.NextProximityAlert = _gameTiming.CurTime + component.ProximityDelay;
                    }
                }

                else if (_whitelist.IsBlacklistPass(component.Blacklist, args.Used)) // imp special. blacklist. this one *does* prevent actions. it would probably be best if this read from the component or something.
                {
                    args.Cancel();
                    if (_gameTiming.CurTime >= component.NextProximityAlert)
                    {
                        _popupSystem.PopupEntity(Loc.GetString("drone-cant-use-nearby", ("being", component.NearestEnt)), uid, uid);
                        component.NextProximityAlert = _gameTiming.CurTime + component.ProximityDelay;
                    }
                }
            }

            else if (args.Used != null && _whitelist.IsWhitelistFail(component.Whitelist, args.Used) && _whitelist.IsBlacklistPass(component.Blacklist, args.Used)) // prevent actions when no one is nearby only if the whitelist fails AND the blacklist passes.
            {
                args.Cancel();
                if (_gameTiming.CurTime >= component.NextProximityAlert)
                {
                    _popupSystem.PopupEntity(Loc.GetString("drone-cant-use"), uid, uid);
                    component.NextProximityAlert = _gameTiming.CurTime + component.ProximityDelay;
                }
            }
        }

        private void OnActivateUIAttempt(EntityUid uid, DroneComponent component, UserOpenActivatableUIAttemptEvent args)
        {
            if (_whitelist.IsBlacklistPass(component.Blacklist, args.Target))
            {
                args.Cancel();
            }
        }

        private void OnExamined(EntityUid uid, DroneComponent component, ExaminedEvent args)
        {
            if (TryComp<MindContainerComponent>(uid, out var mind) && mind.HasMind)
            {
                args.PushMarkup(Loc.GetString("drone-active"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("drone-dormant"));
            }
        }

        private void OnMobStateChanged(EntityUid uid, DroneComponent drone, MobStateChangedEvent args)
        {
            if (args.NewMobState == MobState.Dead)
            {
                if (TryComp<InnateToolComponent>(uid, out var innate))
                    _innateToolSystem.Cleanup(uid, innate);

                if (TryComp<BodyComponent>(uid, out var body))
                    _bodySystem.GibBody(uid, body: body);
                QueueDel(uid);
            }
        }

        private void OnPowerCellChanged(EntityUid uid, DroneComponent component, PowerCellChangedEvent args)
        {
            UpdateBatteryAlert((uid, component));

            // if we run out of charge & the drone isn't being deleted, kill the drone
            if (!TerminatingOrDeleted(uid) && !_powerCell.HasDrawCharge(uid))
            {
                _mobStateSystem.ChangeMobState(uid, MobState.Dead);
            }

            UpdateUI(uid, component);
        }

        private void OnPowerCellSlotEmpty(EntityUid uid, DroneComponent component, ref PowerCellSlotEmptyEvent args)
        {
            if (!TerminatingOrDeleted(uid))
                _mobStateSystem.ChangeMobState(uid, MobState.Dead);
        }

        private void OnMindAdded(EntityUid uid, DroneComponent drone, MindAddedMessage args)
        {
            UpdateDroneAppearance(uid, DroneStatus.On);
            _popupSystem.PopupEntity(Loc.GetString("drone-activated"), uid, PopupType.Large);
            _powerCell.SetDrawEnabled(uid, true);
        }

        private void OnMindRemoved(EntityUid uid, DroneComponent drone, MindRemovedMessage args)
        {
            UpdateDroneAppearance(uid, DroneStatus.Off);
            EnsureComp<GhostTakeoverAvailableComponent>(uid);
            _powerCell.SetDrawEnabled(uid, false);
        }

        private void OnEmoteAttempt(EntityUid uid, DroneComponent component, EmoteAttemptEvent args)
        {
            // No.
            args.Cancel();
        }

        private void OnThrowAttempt(EntityUid uid, DroneComponent drone, ThrowAttemptEvent args)
        {
            args.Cancel();
        }

        private void UpdateDroneAppearance(EntityUid uid, DroneStatus status)
        {
            if (TryComp<AppearanceComponent>(uid, out var appearance))
            {
                _appearance.SetData(uid, DroneVisuals.Status, status, appearance);
            }
        }

        public void UpdateUI(EntityUid uid, DroneComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var chargePercent = 0f;
            var hasBattery = false;
            if (_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            {
                hasBattery = true;
                chargePercent = battery.CurrentCharge / battery.MaxCharge;
            }

            var state = new DroneBuiState(chargePercent, hasBattery);
            _ui.SetUiState(uid, DroneUiKey.Key, state);
        }

        private void UpdateBatteryAlert(Entity<DroneComponent> ent, PowerCellSlotComponent? slotComponent = null)
        {
            if (!_powerCell.TryGetBatteryFromSlot(ent, out var battery, slotComponent))
            {
                _alerts.ClearAlert(ent, ent.Comp.BatteryAlert);
                _alerts.ShowAlert(ent, ent.Comp.NoBatteryAlert);
                return;
            }

            var chargePercent = (short)MathF.Round(battery.CurrentCharge / battery.MaxCharge * 10f);

            if (chargePercent == 5 && chargePercent < ent.Comp.LastChargePercent)
            {
                if (_gameTiming.CurTime >= ent.Comp.NextProximityAlert)
                {
                    _popupSystem.PopupEntity(Loc.GetString("drone-med-battery"), ent.Owner, ent.Owner, PopupType.MediumCaution);
                    ent.Comp.NextProximityAlert = _gameTiming.CurTime + ent.Comp.ProximityDelay;
                }
            }

            if (chargePercent == 2 && chargePercent < ent.Comp.LastChargePercent)
            {
                if (_gameTiming.CurTime >= ent.Comp.NextProximityAlert)
                {
                    _popupSystem.PopupEntity(Loc.GetString("drone-low-battery"), ent.Owner, ent.Owner, PopupType.LargeCaution);
                    ent.Comp.NextProximityAlert = _gameTiming.CurTime + ent.Comp.ProximityDelay;
                }
            }

            // we make sure 0 only shows if they have absolutely no battery.
            // also account for floating point imprecision
            if (chargePercent == 0 && _powerCell.HasDrawCharge(ent, cell: slotComponent))
            {
                chargePercent = 1;
            }

            ent.Comp.LastChargePercent = chargePercent;

            _alerts.ClearAlert(ent, ent.Comp.NoBatteryAlert);
            _alerts.ShowAlert(ent, ent.Comp.BatteryAlert, chargePercent);
        }

        private bool NonDronesInRange(EntityUid uid, DroneComponent component)
        {
            var xform = Comp<TransformComponent>(uid);
            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapPosition, component.InteractionBlockRange))
            {
                // Return true if the entity is/was controlled by a player and is not a drone or ghost.
                if (HasComp<MindContainerComponent>(entity) && !HasComp<DroneComponent>(entity) && !HasComp<GhostComponent>(entity))
                {
                    // imp change. this filters out all dead entities.
                    if (TryComp<MobStateComponent>(entity, out var entityMobState) && _mobStateSystem.IsDead(entity, entityMobState))
                        continue;
                    if (_gameTiming.IsFirstTimePredicted)
                    {
                        component.NearestEnt = Identity.Entity(entity, EntityManager); // imp. instead of doing popups in here, set a variable to the nearest entity for use elsewhere.
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
