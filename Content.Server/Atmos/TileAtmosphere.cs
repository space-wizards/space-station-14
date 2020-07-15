using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.Atmos
{
    public class TileAtmosphere
    {
        private int _archivedCycle = 0;
        private int _currentCycle = 0;
        public int AtmosCooldown { get; set; } = 0;
        public bool Excited { get; set; } = false;
        private IGridAtmosphereManager _gridAtmosphereManager;

        private readonly Dictionary<Direction, TileAtmosphere> _adjacentTiles = new Dictionary<Direction, TileAtmosphere>();

        public MapId MapIndex { get; }
        public GridId GridIndex { get; }
        public MapIndices GridIndices { get; }
        public Tile Tile { get; }
        public ExcitedGroup ExcitedGroup { get; set; }
        public GasMixture Air { get; set; }

        public TileAtmosphere(IGridAtmosphereManager atmosphereManager, TileRef tile, float volume)
        {
            _gridAtmosphereManager = atmosphereManager;
            MapIndex = tile.MapIndex;
            GridIndex = tile.GridIndex;
            GridIndices = tile.GridIndices;
            Tile = tile.Tile;

            if(_gridAtmosphereManager.IsSpace(GridIndices) ||
               _gridAtmosphereManager.IsAirBlocked(GridIndices)) return;

            // TODO ATMOS Load default gases from tile here or something
            Air = new GasMixture(volume);
            Air.Add("chem.O", Atmospherics.MolesCellStandard * 0.2f);
            Air.Add("chem.N", Atmospherics.MolesCellStandard * 0.8f);

            UpdateVisuals();
            UpdateAdjacent();
        }

        private void Archive(int fireCount)
        {
            _archivedCycle = fireCount;
            Air?.Archive();
        }

        public void ProcessCell(int fireCount)
        {
            // Can't process a tile without air
            if (Air == null) return;

            if (_archivedCycle < fireCount)
                Archive(fireCount);

            _currentCycle = fireCount;
            AtmosCooldown++;
            foreach (var (direction, enemyTile) in _adjacentTiles)
            {
                // If the tile is null or has no air, we don't do anything
                if(enemyTile?.Air == null) continue;
                if (fireCount <= enemyTile._currentCycle) continue;
                enemyTile.Archive(fireCount);

                var shouldShareAir = false;

                if (ExcitedGroup != null && enemyTile.ExcitedGroup != null)
                {
                    if (ExcitedGroup != enemyTile.ExcitedGroup)
                    {
                        ExcitedGroup.MergeGroups(enemyTile.ExcitedGroup);
                    }

                    shouldShareAir = true;
                } else if (Air.Compare(enemyTile.Air) != -2)
                {
                    if (!enemyTile.Excited)
                    {
                        _gridAtmosphereManager.AddActiveTile(enemyTile.GridIndices);
                    }

                    var excitedGroup = ExcitedGroup;
                    excitedGroup ??= enemyTile.ExcitedGroup;

                    if (excitedGroup == null)
                    {
                        excitedGroup = new ExcitedGroup();
                        excitedGroup.Initialize(_gridAtmosphereManager);
                    }

                    if (ExcitedGroup == null)
                        excitedGroup.AddTile(this);

                    if(enemyTile.ExcitedGroup == null)
                        excitedGroup.AddTile(enemyTile);

                    shouldShareAir = true;
                }

                if (shouldShareAir)
                {
                    LastShareCheck();
                }

                React();
                UpdateVisuals();
            }
        }

        public void ExplosivelyDepressurize(int cycleNum)
        {
            // TODO ATMOS Explosive depressurization? Woo!
            throw new NotImplementedException();
        }



        private void React()
        {
            // TODO ATMOS React
            //throw new System.NotImplementedException();
        }

        public void UpdateVisuals()
        {
            // TODO ATMOS Updating visuals
            var list = new List<SpriteSpecifier>();

            foreach (var (reagent, _) in Air.Gasses)
            {
                var overlay = reagent.GasOverlay;
                if(overlay == null) continue;
                list.Add(overlay);
            }

            if (list.Count == 0) return;

            EntitySystem.Get<TileOverlaySystem>().SetTileOverlay(GridIndex, GridIndices, list.ToArray());
        }

        public void UpdateAdjacent()
        {
            foreach (var direction in Cardinal())
            {
                _adjacentTiles[direction] = _gridAtmosphereManager.GetTile(GridIndices.Offset(direction));
            }
        }

        private void LastShareCheck()
        {
            var lastShare = Air.LastShare;
            if (lastShare > Atmospherics.MinimumAirToSuspend)
            {
                ExcitedGroup.ResetCooldowns();
                AtmosCooldown = 0;
            } else if (lastShare > Atmospherics.MinimumMolesDeltaToMove)
            {
                ExcitedGroup.DismantleCooldown = 0;
                AtmosCooldown = 0;
            }
        }

        private static IEnumerable<Direction> Cardinal() =>
            new[]
            {
                Direction.North, Direction.East, Direction.South, Direction.West
            };
    }
}
