using Content.Shared.Drone;
using Content.Shared.Drone.Components;
using Content.Shared.Examine;
using Content.Server.Popups;
using Content.Server.Mind.Components;
using Content.Server.Ghost.Roles.Components;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;


namespace Content.Server.Drone
{
    public class DroneSystem : SharedDroneSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

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

        private void OnMindAdded(EntityUid uid, DroneComponent drone, MindAddedMessage args)
        {
            UpdateDroneAppearance(uid, DroneStatus.On);
            _popupSystem.PopupEntity(Loc.GetString("drone-activated"), uid, Filter.Pvs(uid));
        }

        private void OnMindRemoved(EntityUid uid, DroneComponent drone, MindRemovedMessage args)
        {
            UpdateDroneAppearance(uid, DroneStatus.Off);
            EntityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid);
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
