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
using Content.Server.Explosion.EntitySystems;
using Content.Server.Explosion;
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
        [Dependency] private readonly ExplosionSystem _explosions = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DroneComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<DroneComponent, DisarmAttemptEvent>(OnDisarmAttempt);
            SubscribeLocalEvent<DroneComponent, DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<DroneComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
            SubscribeLocalEvent<DroneComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<DroneComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<DroneComponent, MindRemovedMessage>(OnMindRemoved);
        }

        private void OnExamined(EntityUid uid, DroneComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                if (EntityManager.TryGetComponent<MindComponent>(uid, out var mind) && mind.HasMind)
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
                foreach (var item in drone.ToolUids)
                {
                        EntityManager.DeleteEntity(item);
                }
                _explosions.SpawnExplosion(uid);
                EntityManager.DeleteEntity(uid);
            }
        }

        private void OnDisarmAttempt(EntityUid uid, DroneComponent drone, DisarmAttemptEvent args)
        {
            EntityManager.TryGetComponent<HandsComponent>(args.TargetUid, out var hands);
            var item = hands.GetActiveHandItem;
            if (EntityManager.TryGetComponent<DroneToolComponent>(item?.Owner, out var itemInHand))
            {
            args.Cancel();
            }
        }

        private void OnMindAdded(EntityUid uid, DroneComponent drone, MindAddedMessage args)
        {
            EntityManager.TryGetComponent<TagComponent>(uid, out var tagComp);
            UpdateDroneAppearance(uid, DroneStatus.On);
            tagComp.AddTag("DoorBumpOpener");
            _popupSystem.PopupEntity(Loc.GetString("drone-activated"), uid, Filter.Pvs(uid));

            if (drone.alreadyAwoken == false)
            {
                var spawnCoord = Transform(uid).Coordinates;

                if (drone.Tools.Count == 0) return;

                if (EntityManager.TryGetComponent<HandsComponent>(uid, out var hands))
                {
                   foreach (var entry in drone.Tools)
                    {
                        var item = EntityManager.SpawnEntity(entry.PrototypeId, spawnCoord);
                        EntityManager.AddComponent<DroneToolComponent>(item);
                        hands.PutInHand(item);
                        drone.ToolUids.Add(item);
                    }
                }

                drone.alreadyAwoken = true;
            }
        }

        private void OnMindRemoved(EntityUid uid, DroneComponent drone, MindRemovedMessage args)
        {
            EntityManager.TryGetComponent<TagComponent>(uid, out var tagComp);
            UpdateDroneAppearance(uid, DroneStatus.Off);
            tagComp.RemoveTag("DoorBumpOpener");
            EntityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid);
        }

        private void OnDropAttempt(EntityUid uid, DroneComponent drone, DropAttemptEvent args)
        {
            EntityManager.TryGetComponent<HandsComponent>(uid, out var hands);
            var item = hands.GetActiveHandItem;
            if (EntityManager.TryGetComponent<DroneToolComponent>(item?.Owner, out var itemInHand))
            {
            args.Cancel();
            }
        }

        private void OnUnequipAttempt(EntityUid uid, DroneComponent drone, IsUnequippingAttemptEvent args)
        {
            if (EntityManager.TryGetComponent<DroneToolComponent>(args.UnEquipTarget, out var droneTool))
            {
            args.Cancel();
            }
        }

        private void UpdateDroneAppearance(EntityUid uid, DroneStatus status)
        {
            if (EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance))
            {
                appearance.SetData(DroneVisuals.Status, status);
            }
        }


    }
}
