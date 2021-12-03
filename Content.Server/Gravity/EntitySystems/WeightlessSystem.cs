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
            var gridId = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(status.Owner.Uid).GridID;
            var alerts = _alerts.GetOrNew(gridId);

            alerts.Add(status);

            if (_mapManager.TryGetGrid(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(status.Owner.Uid).GridID, out var grid))
            {
                var gridEntity = EntityManager.GetEntity(grid.GridEntityId);
                if (IoCManager.Resolve<IEntityManager>().GetComponent<GravityComponent>(gridEntity.Uid).Enabled)
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
            var grid = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(status.Owner.Uid).GridID;
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
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(ev.Entity.Uid, out ServerAlertsComponent? status))
            {
                return;
            }

            if (ev.OldParent != null &&
                IoCManager.Resolve<IEntityManager>().TryGetComponent(ev.OldParent.Uid, out IMapGridComponent? mapGrid))
            {
                var oldGrid = mapGrid.GridIndex;

                if (_alerts.TryGetValue(oldGrid, out var oldStatuses))
                {
                    oldStatuses.Remove(status);
                }
            }

            var newGrid = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ev.Entity.Uid).GridID;
            var newStatuses = _alerts.GetOrNew(newGrid);

            newStatuses.Add(status);
        }
    }
}
