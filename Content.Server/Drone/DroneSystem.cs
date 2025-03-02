using Content.Server.Body.Systems;
using Content.Server.Drone.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Popups;
using Content.Server.Tools.Innate;
using Content.Shared.UserInterface;
using Content.Shared.Body.Components;
using Content.Shared.Drone;
using Content.Shared.Emoting;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;

namespace Content.Server.Drone
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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DroneComponent, UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<DroneComponent, UserOpenActivatableUIAttemptEvent>(OnActivateUIAttempt);
            SubscribeLocalEvent<DroneComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<DroneComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<DroneComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<DroneComponent, MindRemovedMessage>(OnMindRemoved);
            SubscribeLocalEvent<DroneComponent, EmoteAttemptEvent>(OnEmoteAttempt);
            SubscribeLocalEvent<DroneComponent, ThrowAttemptEvent>(OnThrowAttempt);
        }

        // Imp. this replaces OnInteractionAttempt from the upstream version of DroneSystem.
        private void OnUseAttempt(EntityUid uid, DroneComponent component, UseAttemptEvent args)
        {
            if (args.Used != null && NonDronesInRange(uid, component))
            {
                if (_whitelist.IsBlacklistPass(component.Blacklist, args.Used)) // imp special. blacklist. this one *does* prevent actions. it would probably be best if this read from the component or something.
                {
                    args.Cancel();
                    if (_gameTiming.CurTime >= component.NextProximityAlert)
                    {
                        _popupSystem.PopupEntity(Loc.GetString("drone-cant-use-nearby", ("being", component.NearestEnt)), uid, uid);
                        component.NextProximityAlert = _gameTiming.CurTime + component.ProximityDelay;
                    }
                }

                else if (_whitelist.IsWhitelistPass(component.Whitelist, args.Used)) /// tag whitelist. sends proximity warning popup if the item isn't whitelisted. Doesn't prevent actions.
				{
                    component.NextProximityAlert = _gameTiming.CurTime + component.ProximityDelay;
                    if (_gameTiming.CurTime >= component.NextProximityAlert)
                    {
                        _popupSystem.PopupEntity(Loc.GetString("drone-too-close", ("being", component.NearestEnt)), uid, uid);
                        component.NextProximityAlert = _gameTiming.CurTime + component.ProximityDelay;
                    }
                }
            }

            else if (args.Used != null && _whitelist.IsBlacklistPass(component.Blacklist, args.Used))
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

        private void OnMindAdded(EntityUid uid, DroneComponent drone, MindAddedMessage args)
        {
            UpdateDroneAppearance(uid, DroneStatus.On);
            _popupSystem.PopupEntity(Loc.GetString("drone-activated"), uid, PopupType.Large);
        }

        private void OnMindRemoved(EntityUid uid, DroneComponent drone, MindRemovedMessage args)
        {
            UpdateDroneAppearance(uid, DroneStatus.Off);
            EnsureComp<GhostTakeoverAvailableComponent>(uid);
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

        private bool NonDronesInRange(EntityUid uid, DroneComponent component)
        {
            var xform = Comp<TransformComponent>(uid);
            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapPosition, component.InteractionBlockRange))
            {
                // Return true if the entity is/was controlled by a player and is not a drone or ghost.
                if (HasComp<MindContainerComponent>(entity) && !HasComp<DroneComponent>(entity) && !HasComp<GhostComponent>(entity))
                {
                    // imp change. this filters out all dead entities.
                    if ((TryComp<MobStateComponent>(entity, out var entityMobState) && _mobStateSystem.IsDead(entity, entityMobState)))
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
