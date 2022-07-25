using Content.Shared.Drone;
using Content.Server.Drone.Components;
using Content.Shared.Actions;
using Content.Server.Light.Components;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction.Components;
using Content.Shared.Examine;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Item;
using Content.Shared.Emoting;
using Content.Shared.Body.Components;
using Content.Server.Popups;
using Content.Server.Mind.Components;
using Content.Server.Ghost.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Hands.Components;
using Content.Server.UserInterface;
using Robust.Shared.Player;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.IdentityManagement;

namespace Content.Server.Drone
{
    public sealed class DroneSystem : SharedDroneSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DroneComponent, InteractionAttemptEvent>(OnInteractionAttempt);
            SubscribeLocalEvent<DroneComponent, UserOpenActivatableUIAttemptEvent>(OnActivateUIAttempt);
            SubscribeLocalEvent<DroneComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<DroneComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<DroneComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<DroneComponent, MindRemovedMessage>(OnMindRemoved);
            SubscribeLocalEvent<DroneComponent, EmoteAttemptEvent>(OnEmoteAttempt);
            SubscribeLocalEvent<DroneComponent, ThrowAttemptEvent>(OnThrowAttempt);
        }

        private void OnInteractionAttempt(EntityUid uid, DroneComponent component, InteractionAttemptEvent args)
        {
            if (!component.ApplyLaws)
                return;
            if (args.Target != null && !HasComp<UnremoveableComponent>(args.Target) && NonDronesInRange(uid, component))
                args.Cancel();

            if (HasComp<SharedItemComponent>(args.Target) && !HasComp<UnremoveableComponent>(args.Target))
            {
                if (!_tagSystem.HasAnyTag(args.Target.Value, "DroneUsable", "Trash"))
                    args.Cancel();
            }
        }

        private void OnActivateUIAttempt(EntityUid uid, DroneComponent component, UserOpenActivatableUIAttemptEvent args)
        {
            if (!component.ApplyLaws)
                return;
            if (!_tagSystem.HasTag(args.Target, "DroneUsable"))
            {
                args.Cancel();
            }
        }

        private void OnExamined(EntityUid uid, DroneComponent component, ExaminedEvent args)
        {
            if (TryComp<MindComponent>(uid, out var mind) && mind.HasMind)
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
            if (args.Component.IsDead())
            {
                var body = Comp<SharedBodyComponent>(uid); //There's no way something can have a mobstate but not a body...

                foreach (var item in drone.ToolUids)
                {
                    if (_tagSystem.HasTag(item, "Drone"))
                    {
                        RemComp<UnremoveableComponent>(item);
                    }
                    else
                    {
                        Del(item);
                    }
                }

                body.Gib();
                Del(uid);
            }
        }

        private void OnMindAdded(EntityUid uid, DroneComponent drone, MindAddedMessage args)
        {
            UpdateDroneAppearance(uid, DroneStatus.On);
            _popupSystem.PopupEntity(Loc.GetString("drone-activated"), uid,
                Filter.Pvs(uid), PopupType.Large);

            if (drone.AlreadyAwoken == false)
            {
                var spawnCoord = Transform(uid).Coordinates;

                if (drone.Tools.Count == 0) return;

                if (TryComp<HandsComponent>(uid, out var hands) && hands.Count >= drone.Tools.Count)
                {
                    var items = EntitySpawnCollection.GetSpawns(drone.Tools, _robustRandom);
                    foreach (var entry in items)
                    {
                        var item = Spawn(entry, spawnCoord);
                        AddComp<UnremoveableComponent>(item);
                        if (!_handsSystem.TryPickupAnyHand(uid, item, checkActionBlocker: false))
                        {
                            QueueDel(item);
                            Logger.Error($"Drone ({ToPrettyString(uid)}) failed to pick up innate item ({ToPrettyString(item)})");
                            continue;
                        }
                        drone.ToolUids.Add(item);
                    }
                }

                if (TryComp<ActionsComponent>(uid, out var actions) && TryComp<UnpoweredFlashlightComponent>(uid, out var flashlight))
                {
                    _actionsSystem.AddAction(uid, flashlight.ToggleAction, null, actions);
                }

                drone.AlreadyAwoken = true;
            }
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
                appearance.SetData(DroneVisuals.Status, status);
            }
        }

        private bool NonDronesInRange(EntityUid uid, DroneComponent component)
        {
            var xform = Comp<TransformComponent>(uid);
            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapPosition, component.InteractionBlockRange))
            {
                // Return true if the entity is/was controlled by a player and is not a drone or ghost.
                if (HasComp<MindComponent>(entity) && !HasComp<DroneComponent>(entity) && !HasComp<GhostComponent>(entity))
                {
                    // Filter out dead ghost roles. Dead normal players are intended to block.
                    if ((TryComp<MobStateComponent>(entity, out var entityMobState) && HasComp<GhostTakeoverAvailableComponent>(entity) && entityMobState.IsDead()))
                        continue;
                    if (_gameTiming.IsFirstTimePredicted)
                        _popupSystem.PopupEntity(Loc.GetString("drone-too-close", ("being", Identity.Entity(entity, EntityManager))), uid, Filter.Entities(uid));
                    return true;
                }
            }
            return false;
        }
    }
}
