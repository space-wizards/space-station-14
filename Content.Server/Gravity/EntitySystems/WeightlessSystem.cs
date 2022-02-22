using System.Collections.Generic;
using Content.Shared.Alert;
using Content.Shared.GameTicking;
using Content.Shared.Gravity;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Gravity.EntitySystems
{
    [UsedImplicitly]
    public sealed class WeightlessSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;

        private readonly Dictionary<GridId, List<AlertsComponent>> _alerts = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<GravityChangedMessage>(GravityChanged);
            SubscribeLocalEvent<EntParentChangedMessage>(EntParentChanged);
            SubscribeLocalEvent<AlertsComponent, AlertSyncEvent>(HandleAlertSyncEvent);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            _alerts.Clear();
        }

        public void AddAlert(AlertsComponent status)
        {
            var gridId = EntityManager.GetComponent<TransformComponent>(status.Owner).GridID;
            var alerts = _alerts.GetOrNew(gridId);

            alerts.Add(status);

            if (_mapManager.TryGetGrid(EntityManager.GetComponent<TransformComponent>(status.Owner).GridID, out var grid))
            {
                if (EntityManager.GetComponent<GravityComponent>(grid.GridEntityId).Enabled)
                {
                    RemoveWeightless(status.Owner);
                }
                else
                {
                    AddWeightless(status.Owner);
                }
            }
        }

        public void RemoveAlert(AlertsComponent status)
        {
            var grid = EntityManager.GetComponent<TransformComponent>(status.Owner).GridID;
            if (!_alerts.TryGetValue(grid, out var statuses))
            {
                return;
            }

            statuses.Remove(status);
        }

        private void GravityChanged(GravityChangedMessage ev)
        {
            if (!_alerts.TryGetValue(ev.ChangedGridIndex, out var statuses))
            {
                return;
            }

            if (ev.HasGravity)
            {
                foreach (var status in statuses)
                {
                    RemoveWeightless(status.Owner);
                }
            }
            else
            {
                foreach (var status in statuses)
                {
                    AddWeightless(status.Owner);
                }
            }
        }

        private void AddWeightless(EntityUid euid)
        {
            _alertsSystem.ShowAlert(euid, AlertType.Weightless);
        }

        private void RemoveWeightless(EntityUid euid)
        {
            _alertsSystem.ClearAlert(euid, AlertType.Weightless);
        }

        private void EntParentChanged(ref EntParentChangedMessage ev)
        {
            if (!EntityManager.TryGetComponent(ev.Entity, out AlertsComponent? status))
            {
                return;
            }

            if (ev.OldParent is {Valid: true} old &&
                EntityManager.TryGetComponent(old, out IMapGridComponent? mapGrid))
            {
                var oldGrid = mapGrid.GridIndex;

                if (_alerts.TryGetValue(oldGrid, out var oldStatuses))
                {
                    oldStatuses.Remove(status);
                }
            }

            var newGrid = EntityManager.GetComponent<TransformComponent>(ev.Entity).GridID;
            var newStatuses = _alerts.GetOrNew(newGrid);

            newStatuses.Add(status);
        }

        private void HandleAlertSyncEvent(EntityUid uid, AlertsComponent component, AlertSyncEvent args)
        {
            switch (component.LifeStage)
            {
                case ComponentLifeStage.Starting:
                    AddAlert(component);
                    break;
                case ComponentLifeStage.Removing:
                    RemoveAlert(component);
                    break;
            }
        }
    }
}
