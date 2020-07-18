using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.Atmos;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.EntitySystems;
using NFluidsynth;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Logger = Robust.Shared.Log.Logger;
using Math = CannyFastMath.Math;
using MathF = CannyFastMath.MathF;

namespace Content.Server.Atmos
{
    public class TileAtmosphere
    {
        private static long _eqQueueCycleCtr = 0;
        private int _archivedCycle = 0;
        private int _currentCycle = 0;

        public TileAtmosphere PressureSpecificTarget { get; set; } = null;
        public float PressureDifference { get; set; } = 0;
        public int AtmosCooldown { get; set; } = 0;
        public bool Excited { get; set; } = false;
        private IGridAtmosphereManager _gridAtmosphereManager;

        private readonly Dictionary<Direction, TileAtmosphere> _adjacentTiles = new Dictionary<Direction, TileAtmosphere>();

        private TileAtmosInfo _tileAtmosInfo = new TileAtmosInfo();
        private Direction _pressureDirection;

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

            if(_gridAtmosphereManager.IsAirBlocked(GridIndices)) return;

            // TODO ATMOS Load default gases from tile here or something
            Air = new GasMixture(volume);

            if (_gridAtmosphereManager.IsSpace(GridIndices))
                Air.MarkImmutable();
        }

        private void Archive(int fireCount)
        {
            _archivedCycle = fireCount;
            Air?.Archive();
        }

        public void HighPressureMovements()
        {
            // TODO ATMOS finish this

            if (PressureDifference > 100)
            {
                // Do space wind graphics here!
            }
        }

        public void EqualizePressureInZone(int cycleNum)
        {
            if (Air == null || (_tileAtmosInfo != null && _tileAtmosInfo.LastCycle >= cycleNum)) return; // Already done.

            _tileAtmosInfo = new TileAtmosInfo();

            var startingMoles = Air.TotalMoles;
            var runAtmos = false;

            // We need to figure if this is necessary
            foreach (var (direction, other) in _adjacentTiles.ToArray())
            {
                if (other?.Air == null) continue;
                var comparisonMoles = other.Air.TotalMoles;
                if (!(MathF.Abs(comparisonMoles - startingMoles) > Atmospherics.MinimumMolesDeltaToMove)) continue;
                runAtmos = true;
                break;
            }

            if (!runAtmos) // There's no need so we don't bother.
            {
                _tileAtmosInfo.LastCycle = cycleNum;
                return;
            }

            var queueCycle = ++_eqQueueCycleCtr;
            var totalMoles = 0f;
            var tiles = new List<TileAtmosphere> {this};
            _tileAtmosInfo.LastQueueCycle = queueCycle;
            var tileCount = 1;
            for (var i = 0; i < tileCount; i++)
            {
                if (i > Atmospherics.ZumosHardTileLimit) break;
                var exploring = tiles[i];

                if (i < Atmospherics.ZumosTileLimit)
                {
                    var tileMoles = exploring.Air.TotalMoles;
                    exploring._tileAtmosInfo.MoleDelta = tileMoles;
                    totalMoles += tileMoles;
                }

                foreach (var (direction, adj) in exploring._adjacentTiles)
                {
                    if (adj?.Air == null) continue;
                    if (adj._tileAtmosInfo != null)
                    {
                        if(adj._tileAtmosInfo.LastQueueCycle == queueCycle) continue;
                        adj._tileAtmosInfo = new TileAtmosInfo();
                    }
                    else
                    {
                        adj._tileAtmosInfo = new TileAtmosInfo();
                    }

                    adj._tileAtmosInfo.LastQueueCycle = queueCycle;
                    tiles.Add(adj);
                    tileCount++;
                    if (adj.Air.Immutable)
                    {
                        // Looks like someone opened an airlock to space!
                        ExplosivelyDepressurize(cycleNum);
                        return;
                    }
                }
            }

            if (tiles.Count > Atmospherics.ZumosTileLimit)
            {
                var count = 0;
                for (var i = Atmospherics.ZumosTileLimit; i < tiles.Count; i++)
                {
                    //We unmark them. We shouldn't be pushing/pulling gases to/from them.
                    tiles[i]._tileAtmosInfo.LastQueueCycle = 0;
                    count++;
                }
                tiles.RemoveRange(Atmospherics.ZumosTileLimit, count);
            }

            var averageMoles = totalMoles / (tiles.Count);
            var giverTiles = new List<TileAtmosphere>();
            var takerTiles = new List<TileAtmosphere>();

            foreach (var tile in tiles)
            {
                tile._tileAtmosInfo.LastCycle = cycleNum;
                tile._tileAtmosInfo.MoleDelta -= averageMoles;
                if (tile._tileAtmosInfo.MoleDelta > 0)
                {
                    giverTiles.Add(tile);
                }
                else
                {
                    takerTiles.Add(tile);
                }
            }

            var logN = MathF.Log2(tiles.Count);

            // Optimization - try to spread gases using an O(nlogn) algorithm that has a chance of not working first to avoid O(n^2)
            if (giverTiles.Count > logN && takerTiles.Count > logN)
            {
                // Even if it fails, it will speed up the next part.
                tiles.Sort((a, b) =>
                {
                    var aMoleDelta = a._tileAtmosInfo.MoleDelta;
                    var bMoleDelta = b._tileAtmosInfo.MoleDelta;

                    if (aMoleDelta != bMoleDelta)
                        return aMoleDelta.CompareTo(bMoleDelta);

                    if (aMoleDelta > 0)
                    {
                        return a._tileAtmosInfo.DistanceScore.CompareTo(b._tileAtmosInfo.DistanceScore);
                    }

                    return -a._tileAtmosInfo.DistanceScore.CompareTo(b._tileAtmosInfo.DistanceScore);
                });

                foreach (var tile in tiles)
                {
                    tile._tileAtmosInfo.FastDone = true;
                    if (tile._tileAtmosInfo.MoleDelta > 0)
                    {
                        Direction eligibleAdjBits = 0;
                        int amtEligibleAdj = 0;
                        foreach (var direction in Cardinal())
                        {
                            if (!tile._adjacentTiles.TryGetValue(direction, out var tile2)) continue;

                            // skip anything that isn't part of our current processing block. Original one didn't do this unfortunately, which probably cause some massive lag.
                            if (tile2?._tileAtmosInfo == null || tile2._tileAtmosInfo.FastDone ||
                                tile2._tileAtmosInfo.LastQueueCycle != queueCycle) continue;

                            eligibleAdjBits |= direction;
                            amtEligibleAdj++;
                        }

                        if (amtEligibleAdj <= 0) continue; // Oof we've painted ourselves into a corner. Bad luck. Next part will handle this.
                        var molesToMove = tile._tileAtmosInfo.MoleDelta / amtEligibleAdj;
                        foreach (var direction in Cardinal())
                        {
                            if((eligibleAdjBits & direction) == 0 || !tile._adjacentTiles.TryGetValue(direction, out var tile2) || tile2?._tileAtmosInfo == null) continue;
                            tile.AdjustEqMovement(direction, molesToMove);
                            tile._tileAtmosInfo.MoleDelta -= molesToMove;
                            tile2._tileAtmosInfo.MoleDelta += molesToMove;
                        }
                    }
                }

                giverTiles.Clear();
                takerTiles.Clear();

                foreach (var tile in tiles)
                {
                    if (tile._tileAtmosInfo.MoleDelta > 0)
                    {
                        giverTiles.Add(tile);
                    }
                    else
                    {
                        takerTiles.Add(tile);
                    }
                }

                // This is the part that can become O(n^2).
                if (giverTiles.Count < takerTiles.Count)
                {
                    // as an optimization, we choose one of two methods based on which list is smaller. We really want to avoid O(n^2) if we can.
                    var queue = new List<TileAtmosphere>(takerTiles.Count);
                    foreach (var giver in giverTiles)
                    {
                        giver._tileAtmosInfo.CurrentTransferDirection = (Direction)(-1);
                        giver._tileAtmosInfo.CurrentTransferAmount = 0;
                        var queueCycleSlow = ++_eqQueueCycleCtr;
                        queue.Clear();
                        queue.Add(giver);
                        giver._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                        var queueCount = queue.Count;
                        for (int i = 0; i < queueCount; i++)
                        {
                            if (giver._tileAtmosInfo.MoleDelta <= 0)
                                break; // We're done here now. Let's not do more work than needed.

                            var tile = queue[i];
                            foreach (var direction in Cardinal())
                            {
                                if(!tile._adjacentTiles.TryGetValue(direction, out var tile2)) continue;
                                if (giver._tileAtmosInfo.MoleDelta <= 0)
                                    break; // We're done here now. Let's not do more work than needed.

                                if (tile2?._tileAtmosInfo == null || tile2._tileAtmosInfo.LastQueueCycle != queueCycle)
                                    continue;

                                if (tile2._tileAtmosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                                queue.Add(tile2);
                                queueCount++;
                                tile2._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                                tile2._tileAtmosInfo.CurrentTransferDirection = direction.GetOpposite();
                                tile2._tileAtmosInfo.CurrentTransferAmount = 0;
                                if (tile2._tileAtmosInfo.MoleDelta < 0)
                                {
                                    // This tile needs gas. Let's give it to 'em.
                                    if (-tile2._tileAtmosInfo.MoleDelta > giver._tileAtmosInfo.MoleDelta)
                                    {
                                        // We don't have enough gas!
                                        tile2._tileAtmosInfo.CurrentTransferAmount -= giver._tileAtmosInfo.MoleDelta;
                                        tile2._tileAtmosInfo.MoleDelta += giver._tileAtmosInfo.MoleDelta;
                                        giver._tileAtmosInfo.MoleDelta = 0;
                                    }
                                    else
                                    {
                                        // We have enough gas.
                                        tile2._tileAtmosInfo.CurrentTransferAmount += tile2._tileAtmosInfo.MoleDelta;
                                        giver._tileAtmosInfo.MoleDelta += tile2._tileAtmosInfo.MoleDelta;
                                        tile2._tileAtmosInfo.MoleDelta = 0;
                                    }
                                }
                            }
                        }

                        // Putting this loop here helps make it O(n^2) over O(n^3)
                        for (var i = queue.Count - 1; i >= 0; i--)
                        {
                            var tile = queue[i];
                            if (tile._tileAtmosInfo.CurrentTransferAmount != 0 &&
                                tile._tileAtmosInfo.CurrentTransferDirection != (Direction)(-1))
                            {
                                tile.AdjustEqMovement(tile._tileAtmosInfo.CurrentTransferDirection, tile._tileAtmosInfo.CurrentTransferAmount);
                                tile._adjacentTiles[tile._tileAtmosInfo.CurrentTransferDirection]._tileAtmosInfo
                                    .CurrentTransferAmount += tile._tileAtmosInfo.CurrentTransferAmount;
                                tile._tileAtmosInfo.CurrentTransferAmount = 0;
                            }
                        }
                    }
                }
                else
                {
                    var queue = new List<TileAtmosphere>(giverTiles.Count);
                    foreach (var taker in takerTiles)
                    {
                        taker._tileAtmosInfo.CurrentTransferDirection = (Direction) (-1);
                        taker._tileAtmosInfo.CurrentTransferAmount = 0;
                        var queueCycleSlow = ++_eqQueueCycleCtr;
                        queue.Clear();
                        queue.Add(taker);
                        taker._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                        var queueCount = queue.Count;
                        for (int i = 0; i < queueCount; i++)
                        {
                            if (taker._tileAtmosInfo.MoleDelta >= 0)
                                break; // We're done here now. Let's not do more work than needed.

                            var tile = queue[i];
                            foreach (var direction in Cardinal())
                            {
                                if(!tile._adjacentTiles.ContainsKey(direction)) continue;
                                var tile2 = tile._adjacentTiles[direction];

                                if (taker._tileAtmosInfo.MoleDelta >= 0)
                                    break; // We're done here now. Let's not do more work than needed.

                                if (tile2?._tileAtmosInfo == null || tile2._tileAtmosInfo.LastQueueCycle != queueCycle) continue;
                                if (tile2._tileAtmosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                                queue.Add(tile2);
                                queueCount++;
                                tile2._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                                tile2._tileAtmosInfo.CurrentTransferDirection = direction.GetOpposite();
                                tile2._tileAtmosInfo.CurrentTransferAmount = 0;

                                if (tile2._tileAtmosInfo.MoleDelta > 0)
                                {
                                    // This tile has gas we can suck, so let's
                                    if (tile2._tileAtmosInfo.MoleDelta > -taker._tileAtmosInfo.MoleDelta)
                                    {
                                        // They have enough gas
                                        tile2._tileAtmosInfo.CurrentTransferAmount -= taker._tileAtmosInfo.MoleDelta;
                                        tile2._tileAtmosInfo.MoleDelta += taker._tileAtmosInfo.MoleDelta;
                                        taker._tileAtmosInfo.MoleDelta = 0;
                                    }
                                    else
                                    {
                                        // They don't have enough gas!
                                        tile2._tileAtmosInfo.CurrentTransferAmount += tile2._tileAtmosInfo.MoleDelta;
                                        taker._tileAtmosInfo.MoleDelta += tile2._tileAtmosInfo.MoleDelta;
                                        tile2._tileAtmosInfo.MoleDelta = 0;
                                    }
                                }
                            }
                        }

                        for (var i = queue.Count - 1; i >= 0; i--)
                        {
                            var tile = queue[i];
                            if (tile._tileAtmosInfo.CurrentTransferAmount != 0 &&
                                tile._tileAtmosInfo.CurrentTransferDirection != (Direction) (-1))
                            {
                                tile.AdjustEqMovement(tile._tileAtmosInfo.CurrentTransferDirection, tile._tileAtmosInfo.CurrentTransferAmount);
                                tile._adjacentTiles[tile._tileAtmosInfo.CurrentTransferDirection]._tileAtmosInfo
                                    .CurrentTransferAmount += tile._tileAtmosInfo.CurrentTransferAmount;
                                tile._tileAtmosInfo.CurrentTransferAmount = 0;
                            }
                        }
                    }
                }

                foreach (var tile in tiles)
                {
                    tile.FinalizeEq();
                }

                foreach (var tile in tiles)
                {
                    foreach (var direction in Cardinal())
                    {
                        if (!tile._adjacentTiles.TryGetValue(direction, out var tile2)) continue;
                        if(tile2?.Air == null) continue;
                        if (tile2.Air.Compare(Air) != -2)
                        {
                            _gridAtmosphereManager.AddActiveTile(tile2.GridIndices);
                            break;
                        }
                    }
                }
            }
        }

        private void FinalizeEq()
        {
            var transferDirections = new Dictionary<Direction, float>(_tileAtmosInfo.TransferDirections);
            var hasTransferDirs = false;
            foreach (var (direction, amount) in transferDirections)
            {
                if (amount == 0) continue;
                hasTransferDirs = true;
                break;
            }

            foreach (var direction in Cardinal())
            {
                if (!transferDirections.TryGetValue(direction, out var amount)) continue;
                var tile = _adjacentTiles[direction];
                if (tile?.Air == null) continue;
                if (amount > 0)
                {
                    if (Air.TotalMoles < amount)
                        FinalizeEqNeighbors(ref transferDirections);

                    tile._tileAtmosInfo.TransferDirections[direction.GetOpposite()] = 0;
                    tile.Air.Merge(Air.Remove(amount));
                    UpdateVisuals();
                    tile.UpdateVisuals();
                    ConsiderPressureDifference(tile, amount);
                }
            }
        }

        private void ConsiderPressureDifference(TileAtmosphere tile, float difference)
        {
            _gridAtmosphereManager.AddHighPressureDelta(GridIndices);
            if (difference > PressureDifference)
            {
                PressureDifference = difference;
                _pressureDirection = ((Vector2i) (tile.GridIndices - GridIndices)).GetDir();
            }
        }

        private void FinalizeEqNeighbors(ref Dictionary<Direction,float> transferDirections)
        {
            // TODO ATMOS infinite recursion here for some reason
            return;
            foreach (var direction in Cardinal())
            {
                if (!transferDirections.TryGetValue(direction, out var amount)) continue;
                if(amount < 0 && _adjacentTiles.TryGetValue(direction, out var adjacent))
                    adjacent.FinalizeEq();
            }
        }

        private void AdjustEqMovement(Direction direction, float molesToMove)
        {
            _tileAtmosInfo.TransferDirections[direction] += molesToMove;
            if(direction != (Direction)(-1) && _adjacentTiles.TryGetValue(direction, out var adj) && adj?._tileAtmosInfo != null)
                _adjacentTiles[direction]._tileAtmosInfo.TransferDirections[direction.GetOpposite()] -= molesToMove;
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
            if (Air == null) return;
            var totalGasesRemoved = 0f;
            long queueCycle = ++_eqQueueCycleCtr;
            var tiles = new List<TileAtmosphere>();
            var spaceTiles = new List<TileAtmosphere>();
            tiles.Add(this);
            _tileAtmosInfo = new TileAtmosInfo();
            _tileAtmosInfo.LastQueueCycle = queueCycle;
            _tileAtmosInfo.CurrentTransferDirection = (Direction) (-1);
            var tileCount = 1;
            for (var i = 0; i < tileCount; i++)
            {
                var tile = tiles[i];
                tile._tileAtmosInfo.LastCycle = cycleNum;
                tile._tileAtmosInfo.CurrentTransferDirection = (Direction) (-1);
                if (tile.Air.Immutable)
                {
                    spaceTiles.Add(tile);
                    tile.PressureSpecificTarget = tile;
                }
                else
                {
                    if (i > Atmospherics.ZumosHardTileLimit) continue;
                    foreach (var direction in Cardinal())
                    {
                        if (!_adjacentTiles.TryGetValue(direction, out var tile2)) continue;
                        if (tile2?.Air == null) continue;
                        if (tile2._tileAtmosInfo != null && tile2._tileAtmosInfo.LastQueueCycle == queueCycle) continue;
                        tile.ConsiderFirelocks(tile2);
                        if (tile._adjacentTiles[direction]?.Air != null)
                        {
                            tile2._tileAtmosInfo = new TileAtmosInfo {LastQueueCycle = queueCycle};
                            tiles.Add(tile2);
                            tileCount++;
                        }
                    }
                }
            }

            var queueCycleSlow = ++_eqQueueCycleCtr;
            var progressionOrder = new List<TileAtmosphere>();
            foreach (var tile in spaceTiles)
            {
                progressionOrder.Add(tile);
                tile._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                tile._tileAtmosInfo.CurrentTransferDirection = (Direction) (-1);
            }

            var progressionCount = progressionOrder.Count;
            for (int i = 0; i < progressionCount; i++)
            {
                var tile = progressionOrder[i];
                foreach (var direction in Cardinal())
                {
                    if (!_adjacentTiles.TryGetValue(direction, out var tile2)) continue;
                    if (tile2?._tileAtmosInfo == null || tile2._tileAtmosInfo.LastQueueCycle != queueCycle) continue;
                    if (tile2._tileAtmosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                    if(tile2.Air.Immutable) continue;
                    tile2._tileAtmosInfo.CurrentTransferDirection = direction.GetOpposite();
                    tile2._tileAtmosInfo.CurrentTransferAmount = 0;
                    tile2.PressureSpecificTarget = tile.PressureSpecificTarget;
                    tile2._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                    progressionOrder.Add(tile2);
                    progressionCount++;
                }
            }

            for (int i = 0; i < progressionCount; i++)
            {
                var tile = progressionOrder[i];
                if (tile._tileAtmosInfo.CurrentTransferDirection == (Direction) (-1)) continue;
                var hpdLength = _gridAtmosphereManager.HighPressureDeltaCount;
                var inHdp = _gridAtmosphereManager.HasHighPressureDelta(tile.GridIndices);
                if(!inHdp)
                    _gridAtmosphereManager.AddHighPressureDelta(tile.GridIndices);
                var tile2 = tile._adjacentTiles[tile._tileAtmosInfo.CurrentTransferDirection];
                if (tile2?.Air == null) continue;
                var sum = tile2.Air.TotalMoles;
                totalGasesRemoved += sum;
                tile._tileAtmosInfo.CurrentTransferAmount += sum;
                tile2._tileAtmosInfo.CurrentTransferAmount += tile._tileAtmosInfo.CurrentTransferAmount;
                tile.PressureDifference = tile._tileAtmosInfo.CurrentTransferAmount;
                tile._pressureDirection = tile._tileAtmosInfo.CurrentTransferDirection;
                if (tile2._tileAtmosInfo.CurrentTransferDirection == (Direction) (-1))
                {
                    tile2.PressureDifference = tile2._tileAtmosInfo.CurrentTransferAmount;
                    tile2._pressureDirection = tile._tileAtmosInfo.CurrentTransferDirection;
                }
                tile.Air.Clear();
                tile.UpdateVisuals();
                tile.FloorRip(sum);
            }
        }

        private void FloorRip(float sum)
        {
            Logger.Info($"Floor rip! SUM: {sum} CHANCE: {MathF.Clamp(sum / 100, 0, 1)}");
            if (sum > 20 && IoCManager.Resolve<IRobustRandom>().Prob(MathF.Clamp(sum / 100, 0, 1)))
                _gridAtmosphereManager.PryTile(GridIndices);
        }

        private void ConsiderFirelocks(TileAtmosphere other)
        {
            // TODO ATMOS firelocks!
            //throw new NotImplementedException();
        }


        private void React()
        {
            // TODO ATMOS React
            //throw new System.NotImplementedException();
        }

        public void UpdateVisuals()
        {
            if (Air == null) return;

            // TODO ATMOS Updating visuals
            var list = new List<SharedGasTileOverlaySystem.GasData>();
            var gases = Air.Gases;

            for(var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gas = Atmospherics.GetGas(i);
                var moles = gases[i];
                var overlay = gas.GasOverlay;
                if(moles == 0f || overlay == null || moles < gas.GasMolesVisible) continue;
                list.Add(new SharedGasTileOverlaySystem.GasData(i, MathF.Max(MathF.Min(1, moles / Atmospherics.GasMolesVisibleMax), 0f)));
            }

            if (list.Count == 0) return;

            EntitySystem.Get<GasTileOverlaySystem>().SetTileOverlay(GridIndex, GridIndices, list.ToArray());
        }

        public void UpdateAdjacent()
        {
            foreach (var direction in Cardinal())
            {
                _adjacentTiles[direction] = _gridAtmosphereManager.GetTile(GridIndices.Offset(direction));
            }
        }

        public void UpdateAdjacent(Direction direction)
        {
            _adjacentTiles[direction] = _gridAtmosphereManager.GetTile(GridIndices.Offset(direction));
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
