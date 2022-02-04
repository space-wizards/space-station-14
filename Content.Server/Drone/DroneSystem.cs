using Content.Shared.Drone;
using Content.Server.Drone.Components;
using Content.Shared.Drone.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.MobState.Components;
using Content.Shared.MobState;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Server.Popups;
using Content.Server.Mind.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Hands.Components;
using Content.Shared.Body.Components;
using Content.Server.Actions.Events;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Content.Shared.Tag;

namespace Content.Server.Drone
{
    public class DroneSystem : SharedDroneSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DroneComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<DroneComponent, DisarmAttemptEvent>(OnDisarmAttempt);
            SubscribeLocalEvent<DroneComponent, DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<DroneComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<DroneComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<DroneComponent, MindRemovedMessage>(OnMindRemoved);
        }

        private void OnExamined(EntityUid uid, DroneComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
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
        }

        private void OnMobStateChanged(EntityUid uid, DroneComponent drone, MobStateChangedEvent args)
        {
            if (args.Component.IsDead())
            {
                var body = Comp<SharedBodyComponent>(uid); //There's no way something can have a mobstate but not a body...

                foreach (var item in drone.ToolUids)
                {
                    EntityManager.DeleteEntity(item);
                }
                body.Gib();
                EntityManager.DeleteEntity(uid);
            }
        }

        private void OnDisarmAttempt(EntityUid uid, DroneComponent drone, DisarmAttemptEvent args)
        {
            TryComp<HandsComponent>(args.TargetUid, out var hands);
            var item = hands?.GetActiveHandItem;
            if (TryComp<DroneToolComponent>(item?.Owner, out var itemInHand))
            {
                args.Cancel();
            }
        }

        private void OnMindAdded(EntityUid uid, DroneComponent drone, MindAddedMessage args)
        {
            TryComp<TagComponent>(uid, out var tagComp);
            UpdateDroneAppearance(uid, DroneStatus.On);
            tagComp?.AddTag("DoorBumpOpener");
            _popupSystem.PopupEntity(Loc.GetString("drone-activated"), uid, Filter.Pvs(uid));

            if (drone.AlreadyAwoken == false)
            {
                var spawnCoord = Transform(uid).Coordinates;

                if (drone.Tools.Count == 0) return;

                if (TryComp<HandsComponent>(uid, out var hands) && hands.Count >= drone.Tools.Count)
                {
                   foreach (var entry in drone.Tools)
                    {
                        var item = EntityManager.SpawnEntity(entry.PrototypeId, spawnCoord);
                        AddComp<DroneToolComponent>(item);
                        hands.PutInHand(item);
                        drone.ToolUids.Add(item);
                    }
                }

                drone.AlreadyAwoken = true;
            }
        }

        private void OnMindRemoved(EntityUid uid, DroneComponent drone, MindRemovedMessage args)
        {
            TryComp<TagComponent>(uid, out var tagComp);
            UpdateDroneAppearance(uid, DroneStatus.Off);
            tagComp?.RemoveTag("DoorBumpOpener");
            EnsureComp<GhostTakeoverAvailableComponent>(uid);
        }

        private void OnDropAttempt(EntityUid uid, DroneComponent drone, DropAttemptEvent args)
        {
            TryComp<HandsComponent>(uid, out var hands);
            var item = hands?.GetActiveHandItem;
            if (TryComp<DroneToolComponent>(item?.Owner, out var itemInHand))
            {
                args.Cancel();
            }
        }

        private void UpdateDroneAppearance(EntityUid uid, DroneStatus status)
        {
            if (TryComp<AppearanceComponent>(uid, out var appearance))
            {
                appearance.SetData(DroneVisuals.Status, status);
            }
        }
    }
}
