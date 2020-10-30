using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystemMessages.Gravity;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class WeightlessSystem : EntitySystem, IResettingEntitySystem
    {
        private readonly Dictionary<GridId, List<ServerStatusEffectsComponent>> _statuses = new Dictionary<GridId, List<ServerStatusEffectsComponent>>();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GravityChangedMessage>(GravityChanged);
            SubscribeLocalEvent<EntParentChangedMessage>(EntParentChanged);
        }

        public void Reset()
        {
            _statuses.Clear();
        }

        public void AddStatus(ServerStatusEffectsComponent status)
        {
            var grid = status.Owner.Transform.GridID;
            var statuses = _statuses.GetOrNew(grid);

            statuses.Add(status);
        }

        public void RemoveStatus(ServerStatusEffectsComponent status)
        {
            var grid = status.Owner.Transform.GridID;
            if (!_statuses.TryGetValue(grid, out var statuses))
            {
                return;
            }

            statuses.Remove(status);
        }

        private void GravityChanged(GravityChangedMessage ev)
        {
            if (!_statuses.TryGetValue(ev.Grid.Index, out var statuses))
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

        private void AddWeightless(ServerStatusEffectsComponent status)
        {
            status.ChangeStatusEffect(StatusEffect.Weightless, "/Textures/Interface/StatusEffects/Weightless/weightless.png", null);
        }

        private void RemoveWeightless(ServerStatusEffectsComponent status)
        {
            status.RemoveStatusEffect(StatusEffect.Weightless);
        }

        private void EntParentChanged(EntParentChangedMessage ev)
        {
            if (!ev.Entity.TryGetComponent(out ServerStatusEffectsComponent status))
            {
                return;
            }

            if (ev.OldParent != null &&
                ev.OldParent.TryGetComponent(out IMapGridComponent mapGrid))
            {
                var oldGrid = mapGrid.GridIndex;

                if (_statuses.TryGetValue(oldGrid, out var oldStatuses))
                {
                    oldStatuses.Remove(status);
                }
            }

            var newGrid = ev.Entity.Transform.GridID;
            var newStatuses = _statuses.GetOrNew(newGrid);

            newStatuses.Add(status);
        }
    }
}
