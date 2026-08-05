using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Server.Doors.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Database;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

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

        private void EqualizePressureInZone(
            Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
            TileAtmosphere tile,
            int cycleNum)
        {
            if (tile.Air == null || (tile.MonstermosInfo.LastCycle >= cycleNum))
                return; // Already done.

            tile.MonstermosInfo = new MonstermosInfo();

            var startingMoles = tile.Air.TotalMoles;
            var runAtmos = false;

            // We need to figure if this is necessary
            var adjacentBits = (int) tile.AdjacentBits; // DS14
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                if ((adjacentBits & (1 << i)) == 0) continue; // DS14
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

            var gridAtmosphere = ent.Comp1;
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

                adjacentBits = (int) exploring.AdjacentBits; // DS14
                for (var j = 0; j < Atmospherics.Directions; j++)
                {
                    var directionBit = 1 << j; // DS14
                    if ((adjacentBits & directionBit) == 0) continue; // DS14
                    var adj = exploring.AdjacentTiles[j];
                    if (adj?.Air == null) continue;
                    if(adj.MonstermosInfo.LastQueueCycle == queueCycle) continue;
                    adj.MonstermosInfo = new MonstermosInfo {LastQueueCycle = queueCycle};

                    if(tileCount < Atmospherics.MonstermosHardTileLimit)
                        _equalizeTiles[tileCount++] = adj;

                    if (adj.Space && MonstermosDepressurization)
                    {
                        // Looks like someone opened an airlock to space!

                        ExplosivelyDepressurize(ent, tile, cycleNum);
                        return;
                    }
                }
            }

            var equalizeTileCleanupLength = tileCount; // DS14

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

            // DS14-start: clear only Monstermos buffer slots touched by this run.
            var giverTilesCleanupLength = giverTilesLength;
            var takerTilesCleanupLength = takerTilesLength;
            var equalizeQueueCleanupLength = 0;
            // DS14-end

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
                    var eligibleDirectionBits = 0; // DS14
                    var eligibleDirectionCount = 0;
                    adjacentBits = (int) otherTile.AdjacentBits; // DS14
                    for (var j = 0; j < Atmospherics.Directions; j++)
                    {
                        var directionBit = 1 << j; // DS14
                        if ((adjacentBits & directionBit) == 0) continue; // DS14
                        var tile2 = otherTile.AdjacentTiles[j]!;
                        DebugTools.Assert(tile2.AdjacentBits.IsFlagSet((AtmosDirection) (1 << (j ^ 1)))); // DS14

                        // skip anything that isn't part of our current processing block.
                        if (tile2.MonstermosInfo.FastDone || tile2.MonstermosInfo.LastQueueCycle != queueCycle)
                            continue;

                        eligibleDirectionBits |= directionBit; // DS14
                        eligibleDirectionCount++;
                    }

                    if (eligibleDirectionCount <= 0)
                        continue; // Oof we've painted ourselves into a corner. Bad luck. Next part will handle this.

                    var molesToMove = otherTile.MonstermosInfo.MoleDelta / eligibleDirectionCount;
                    for (var j = 0; j < Atmospherics.Directions; j++)
                    {
                        if ((eligibleDirectionBits & (1 << j)) == 0) continue; // DS14

                        AdjustEqMovement(otherTile, j, molesToMove);
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

                // DS14-start
                giverTilesCleanupLength = Math.Max(giverTilesCleanupLength, giverTilesLength);
                takerTilesCleanupLength = Math.Max(takerTilesCleanupLength, takerTilesLength);
                // DS14-end
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
                        adjacentBits = (int) otherTile.AdjacentBits; // DS14
                        for (var k = 0; k < Atmospherics.Directions; k++)
                        {
                            var directionBit = 1 << k; // DS14
                            if ((adjacentBits & directionBit) == 0) // DS14
                                continue;

                            if (giver.MonstermosInfo.MoleDelta <= 0)
                                break; // We're done here now. Let's not do more work than needed.

                            var otherTile2 = otherTile.AdjacentTiles[k];
                            if (otherTile2 == null || otherTile2.MonstermosInfo.LastQueueCycle != queueCycle) continue;
                            DebugTools.Assert(otherTile2.AdjacentBits.IsFlagSet((AtmosDirection) (1 << (k ^ 1)))); // DS14
                            if (otherTile2.MonstermosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                            _equalizeQueue[queueLength++] = otherTile2;
                            otherTile2.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                            otherTile2.MonstermosInfo.CurrentTransferDirection = (AtmosDirection) (1 << (k ^ 1)); // DS14
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

                    equalizeQueueCleanupLength = Math.Max(equalizeQueueCleanupLength, queueLength); // DS14

                    // Putting this loop here helps make it O(n^2) over O(n^3)
                    for (var i = queueLength - 1; i >= 0; i--)
                    {
                        var otherTile = _equalizeQueue[i];
                        if (otherTile.MonstermosInfo.CurrentTransferAmount != 0 && otherTile.MonstermosInfo.CurrentTransferDirection != AtmosDirection.Invalid)
                        {
                            var transferDirectionIndex = otherTile.MonstermosInfo.CurrentTransferDirection.ToIndex();
                            var transferAmount = otherTile.MonstermosInfo.CurrentTransferAmount;
                            AdjustEqMovement(otherTile, transferDirectionIndex, transferAmount);
                            otherTile.AdjacentTiles[transferDirectionIndex]!
                                .MonstermosInfo.CurrentTransferAmount += transferAmount;
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
                        adjacentBits = (int) otherTile.AdjacentBits; // DS14
                        for (var k = 0; k < Atmospherics.Directions; k++)
                        {
                            var directionBit = 1 << k; // DS14
                            if ((adjacentBits & directionBit) == 0) continue; // DS14
                            var otherTile2 = otherTile.AdjacentTiles[k];

                            if (taker.MonstermosInfo.MoleDelta >= 0) break; // We're done here now. Let's not do more work than needed.
                            if (otherTile2 == null || otherTile2.AdjacentBits == 0 || otherTile2.MonstermosInfo.LastQueueCycle != queueCycle) continue;
                            DebugTools.Assert(otherTile2.AdjacentBits.IsFlagSet((AtmosDirection) (1 << (k ^ 1)))); // DS14
                            if (otherTile2.MonstermosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                            _equalizeQueue[queueLength++] = otherTile2;
                            otherTile2.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                            otherTile2.MonstermosInfo.CurrentTransferDirection = (AtmosDirection) (1 << (k ^ 1)); // DS14
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

                    equalizeQueueCleanupLength = Math.Max(equalizeQueueCleanupLength, queueLength); // DS14

                    for (var i = queueLength - 1; i >= 0; i--)
                    {
                        var otherTile = _equalizeQueue[i];
                        if (otherTile.MonstermosInfo.CurrentTransferAmount == 0 || otherTile.MonstermosInfo.CurrentTransferDirection == AtmosDirection.Invalid)
                            continue;

                        var transferDirectionIndex = otherTile.MonstermosInfo.CurrentTransferDirection.ToIndex();
                        var transferAmount = otherTile.MonstermosInfo.CurrentTransferAmount;
                        AdjustEqMovement(otherTile, transferDirectionIndex, transferAmount);

                        otherTile.AdjacentTiles[transferDirectionIndex]!
                            .MonstermosInfo.CurrentTransferAmount += transferAmount;
                        otherTile.MonstermosInfo.CurrentTransferAmount = 0;
                    }
                }
            }

            for (var i = 0; i < tileCount; i++)
            {
                var otherTile = _equalizeTiles[i]!;
                FinalizeEq(ent, otherTile);
            }

            for (var i = 0; i < tileCount; i++)
            {
                var otherTile = _equalizeTiles[i]!;
                adjacentBits = (int) otherTile.AdjacentBits; // DS14
                for (var j = 0; j < Atmospherics.Directions; j++)
                {
                    var directionBit = 1 << j; // DS14
                    if ((adjacentBits & directionBit) == 0) // DS14
                        continue;

                    var otherTile2 = otherTile.AdjacentTiles[j]!;
                    if (otherTile2.AdjacentBits == 0)
                        continue;

                    DebugTools.Assert(otherTile2.AdjacentBits.IsFlagSet((AtmosDirection) (1 << (j ^ 1)))); // DS14
                    if (otherTile2.Air != null && CompareExchange(otherTile2, tile) == GasCompareResult.NoExchange)
                        continue;

                    AddActiveTile(gridAtmosphere, otherTile2);
                    break;
                }
            }

            // We do cleanup.
            // DS14-start: these arrays are fully overwritten up to their active lengths next run.
            Array.Clear(_equalizeTiles, 0, equalizeTileCleanupLength);
            Array.Clear(_equalizeGiverTiles, 0, giverTilesCleanupLength);
            Array.Clear(_equalizeTakerTiles, 0, takerTilesCleanupLength);
            Array.Clear(_equalizeQueue, 0, equalizeQueueCleanupLength);
            // DS14-end
        }

        private void ExplosivelyDepressurize(
            Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
            TileAtmosphere tile,
            int cycleNum)
        {
            // Check if explosive depressurization is enabled and if the tile is valid.
            if (!MonstermosDepressurization || tile.Air == null)
                return;

            const int limit = Atmospherics.MonstermosHardTileLimit;

            var totalMolesRemoved = 0f;
            var (owner, gridAtmosphere, visuals, mapGrid, _) = ent;
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
                        var otherTile2 = otherTile.AdjacentTiles[j];
                        if (otherTile2?.Air == null)
                            continue;

                        if (otherTile2.MonstermosInfo.LastQueueCycle == queueCycle)
                            continue;

                        var direction = (AtmosDirection) (1 << j);
                        DebugTools.Assert(otherTile.AdjacentBits.IsFlagSet(direction));
                        DebugTools.Assert(otherTile2.AdjacentBits.IsFlagSet((AtmosDirection) (1 << (j ^ 1)))); // DS14

                        ConsiderFirelocks(ent, otherTile, otherTile2);

                        // The firelocks might have closed on us.
                        if (!otherTile.AdjacentBits.IsFlagSet(direction))
                            continue;

                        otherTile2.MonstermosInfo = new MonstermosInfo { LastQueueCycle = queueCycle };
                        _depressurizeTiles[tileCount++] = otherTile2;
                        if (tileCount >= limit)
                            break;
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

            // Moving into the room from the breach or airlock
            for (var i = 0; i < progressionCount; i++)
            {
                // From a tile exposed to space
                var otherTile = _depressurizeProgressionOrder[i];
                for (var j = 0; j < Atmospherics.Directions; j++)
                {
                    // Flood fill into this new direction
                    var direction = (AtmosDirection) (1 << j);
                    // Tiles in _depressurizeProgressionOrder cannot have null air.
                    if (!otherTile.AdjacentBits.IsFlagSet(direction) && !otherTile.Space)
                        continue;

                    var tile2 = otherTile.AdjacentTiles[j];
                    if (tile2?.MonstermosInfo.LastQueueCycle != queueCycle)
                        continue;

                    DebugTools.Assert(tile2.AdjacentBits.IsFlagSet((AtmosDirection) (1 << (j ^ 1)))); // DS14
                    // If flood fill has already reached this tile, continue.
                    if (tile2.MonstermosInfo.LastSlowQueueCycle == queueCycleSlow)
                        continue;

                    if(tile2.Space)
                        continue;

                    tile2.MonstermosInfo.CurrentTransferDirection = j.ToOppositeDir();
                    tile2.MonstermosInfo.CurrentTransferAmount = 0.0f;
                    tile2.PressureSpecificTarget = otherTile.PressureSpecificTarget;
                    tile2.MonstermosInfo.LastSlowQueueCycle = queueCycleSlow;
                    _depressurizeProgressionOrder[progressionCount++] = tile2;
                }
            }

            // Moving towards the breach from the edges of the flood filled region
            for (var i = progressionCount - 1; i >= 0; i--)
            {
                var otherTile = _depressurizeProgressionOrder[i];
                if (otherTile?.Air == null) { continue;}
                if (otherTile.MonstermosInfo.CurrentTransferDirection == AtmosDirection.Invalid) continue;
                gridAtmosphere.HighPressureDelta.Add(otherTile);
                AddActiveTile(gridAtmosphere, otherTile);
                var transferDirectionIndex = otherTile.MonstermosInfo.CurrentTransferDirection.ToIndex();
                var otherTile2 = otherTile.AdjacentTiles[transferDirectionIndex];
                if (otherTile2?.Air == null)
                {
                    // The tile connecting us to space is spaced already. So just space this tile now.
                    otherTile.Air!.Clear();
                    otherTile.Air.Temperature = Atmospherics.TCMB;
                    continue;
                }
                var sum = otherTile.Air.TotalMoles;
                if (SpacingEscapeRatio < 1f)
                {
                    sum *= SpacingEscapeRatio;
                    if (sum < SpacingMinGas)
                    {
                        // Boost the last bit of air draining from the tile.
                        sum = Math.Min(SpacingMinGas, otherTile.Air.TotalMoles);
                    }
                    if (sum + otherTile.MonstermosInfo.CurrentTransferAmount > SpacingMaxWind)
                    {
                        // Limit the flow of air out of tiles which have air flowing into them from elsewhere.
                        sum = Math.Max(SpacingMinGas, SpacingMaxWind - otherTile.MonstermosInfo.CurrentTransferAmount);
                    }
                }
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

                if (otherTile.Air != null && otherTile.Air.Pressure - sum > SpacingMinGas * 0.1f)
                {
                    // Transfer the air into the other tile (space wind :)
                    ReleaseGasTo(otherTile.Air!, otherTile2.Air!, sum);
                    // And then some magically into space
                    ReleaseGasTo(otherTile2.Air!, null, sum * 0.3f);

                    if (otherTile.Air.Temperature > 280.0f)
                    {
                        // Temperature reduces as air drains. But nerf the real temperature reduction a bit
                        //   Also, limit the temperature loss to remain > 10 Deg.C for convenience
                        float realtemploss = (otherTile.Air.TotalMoles - sum) / otherTile.Air.TotalMoles;
                        otherTile.Air.Temperature *= 0.9f + 0.1f * realtemploss;
                    }
                }
                else
                {
                    // This gas mixture cannot be null, no tile in _depressurizeProgressionOrder can have a null gas mixture
                    otherTile.Air!.Clear();

                    // This is a little hacky, but hear me out. It makes sense. We have just vacuumed all of the tile's air
                    // therefore there is no more gas in the tile, therefore the tile should be as cold as space!
                    otherTile.Air.Temperature = Atmospherics.TCMB;
                }

                InvalidateVisuals(ent, otherTile);
                HandleDecompressionFloorRip((owner, mapGrid), otherTile, otherTile.MonstermosInfo.CurrentTransferAmount);
            }

            if (GridImpulse && tileCount > 0)
            {
                var direction = ((Vector2)_depressurizeTiles[tileCount - 1].GridIndices - tile.GridIndices).Normalized();

                var gridPhysics = Comp<PhysicsComponent>(owner);

                // TODO ATMOS: Come up with better values for these.
                _physics.ApplyLinearImpulse(owner, direction * totalMolesRemoved * gridPhysics.Mass, body: gridPhysics);
                _physics.ApplyAngularImpulse(owner, Vector2Helpers.Cross(tile.GridIndices - gridPhysics.LocalCenter, direction) * totalMolesRemoved, body: gridPhysics);
            }

            if (tileCount > 10 && (totalMolesRemoved / tileCount) > 10)
                _adminLog.Add(LogType.ExplosiveDepressurization, LogImpact.High,
                    $"Explosive depressurization removed {totalMolesRemoved} moles from {tileCount} tiles starting from position {tile.GridIndices:position} on grid ID {tile.GridIndex:grid}");

            // DS14-start: clear only slots written by this depressurization flood fill.
            Array.Clear(_depressurizeTiles, 0, tileCount);
            Array.Clear(_depressurizeSpaceTiles, 0, spaceTileCount);
            Array.Clear(_depressurizeProgressionOrder, 0, progressionCount);
            // DS14-end
        }

        private void ConsiderFirelocks(
            Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
            TileAtmosphere tile,
            TileAtmosphere other)
        {
            var reconsiderAdjacent = false;

            var mapGrid = ent.Comp3;
            foreach (var entity in _map.GetAnchoredEntities(ent.Owner, mapGrid, tile.GridIndices))
            {
                if (_firelockQuery.TryGetComponent(entity, out var firelock) && firelock.Powered)
                    reconsiderAdjacent |= _firelockSystem.EmergencyPressureStop(entity, firelock);
            }

            foreach (var entity in _map.GetAnchoredEntities(ent.Owner, mapGrid, other.GridIndices))
            {
                if (_firelockQuery.TryGetComponent(entity, out var firelock) && firelock.Powered)
                    reconsiderAdjacent |= _firelockSystem.EmergencyPressureStop(entity, firelock);
            }

            if (!reconsiderAdjacent)
                return;

            // Before updating the adjacent tile flags that determine whether air is allowed to flow
            // or not, we explicitly update airtight data on these tiles right now.
            // This ensures that UpdateAdjacentTiles has updated data before updating flags.
            // This allows monstermos' floodfill check that determines if firelocks have dropped
            // to work correctly.
            UpdateAirtightData(ent.Owner, ent.Comp1, ent.Comp3, tile);
            UpdateAirtightData(ent.Owner, ent.Comp1, ent.Comp3, other);

            UpdateAdjacentTiles(ent, tile);
            UpdateAdjacentTiles(ent, other);

            InvalidateVisuals(ent, tile);
            InvalidateVisuals(ent, other);
        }

        private void FinalizeEq(
            Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
            TileAtmosphere tile)
        {
            // DS14-start: avoid MonstermosInfo indexer switches in this hot finalization loop.
            Span<float> transferDirections = stackalloc float[Atmospherics.Directions]
            {
                tile.MonstermosInfo.TransferDirectionNorth,
                tile.MonstermosInfo.TransferDirectionSouth,
                tile.MonstermosInfo.TransferDirectionEast,
                tile.MonstermosInfo.TransferDirectionWest,
            };

            if (transferDirections[0] == 0 &&
                transferDirections[1] == 0 &&
                transferDirections[2] == 0 &&
                transferDirections[3] == 0)
            {
                return;
            }

            // Set them to 0 before recursing to prevent infinite recursion.
            tile.MonstermosInfo.TransferDirectionNorth = 0;
            tile.MonstermosInfo.TransferDirectionSouth = 0;
            tile.MonstermosInfo.TransferDirectionEast = 0;
            tile.MonstermosInfo.TransferDirectionWest = 0;
            // DS14-end

            var adjacentBits = (int) tile.AdjacentBits; // DS14
            for(var i = 0; i < Atmospherics.Directions; i++)
            {
                var directionBit = 1 << i; // DS14
                if ((adjacentBits & directionBit) == 0) continue; // DS14
                var direction = (AtmosDirection) directionBit; // DS14
                var amount = transferDirections[i];
                var otherTile = tile.AdjacentTiles[i];
                if (otherTile?.Air == null) continue;
                DebugTools.Assert(otherTile.AdjacentBits.IsFlagSet((AtmosDirection) (1 << (i ^ 1)))); // DS14
                if (amount <= 0) continue;

                // Everything that calls this method already ensures that Air will not be null.
                if (tile.Air!.TotalMoles < amount)
                    FinalizeEqNeighbors(ent, tile, transferDirections);

                SetTransferDirection(ref otherTile.MonstermosInfo, i ^ 1, 0); // DS14
                TransferGas(otherTile.Air, tile.Air, amount); // DS14
                InvalidateVisuals(ent, tile);
                InvalidateVisuals(ent, otherTile);
                ConsiderPressureDifference(ent, tile, direction, amount);
            }
        }

        private void FinalizeEqNeighbors(
            Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
            TileAtmosphere tile, ReadOnlySpan<float> transferDirs)
        {
            var adjacentBits = (int) tile.AdjacentBits; // DS14
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var amount = transferDirs[i];
                // Since AdjacentBits is set, AdjacentTiles[i] wouldn't be null, and neither would its air.
                if(amount < 0 && (adjacentBits & (1 << i)) != 0) // DS14
                    FinalizeEq(ent, tile.AdjacentTiles[i]!);  // A bit of recursion if needed.
            }
        }

        private void AdjustEqMovement(TileAtmosphere tile, int directionIndex, float amount)
        {
            // DS14-start: callers usually already have the direction index; avoid Log2 and indexer switches.
            var direction = (AtmosDirection) (1 << directionIndex);
            DebugTools.AssertNotNull(tile);
            DebugTools.Assert(tile.AdjacentBits.IsFlagSet(direction));
            DebugTools.Assert(tile.AdjacentTiles[directionIndex] != null);
            // Every call to this method already ensures that the adjacent tile won't be null.

            // Turns out: no they don't. Temporary debug checks to figure out which caller is causing problems:
            if (tile == null)
            {
                Log.Error($"Encountered null-tile in {nameof(AdjustEqMovement)}. Trace: {Environment.StackTrace}");
                return;
            }

            var adj = tile.AdjacentTiles[directionIndex];
            if (adj == null)
            {
                var nonNull = 0;
                for (var i = 0; i < tile.AdjacentTiles.Length; i++)
                {
                    if (tile.AdjacentTiles[i] != null)
                        nonNull++;
                }

                Log.Error($"Encountered null adjacent tile in {nameof(AdjustEqMovement)}. Dir: {direction}, Tile: ({tile.GridIndex}, {tile.GridIndices}), non-null adj count: {nonNull}, Trace: {Environment.StackTrace}");
                return;
            }

            switch (directionIndex)
            {
                case 0:
                    tile.MonstermosInfo.TransferDirectionNorth += amount;
                    adj.MonstermosInfo.TransferDirectionSouth -= amount;
                    break;
                case 1:
                    tile.MonstermosInfo.TransferDirectionSouth += amount;
                    adj.MonstermosInfo.TransferDirectionNorth -= amount;
                    break;
                case 2:
                    tile.MonstermosInfo.TransferDirectionEast += amount;
                    adj.MonstermosInfo.TransferDirectionWest -= amount;
                    break;
                case 3:
                    tile.MonstermosInfo.TransferDirectionWest += amount;
                    adj.MonstermosInfo.TransferDirectionEast -= amount;
                    break;
            }
            // DS14-end
        }

        private static void SetTransferDirection(ref MonstermosInfo info, int directionIndex, float value)
        {
            switch (directionIndex)
            {
                case 0:
                    info.TransferDirectionNorth = value;
                    break;
                case 1:
                    info.TransferDirectionSouth = value;
                    break;
                case 2:
                    info.TransferDirectionEast = value;
                    break;
                case 3:
                    info.TransferDirectionWest = value;
                    break;
            }
        }

        // DS14-start: Monstermos finalization transfers gas very often; avoid per-transfer GasMixture allocation.
        private void TransferGas(GasMixture receiver, GasMixture giver, float amount)
        {
            var ratio = amount / giver.TotalMoles;
            switch (ratio)
            {
                case <= 0:
                    return;
                case > 1:
                    ratio = 1;
                    break;
            }

            Span<float> removedMoles = stackalloc float[Atmospherics.AdjustedNumberOfGases];
            var giverMoles = giver.Moles;
            var receiverMoles = receiver.Moles;
            var mixTemperatures = MathF.Abs(receiver.Temperature - giver.Temperature) >
                                  Atmospherics.MinimumTemperatureDeltaToConsider;
            var removedHeatCapacity = 0f;
            var receiverHeatCapacity = 0f;

            for (var i = 0; i < giverMoles.Length; i++)
            {
                var removed = giverMoles[i] * ratio;
                var gasHeatCapacity = GasSpecificHeats[i];

                if (!giver.Immutable)
                {
                    var remaining = giverMoles[i] - removed;
                    giverMoles[i] = remaining < Atmospherics.GasMinMoles || float.IsNaN(remaining)
                        ? 0
                        : remaining;
                }

                if (removed < Atmospherics.GasMinMoles || float.IsNaN(removed))
                    removed = 0;

                removedMoles[i] = removed;
                removedHeatCapacity += removed * gasHeatCapacity;
                if (mixTemperatures)
                    receiverHeatCapacity += receiverMoles[i] * gasHeatCapacity;
            }

            if (receiver.Immutable)
                return;

            if (mixTemperatures)
            {
                receiverHeatCapacity = MathF.Max(receiverHeatCapacity, Atmospherics.MinimumHeatCapacity);
                removedHeatCapacity = MathF.Max(removedHeatCapacity, Atmospherics.MinimumHeatCapacity);
                var combinedHeatCapacity = receiverHeatCapacity + removedHeatCapacity;
                if (combinedHeatCapacity > Atmospherics.MinimumHeatCapacity)
                {
                    receiver.Temperature =
                        (GetThermalEnergy(receiver, receiverHeatCapacity) + giver.Temperature * removedHeatCapacity) /
                        combinedHeatCapacity;
                }
            }

            for (var i = 0; i < receiverMoles.Length; i++)
            {
                receiverMoles[i] += removedMoles[i];
            }
        }
        // DS14-end

        private void HandleDecompressionFloorRip(Entity<MapGridComponent> mapGrid, TileAtmosphere tile, float sum)
        {
            if (!MonstermosRipTiles)
                return;

            var chance = MathHelper.Clamp(0.01f + (sum / SpacingMaxWind) * 0.3f, 0.003f, 0.3f);

            if (sum > 20 && _random.Prob(chance))
                PryTile(mapGrid, tile.GridIndices);
        }

        private sealed class TileAtmosphereComparer : IComparer<TileAtmosphere?>
        {
            public int Compare(TileAtmosphere? a, TileAtmosphere? b)
            {
                return a!.MonstermosInfo.MoleDelta.CompareTo(b!.MonstermosInfo.MoleDelta); // DS14
            }
        }
    }
}
