using Content.Server.GameObjects.Components.Atmos;
using Content.Server.Interfaces.Atmos;
using Content.Shared.Atmos;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Content.Server.Atmos
{
    /// <inheritdoc cref="IAtmosphereMap"/>
    internal class AtmosphereMap : IAtmosphereMap, IPostInjectInit
    {
#pragma warning disable 649
        [Robust.Shared.IoC.Dependency] private readonly IMapManager _mapManager;
        [Robust.Shared.IoC.Dependency] private readonly IPauseManager _pauseManager;
#pragma warning restore 649

        private readonly Dictionary<GridId, GridAtmosphereManager> _gridAtmosphereManagers =
            new Dictionary<GridId, GridAtmosphereManager>();

        public void PostInject()
        {
            //_mapManager.OnGridCreated += OnGridCreated;
            //_mapManager.OnGridRemoved += OnGridRemoved;
            //_pauseManager.OnGridInitialize += OnGridInitialize;
            _mapManager.TileChanged += AtmosphereMapOnTileChanged;
        }

        public IGridAtmosphereManager GetGridAtmosphereManager(GridId grid)
        {
            if (_gridAtmosphereManagers.TryGetValue(grid, out var manager))
                return manager;

            if (!_mapManager.TryGetGrid(grid, out var gridInstance))
                throw new ArgumentException("Cannot get atmosphere of missing grid", nameof(grid));

            manager = new GridAtmosphereManager(gridInstance);
            _gridAtmosphereManagers[grid] = manager;
            return manager;
        }

        public void Update(float frameTime)
        {
            foreach (var (gridId, atmos) in _gridAtmosphereManagers)
            {
                if (_pauseManager.IsGridPaused(gridId))
                    continue;

                atmos.Update(frameTime);
            }
        }

        private void OnGridCreated(GridId gridId)
        {
            var gam = new GridAtmosphereManager(_mapManager.GetGrid(gridId));
            _gridAtmosphereManagers[gridId] = gam;
        }

        private void OnGridInitialize(GridId gridId)
        {
            if (!_gridAtmosphereManagers.TryGetValue(gridId, out var gam)) return;
        }

        private void OnGridRemoved(GridId gridId)
        {
            if (!_gridAtmosphereManagers.TryGetValue(gridId, out var gam)) return;
            gam.Dispose();
            _gridAtmosphereManagers.Remove(gridId);
        }

        private void AtmosphereMapOnTileChanged(object sender, TileChangedEventArgs eventArgs)
        {
            // When a tile changes, we want to update it only if it's gone from
            // space -> not space or vice versa. So if the old tile is the
            // same as the new tile in terms of space-ness, ignore the change

            if (eventArgs.NewTile.Tile.IsEmpty == eventArgs.OldTile.IsEmpty)
            {
                return;
            }

            if (!_gridAtmosphereManagers.TryGetValue(eventArgs.NewTile.GridIndex, out var gridManager))
            {
                return;
            }

            gridManager.Invalidate(eventArgs.NewTile.GridIndices);
        }
    }
}
