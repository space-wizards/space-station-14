using Content.Server.Atmos.Components;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Database;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        [Dependency] private readonly FirelockSystem _firelockSystem = default!;

        private readonly TileAtmosphereComparer _monstermosComparer = new();

        private readonly TileAtmosphere?[] _equalizeTiles = new TileAtmosphere[Atmospherics.MonstermosHardTileLimit];
        private readonly TileAtmosphere[] _equalizeGiverTiles = new TileAtmosphere[Atmospherics.MonstermosTileLimit];
        private readonly TileAtmosphere[] _equalizeTakerTiles = new TileAtmosphere[Atmospherics.MonstermosTileLimit];
        private readonly TileAtmosphere[] _equalizeQueue = new TileAtmosphere[Atmospherics.MonstermosTileLimit];
        private readonly TileAtmosphere[] _depressurizeTiles = new TileAtmosphere[Atmospherics.MonstermosHardTileLimit];
        private readonly TileAtmosphere[] _depressurizeSpaceTiles = new TileAtmosphere[Atmospherics.MonstermosHardTileLimit];
        private readonly TileAtmosphere[] _depressurizeProgressionOrder = new TileAtmosphere[Atmospherics.MonstermosHardTileLimit * 2];

        private void EqualizePressureInZone(MapGridComponent mapGrid, GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, int cycleNum, GasTileOverlayComponent? visuals)
        {
            if (tile.Air == null || (tile.MonstermosInfo.LastCycle >= cycleNum))
                return; // Already done.

            tile.MonstermosInfo = new MonstermosInfo();

            var startingMoles = tile.Air.TotalMoles;
            var runAtmos = false;

            // We need to figure if this is necessary
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (!tile.AdjacentBits.IsFlagSet(direction)) continue;
                var other = tile.AdjacentTiles[i];
                if (other?.Air == null) continue;
                var comparisonMoles = other.Air.TotalMoles;
                if (!(MathF.Abs(comparisonMoles - startingMoles) > Atmospherics.MinimumMolesDeltaToMove)) continue;
                runAtmos = true;
                break;
            }

            if (!runAtmos) // There's no need so we don't bother.
            {
                tile.MonstermosInfo.LastCycle = cycleNum;
                return;
            }

            var queueCycle = ++gridAtmosphere.EqualizationQueueCycleControl;
            var totalMoles = 0f;
            _equalizeTiles[0] = tile;
            tile.MonstermosInfo.LastQueueCycle = queueCycle;
            var tileCount = 1;
            for (var i = 0; i < tileCount; i++)
            {
                if (i > Atmospherics.MonstermosHardTileLimit) break;
                var exploring = _equalizeTiles[i]!;

                if (i < Atmospherics.MonstermosTileLimit)
                {
                    // Tiles in the _equalizeTiles array cannot have null air.
                    var tileMoles = exploring.Air!.TotalMoles;
                    exploring.MonstermosInfo.MoleDelta = tileMoles;
                    totalMoles += tileMoles;
                }

                for (var j = 0; j < Atmospherics.Directions; j++)
                {
                    var direction = (AtmosDirection) (1 << j);
                    if (!exploring.AdjacentBits.IsFlagSet(direction)) continue;
                    var adj = exploring.AdjacentTiles[j];
                    if (adj?.Air == null) continue;
                    if(adj.MonstermosInfo.LastQueueCycle == queueCycle) continue;
                    adj.MonstermosInfo = new MonstermosInfo {LastQueueCycle = queueCycle};

                    if(tileCount < Atmospherics.MonstermosHardTileLimit)
                        _equalizeTiles[tileCount++] = adj;

                    if (adj.Space && MonstermosDepressurization)
                    {
                        // Looks like someone opened an airlock to space!

                        ExplosivelyDepressurize(mapGrid, gridAtmosphere, tile, cycleNum, visuals);
                        return;
                    }
                }
            }

            if (tileCount > Atmospherics.MonstermosTileLimit)
            {
                for (var i = Atmospherics.MonstermosTileLimit; i < tileCount; i++)
                {
                    //We unmark them. We shouldn't be pushing/pulling gases to/from them.
                    var otherTile = _equalizeTiles[i];

                    if (otherTile == null)
                        continue;

                    otherTile.MonstermosInfo.LastQueueCycle = 0;
                }

                tileCount = Atmospherics.MonstermosTileLimit;
            }

            var averageMoles = totalMoles / (tileCount);
            var giverTilesLength = 0;
            var takerTilesLength = 0;

            for (var i = 0; i < tileCount; i++)
            {
                var otherTile = _equalizeTiles[i]!;
                otherTile.MonstermosInfo.LastCycle = cycleNum;
                otherTile.MonstermosInfo.MoleDelta -= averageMoles;
                if (otherTile.MonstermosInfo.MoleDelta > 0)
                {
                    _equalizeGiverTiles[giverTilesLength++] = otherTile;
                }
                else
                {
                    _equalizeTakerTiles[takerTilesLength++] = otherTile;
                }
            }

            var logN = MathF.Log2(tileCount);

            // Optimization - try to spread gases using an O(n log n) algorithm that has a chance of not working first to avoid O(n^2)
            if (giverTilesLength > logN && takerTilesLength > logN)
            {
                // Even if it fails, it will speed up the next part.
                Array.Sort(_equalizeTiles, 0, tileCount, _monstermosComparer);

                for (var i = 0; i < tileCount; i++)
                {
                    var otherTile = _equalizeTiles[i]!;
                    otherTile.MonstermosInfo.FastDone = true;
                    if (!(otherTile.MonstermosInfo.MoleDelta > 0)) continue;
                    var eligibleDirections = AtmosDirection.Invalid;
                    var eligibleDirectionCount = 0;
                    for (var j = 0; j < Atmospherics.Directions; j++)
                    {
                        var direction = (AtmosDirection) (1 << j);
                        if (!otherTile.AdjacentBits.IsFlagSet(direction)) continue;
                        var tile2 = otherTile.AdjacentTiles[j]!;
                        DebugTools.Assert(tile2.AdjacentBits.IsFlagSet(direction.GetOpposite()));

                        // skip anything that isn't part of our current processing block.
                        if (tile2.MonstermosInfo.FastDone || tile2.MonstermosInfo.LastQueueCycle != queueCycle)
                            continue;

                        eligibleDirections |= direction;
                        eligibleDirectionCount++;
                    }

                    if (eligibleDirectionCount <= 0)
                        continue; // Oof we've painted ourselves into a corner. Bad luck. Next part will handle this.

                    var molesToMove = otherTile.MonstermosInfo.MoleDelta / eligibleDirectionCount;
                    for (var j = 0; j < Atmospherics.Directions; j++)
                    {
                        var direction = (AtmosDirection) (1 << j);
                        if (!eligibleDirections.IsFlagSet(direction)) continue;

                        AdjustEqMovement(otherTile, direction, molesToMove);
                        otherTile.MonstermosInfo.MoleDelta -= molesToMove;
                        otherTile.AdjacentTiles[j]!.MonstermosInfo.MoleDelta += molesToMove;
                    }
                }

                giverTilesLength = 0;
                takerTilesLength = 0;

                for (var i = 0; i < tileCount; i++)
                {
                    var otherTile = _equalizeTiles[i]!;
                    if (otherTile.MonstermosInfo.MoleDelta > 0)
                    {
                        _equalizeGiverTiles[giverTilesLength++] = otherTile;
                    }
                    else
                    {
                        _equalizeTakerTiles[takerTilesLength++] = otherTile;
                    }
                }
            }

            // This is the part that can become O(n^2).
            if (giverTilesLength < takerTilesLength)
            {
                // as an optimization, we choose one of two methods based on which list is smaller. We really want to avoid O(n^2) if we can.
                for (var j = 0; j < giverTilesLength; j++)
                {
                    var giver = _equalizeGiverTiles[j];
                    giver.MonstermosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
                    giver.MonstermosInfo.CurrentTransferAmount = 0;
                    var queueCycleSlow = ++gridAtmosphere.EqualizationQueueCycleControl;
                    var queueLength = 0;
                    _equalizeQueue[queueLength++] = giver;
                    giver.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                    for (var i = 0; i < queueLength; i++)
                    {
                        if (giver.MonstermosInfo.MoleDelta <= 0)
                            break; // We're done here now. Let's not do more work than needed.

                        var otherTile = _equalizeQueue[i];
                        for (var k = 0; k < Atmospherics.Directions; k++)
                        {
                            var direction = (AtmosDirection) (1 << k);
                            if (!otherTile.AdjacentBits.IsFlagSet(direction)) continue;
                            var otherTile2 = otherTile.AdjacentTiles[k];
                            if (giver.MonstermosInfo.MoleDelta <= 0) break; // We're done here now. Let's not do more work than needed.
                            if (otherTile2 == null || otherTile2.MonstermosInfo.LastQueueCycle != queueCycle) continue;
                            DebugTools.Assert(otherTile2.AdjacentBits.IsFlagSet(direction.GetOpposite()));
                            if (otherTile2.MonstermosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                            _equalizeQueue[queueLength++] = otherTile2;
                            otherTile2.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                            otherTile2.MonstermosInfo.CurrentTransferDirection = direction.GetOpposite();
                            otherTile2.MonstermosInfo.CurrentTransferAmount = 0;
                            if (otherTile2.MonstermosInfo.MoleDelta < 0)
                            {
                                // This tile needs gas. Let's give it to 'em.
                                if (-otherTile2.MonstermosInfo.MoleDelta > giver.MonstermosInfo.MoleDelta)
                                {
                                    // We don't have enough gas!
                                    otherTile2.MonstermosInfo.CurrentTransferAmount -= giver.MonstermosInfo.MoleDelta;
                                    otherTile2.MonstermosInfo.MoleDelta += giver.MonstermosInfo.MoleDelta;
                                    giver.MonstermosInfo.MoleDelta = 0;
                                }
                                else
                                {
                                    // We have enough gas.
                                    otherTile2.MonstermosInfo.CurrentTransferAmount += otherTile2.MonstermosInfo.MoleDelta;
                                    giver.MonstermosInfo.MoleDelta += otherTile2.MonstermosInfo.MoleDelta;
                                    otherTile2.MonstermosInfo.MoleDelta = 0;
                                }
                            }
                        }
                    }

                    // Putting this loop here helps make it O(n^2) over O(n^3)
                    for (var i = queueLength - 1; i >= 0; i--)
                    {
                        var otherTile = _equalizeQueue[i];
                        if (otherTile.MonstermosInfo.CurrentTransferAmount != 0 && otherTile.MonstermosInfo.CurrentTransferDirection != AtmosDirection.Invalid)
                        {
                            AdjustEqMovement(otherTile, otherTile.MonstermosInfo.CurrentTransferDirection, otherTile.MonstermosInfo.CurrentTransferAmount);
                            otherTile.AdjacentTiles[otherTile.MonstermosInfo.CurrentTransferDirection.ToIndex()]!
                                .MonstermosInfo.CurrentTransferAmount += otherTile.MonstermosInfo.CurrentTransferAmount;
                            otherTile.MonstermosInfo.CurrentTransferAmount = 0;
                        }
                    }
                }
            }
            else
            {
                for (var j = 0; j < takerTilesLength; j++)
                {
                    var taker = _equalizeTakerTiles[j];
                    taker.MonstermosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
                    taker.MonstermosInfo.CurrentTransferAmount = 0;
                    var queueCycleSlow = ++gridAtmosphere.EqualizationQueueCycleControl;
                    var queueLength = 0;
                    _equalizeQueue[queueLength++] = taker;
                    taker.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                    for (var i = 0; i < queueLength; i++)
                    {
                        if (taker.MonstermosInfo.MoleDelta >= 0)
                            break; // We're done here now. Let's not do more work than needed.

                        var otherTile = _equalizeQueue[i];
                        for (var k = 0; k < Atmospherics.Directions; k++)
                        {
                            var direction = (AtmosDirection) (1 << k);
                            if (!otherTile.AdjacentBits.IsFlagSet(direction)) continue;
                            var otherTile2 = otherTile.AdjacentTiles[k];

                            if (taker.MonstermosInfo.MoleDelta >= 0) break; // We're done here now. Let's not do more work than needed.
                            if (otherTile2 == null || otherTile2.MonstermosInfo.LastQueueCycle != queueCycle) continue;
                            DebugTools.Assert(otherTile2.AdjacentBits.IsFlagSet(direction.GetOpposite()));
                            if (otherTile2.MonstermosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                            _equalizeQueue[queueLength++] = otherTile2;
                            otherTile2.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                            otherTile2.MonstermosInfo.CurrentTransferDirection = direction.GetOpposite();
                            otherTile2.MonstermosInfo.CurrentTransferAmount = 0;

                            if (otherTile2.MonstermosInfo.MoleDelta > 0)
                            {
                                // This tile has gas we can suck, so let's
                                if (otherTile2.MonstermosInfo.MoleDelta > -taker.MonstermosInfo.MoleDelta)
                                {
                                    // They have enough gas
                                    otherTile2.MonstermosInfo.CurrentTransferAmount -= taker.MonstermosInfo.MoleDelta;
                                    otherTile2.MonstermosInfo.MoleDelta += taker.MonstermosInfo.MoleDelta;
                                    taker.MonstermosInfo.MoleDelta = 0;
                                }
                                else
                                {
                                    // They don't have enough gas!
                                    otherTile2.MonstermosInfo.CurrentTransferAmount += otherTile2.MonstermosInfo.MoleDelta;
                                    taker.MonstermosInfo.MoleDelta += otherTile2.MonstermosInfo.MoleDelta;
                                    otherTile2.MonstermosInfo.MoleDelta = 0;
                                }
                            }
                        }
                    }

                    for (var i = queueLength - 1; i >= 0; i--)
                    {
                        var otherTile = _equalizeQueue[i];
                        if (otherTile.MonstermosInfo.CurrentTransferAmount == 0 || otherTile.MonstermosInfo.CurrentTransferDirection == AtmosDirection.Invalid)
                            continue;

                        AdjustEqMovement(otherTile, otherTile.MonstermosInfo.CurrentTransferDirection, otherTile.MonstermosInfo.CurrentTransferAmount);

                        otherTile.AdjacentTiles[otherTile.MonstermosInfo.CurrentTransferDirection.ToIndex()]!
                            .MonstermosInfo.CurrentTransferAmount += otherTile.MonstermosInfo.CurrentTransferAmount;
                        otherTile.MonstermosInfo.CurrentTransferAmount = 0;
                    }
                }
            }

            for (var i = 0; i < tileCount; i++)
            {
                var otherTile = _equalizeTiles[i]!;
                FinalizeEq(gridAtmosphere, otherTile, visuals);
            }

            for (var i = 0; i < tileCount; i++)
            {
                var otherTile = _equalizeTiles[i]!;
                for (var j = 0; j < Atmospherics.Directions; j++)
                {
                    var direction = (AtmosDirection) (1 << j);
                    if (!otherTile.AdjacentBits.IsFlagSet(direction)) continue;
                    var otherTile2 = otherTile.AdjacentTiles[j]!;
                    DebugTools.Assert(otherTile2.AdjacentBits.IsFlagSet(direction.GetOpposite()));
                    if (otherTile2.Air != null && CompareExchange(otherTile2.Air, tile.Air) == GasCompareResult.NoExchange) continue;
                    AddActiveTile(gridAtmosphere, otherTile2);
                    break;
                }
            }

            // We do cleanup.
            Array.Clear(_equalizeTiles, 0, Atmospherics.MonstermosHardTileLimit);
            Array.Clear(_equalizeGiverTiles, 0, Atmospherics.MonstermosTileLimit);
            Array.Clear(_equalizeTakerTiles, 0, Atmospherics.MonstermosTileLimit);
            Array.Clear(_equalizeQueue, 0, Atmospherics.MonstermosTileLimit);
        }

        private void ExplosivelyDepressurize(MapGridComponent mapGrid, GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, int cycleNum, GasTileOverlayComponent? visuals)
        {
            // Check if explosive depressurization is enabled and if the tile is valid.
            if (!MonstermosDepressurization || tile.Air == null)
                return;

            const int limit = Atmospherics.MonstermosHardTileLimit;

            var totalMolesRemoved = 0f;
            var queueCycle = ++gridAtmosphere.EqualizationQueueCycleControl;

            var tileCount = 0;
            var spaceTileCount = 0;

            _depressurizeTiles[tileCount++] = tile;

            tile.MonstermosInfo = new MonstermosInfo {LastQueueCycle = queueCycle};

            for (var i = 0; i < tileCount; i++)
            {
                var otherTile = _depressurizeTiles[i];
                otherTile.MonstermosInfo.LastCycle = cycleNum;
                otherTile.MonstermosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
                // Tiles in the _depressurizeTiles array cannot have null air.
                if (!otherTile.Space)
                {
                    for (var j = 0; j < Atmospherics.Directions; j++)
                    {
                        var direction = (AtmosDirection) (1 << j);
                        if (!otherTile.AdjacentBits.IsFlagSet(direction)) continue;
                        var otherTile2 = otherTile.AdjacentTiles[j];
                        if (otherTile2?.Air == null) continue;
                        DebugTools.Assert(otherTile2.AdjacentBits.IsFlagSet(direction.GetOpposite()));
                        if (otherTile2.MonstermosInfo.LastQueueCycle == queueCycle) continue;

                        ConsiderFirelocks(gridAtmosphere, otherTile, otherTile2, visuals, mapGrid);

                        // The firelocks might have closed on us.
                        if (!otherTile.AdjacentBits.IsFlagSet(direction)) continue;
                        otherTile2.MonstermosInfo = new MonstermosInfo { LastQueueCycle = queueCycle };
                        _depressurizeTiles[tileCount++] = otherTile2;
                        if (tileCount >= limit) break;
                    }
                }
                else
                {
                    _depressurizeSpaceTiles[spaceTileCount++] = otherTile;
                    otherTile.PressureSpecificTarget = otherTile;
                }

                if (tileCount < limit && spaceTileCount < limit)
                    continue;

                break;
            }

            var queueCycleSlow = ++gridAtmosphere.EqualizationQueueCycleControl;
            var progressionCount = 0;

            for (var i = 0; i < spaceTileCount; i++)
            {
                var otherTile = _depressurizeSpaceTiles[i];
                _depressurizeProgressionOrder[progressionCount++] = otherTile;
                otherTile.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                otherTile.MonstermosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
            }

            for (var i = 0; i < progressionCount; i++)
            {
                var otherTile = _depressurizeProgressionOrder[i];
                for (var j = 0; j < Atmospherics.Directions; j++)
                {
                    var direction = (AtmosDirection) (1 << j);
                    // Tiles in _depressurizeProgressionOrder cannot have null air.
                    if (!otherTile.AdjacentBits.IsFlagSet(direction) && !otherTile.Space) continue;
                    var tile2 = otherTile.AdjacentTiles[j];
                    if (tile2?.MonstermosInfo.LastQueueCycle != queueCycle) continue;
                    DebugTools.Assert(tile2.AdjacentBits.IsFlagSet(direction.GetOpposite()));
                    if (tile2.MonstermosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                    if(tile2.Space) continue;
                    tile2.MonstermosInfo.CurrentTransferDirection = direction.GetOpposite();
                    tile2.MonstermosInfo.CurrentTransferAmount = 0;
                    tile2.PressureSpecificTarget = otherTile.PressureSpecificTarget;
                    tile2.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                    _depressurizeProgressionOrder[progressionCount++] = tile2;
                }
            }

            for (var i = progressionCount - 1; i >= 0; i--)
            {
                var otherTile = _depressurizeProgressionOrder[i];
                if (otherTile.MonstermosInfo.CurrentTransferDirection == AtmosDirection.Invalid) continue;
                gridAtmosphere.HighPressureDelta.Add(otherTile);
                AddActiveTile(gridAtmosphere, otherTile);
                var otherTile2 = otherTile.AdjacentTiles[otherTile.MonstermosInfo.CurrentTransferDirection.ToIndex()];
                if (otherTile2?.Air == null) continue;
                var sum = otherTile2.Air.TotalMoles;
                totalMolesRemoved += sum;
                otherTile.MonstermosInfo.CurrentTransferAmount += sum;
                otherTile2.MonstermosInfo.CurrentTransferAmount += otherTile.MonstermosInfo.CurrentTransferAmount;
                otherTile.PressureDifference = otherTile.MonstermosInfo.CurrentTransferAmount;
                otherTile.PressureDirection = otherTile.MonstermosInfo.CurrentTransferDirection;

                if (otherTile2.MonstermosInfo.CurrentTransferDirection == AtmosDirection.Invalid)
                {
                    otherTile2.PressureDifference = otherTile2.MonstermosInfo.CurrentTransferAmount;
                    otherTile2.PressureDirection = otherTile.MonstermosInfo.CurrentTransferDirection;
                }


                // This gas mixture cannot be null, no tile in _depressurizeProgressionOrder can have a null gas mixture
                otherTile.Air!.Clear();

                // This is a little hacky, but hear me out. It makes sense. We have just vacuumed all of the tile's air
                // therefore there is no more gas in the tile, therefore the tile should be as cold as space!
                otherTile.Air.Temperature = Atmospherics.TCMB;

                InvalidateVisuals(otherTile.GridIndex, otherTile.GridIndices, visuals);
                HandleDecompressionFloorRip(mapGrid, otherTile, sum);
            }

            if (GridImpulse && tileCount > 0)
            {
                var direction = ((Vector2)_depressurizeTiles[tileCount - 1].GridIndices - tile.GridIndices).Normalized;

                var gridPhysics = Comp<PhysicsComponent>(mapGrid.Owner);

                // TODO ATMOS: Come up with better values for these.
                _physics.ApplyLinearImpulse(mapGrid.Owner, direction * totalMolesRemoved * gridPhysics.Mass, body: gridPhysics);
                _physics.ApplyAngularImpulse(mapGrid.Owner, Vector2.Cross(tile.GridIndices - gridPhysics.LocalCenter, direction) * totalMolesRemoved, body: gridPhysics);
            }

            if(tileCount > 10 && (totalMolesRemoved / tileCount) > 20)
                _adminLog.Add(LogType.ExplosiveDepressurization, LogImpact.High,
                    $"Explosive depressurization removed {totalMolesRemoved} moles from {tileCount} tiles starting from position {tile.GridIndices:position} on grid ID {tile.GridIndex:grid}");

            Array.Clear(_depressurizeTiles, 0, Atmospherics.MonstermosHardTileLimit);
            Array.Clear(_depressurizeSpaceTiles, 0, Atmospherics.MonstermosHardTileLimit);
            Array.Clear(_depressurizeProgressionOrder, 0, Atmospherics.MonstermosHardTileLimit * 2);
        }

        private void ConsiderFirelocks(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, TileAtmosphere other, GasTileOverlayComponent? visuals, MapGridComponent mapGrid)
        {
            var reconsiderAdjacent = false;

            foreach (var entity in mapGrid.GetAnchoredEntities(tile.GridIndices))
            {
                if (!TryComp(entity, out FirelockComponent? firelock))
                    continue;

                reconsiderAdjacent |= _firelockSystem.EmergencyPressureStop(entity, firelock);
            }

            foreach (var entity in mapGrid.GetAnchoredEntities(other.GridIndices))
            {
                if (!TryComp(entity, out FirelockComponent? firelock))
                    continue;

                reconsiderAdjacent |= _firelockSystem.EmergencyPressureStop(entity, firelock);
            }

            if (!reconsiderAdjacent)
                return;

            var tileEv = new UpdateAdjacentMethodEvent(mapGrid.Owner, tile.GridIndices);
            var otherEv = new UpdateAdjacentMethodEvent(mapGrid.Owner, other.GridIndices);
            GridUpdateAdjacent(mapGrid.Owner, gridAtmosphere, ref tileEv);
            GridUpdateAdjacent(mapGrid.Owner, gridAtmosphere, ref otherEv);
            InvalidateVisuals(tile.GridIndex, tile.GridIndices, visuals);
            InvalidateVisuals(other.GridIndex, other.GridIndices, visuals);
        }

        private void FinalizeEq(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, GasTileOverlayComponent? visuals)
        {
            Span<float> transferDirections = stackalloc float[Atmospherics.Directions];
            var hasTransferDirs = false;
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var amount = tile.MonstermosInfo[i];
                if (amount == 0) continue;
                transferDirections[i] = amount;
                tile.MonstermosInfo[i] = 0; // Set them to 0 to prevent infinite recursion.
                hasTransferDirs = true;
            }

            if (!hasTransferDirs) return;

            for(var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (!tile.AdjacentBits.IsFlagSet(direction)) continue;
                var amount = transferDirections[i];
                var otherTile = tile.AdjacentTiles[i];
                if (otherTile?.Air == null) continue;
                DebugTools.Assert(otherTile.AdjacentBits.IsFlagSet(direction.GetOpposite()));
                if (amount <= 0) continue;

                // Everything that calls this method already ensures that Air will not be null.
                if (tile.Air!.TotalMoles < amount)
                    FinalizeEqNeighbors(gridAtmosphere, tile, transferDirections, visuals);

                otherTile.MonstermosInfo[direction.GetOpposite()] = 0;
                Merge(otherTile.Air, tile.Air.Remove(amount));
                InvalidateVisuals(tile.GridIndex, tile.GridIndices, visuals);
                InvalidateVisuals(otherTile.GridIndex, otherTile.GridIndices, visuals);
                ConsiderPressureDifference(gridAtmosphere, tile, direction, amount);
            }
        }

        private void FinalizeEqNeighbors(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, ReadOnlySpan<float> transferDirs, GasTileOverlayComponent? visuals)
        {
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                var amount = transferDirs[i];
                // Since AdjacentBits is set, AdjacentTiles[i] wouldn't be null, and neither would its air.
                if(amount < 0 && tile.AdjacentBits.IsFlagSet(direction))
                    FinalizeEq(gridAtmosphere, tile.AdjacentTiles[i]!, visuals);  // A bit of recursion if needed.
            }
        }

        private void AdjustEqMovement(TileAtmosphere tile, AtmosDirection direction, float amount)
        {
            DebugTools.AssertNotNull(tile);
            DebugTools.Assert(tile.AdjacentBits.IsFlagSet(direction));
            DebugTools.Assert(tile.AdjacentTiles[direction.ToIndex()] != null);
            // Every call to this method already ensures that the adjacent tile won't be null.

            // Turns out: no they don't. Temporary debug checks to figure out which caller is causing problems:
            if (tile == null)
            {
                Logger.Error($"Encountered null-tile in {nameof(AdjustEqMovement)}. Trace: {Environment.StackTrace}");
                return;
            }
            var adj = tile.AdjacentTiles[direction.ToIndex()];
            if (adj == null)
            {
                var nonNull = tile.AdjacentTiles.Where(x => x != null).Count();
                Logger.Error($"Encountered null adjacent tile in {nameof(AdjustEqMovement)}. Dir: {direction}, Tile: {tile.Tile}, non-null adj count: {nonNull}, Trace: {Environment.StackTrace}");
                return;
            }

            tile.MonstermosInfo[direction] += amount;
            adj.MonstermosInfo[direction.GetOpposite()] -= amount;
        }

        private void HandleDecompressionFloorRip(MapGridComponent mapGrid, TileAtmosphere tile, float sum)
        {
            if (!MonstermosRipTiles)
                return;

            var chance = MathHelper.Clamp(sum / 500, 0.005f, 0.5f);

            if (sum > 20 && _robustRandom.Prob(chance))
                PryTile(mapGrid, tile.GridIndices);
        }

        private sealed class TileAtmosphereComparer : IComparer<TileAtmosphere?>
        {
            public int Compare(TileAtmosphere? a, TileAtmosphere? b)
            {
                if (a == null && b == null)
                    return 0;

                if (a == null)
                    return -1;

                if (b == null)
                    return 1;

                return a.MonstermosInfo.MoleDelta.CompareTo(b.MonstermosInfo.MoleDelta);
            }
        }
    }
}
