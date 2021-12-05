using System.Collections.Generic;
using Content.Server.Alert;
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
    public class WeightlessSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly Dictionary<GridId, List<ServerAlertsComponent>> _alerts = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<GravityChangedMessage>(GravityChanged);
            SubscribeLocalEvent<EntParentChangedMessage>(EntParentChanged);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            _alerts.Clear();
        }

        public void AddAlert(ServerAlertsComponent status)
        {
            var gridId = EntityManager.GetComponent<TransformComponent>(status.Owner).GridID;
            var alerts = _alerts.GetOrNew(gridId);

            alerts.Add(status);

            if (_mapManager.TryGetGrid(EntityManager.GetComponent<TransformComponent>(status.Owner).GridID, out var grid))
            {
                if (EntityManager.GetComponent<GravityComponent>(grid.GridEntityId).Enabled)
                {
                    RemoveWeightless(status);
                }
                else
                {
                    AddWeightless(status);
                }
            }
        }

        public void RemoveAlert(ServerAlertsComponent status)
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
                    RemoveWeightless(status);
                }
            }
            else
            {
                foreach (var status in statuses)
                {
                    AddWeightless(status);
                }
            }
        }

        private void AddWeightless(ServerAlertsComponent status)
        {
            status.ShowAlert(AlertType.Weightless);
        }

        private void RemoveWeightless(ServerAlertsComponent status)
        {
            status.ClearAlert(AlertType.Weightless);
        }

        private void EntParentChanged(ref EntParentChangedMessage ev)
        {
            if (!EntityManager.TryGetComponent(ev.Entity, out ServerAlertsComponent? status))
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
    }
}
