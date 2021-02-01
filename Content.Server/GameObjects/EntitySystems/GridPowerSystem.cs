#nullable enable
using Content.Server.GameObjects.Components.Power;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class GridPowerSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override void Update(float frameTime)
        {
            foreach (var gridPower in ComponentManager.EntityQuery<IGridPowerComponent>(false))
            {
                gridPower.Update(frameTime);
            }
        }

        //TODO: what to do about null cases
        public IGridPowerComponent? GetGridPower(GridId gridId)
        {
            if (!gridId.IsValid())
                return null;

            var grid = _mapManager.GetGrid(gridId);

            if (!EntityManager.TryGetEntity(grid.GridEntityId, out var gridEnt))
                return null;

            gridEnt.TryGetComponent<IGridPowerComponent>(out var gridPower);

            return gridPower;
        }
    }
}
