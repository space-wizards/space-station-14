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
    public class WeightlessSystem : EntitySystem, IResettingEntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly Dictionary<GridId, List<ServerAlertsComponent>> _alerts = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GravityChangedMessage>(GravityChanged);
            SubscribeLocalEvent<EntParentChangedMessage>(EntParentChanged);
        }

        public void Reset()
        {
            _alerts.Clear();
        }

        public void AddAlert(ServerAlertsComponent status)
        {
            var gridId = status.Owner.Transform.GridID;
            var alerts = _alerts.GetOrNew(gridId);

            alerts.Add(status);

            if (_mapManager.TryGetGrid(status.Owner.Transform.GridID, out var grid))
            {
                var gridEntity = EntityManager.GetEntity(grid.GridEntityId);
                if (gridEntity.HasComponent<GravityComponent>())
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
            var grid = status.Owner.Transform.GridID;
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

        private void EntParentChanged(EntParentChangedMessage ev)
        {
            if (!ev.Entity.TryGetComponent(out ServerAlertsComponent? status))
            {
                return;
            }

            if (ev.OldParent != null &&
                ev.OldParent.TryGetComponent(out IMapGridComponent? mapGrid))
            {
                var oldGrid = mapGrid.GridIndex;

                if (_alerts.TryGetValue(oldGrid, out var oldStatuses))
                {
                    oldStatuses.Remove(status);
                }
            }

            var newGrid = ev.Entity.Transform.GridID;
            var newStatuses = _alerts.GetOrNew(newGrid);

            newStatuses.Add(status);
        }
    }
}
