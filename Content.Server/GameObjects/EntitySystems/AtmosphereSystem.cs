#nullable enable
using Content.Server.Atmos;
using Content.Shared.GameObjects.EntitySystems.Atmos;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AtmosphereSystem : SharedAtmosphereSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPauseManager _pauseManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            _mapManager.TileChanged += OnTileChanged;
        }

        public IGridAtmosphereComponent? GetGridAtmosphere(GridId gridId)
        {
            // TODO Return space grid atmosphere for invalid grids or grids with no atmos
            var grid = _mapManager.GetGrid(gridId);

            if (!EntityManager.TryGetEntity(grid.GridEntityId, out var gridEnt)) return null;

            return gridEnt.TryGetComponent(out IGridAtmosphereComponent? atmos) ? atmos : null;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (mapGridComponent, gridAtmosphereComponent) in EntityManager.ComponentManager.EntityQuery<IMapGridComponent, IGridAtmosphereComponent>())
            {
                if (_pauseManager.IsGridPaused(mapGridComponent.GridIndex))
                    continue;

                gridAtmosphereComponent.Update(frameTime);
            }
        }

        private void OnTileChanged(object? sender, TileChangedEventArgs eventArgs)
        {
            // When a tile changes, we want to update it only if it's gone from
            // space -> not space or vice versa. So if the old tile is the
            // same as the new tile in terms of space-ness, ignore the change

            if (eventArgs.NewTile.Tile.IsEmpty == eventArgs.OldTile.IsEmpty)
            {
                return;
            }

            GetGridAtmosphere(eventArgs.NewTile.GridIndex)?.Invalidate(eventArgs.NewTile.GridIndices);
        }
    }
}
