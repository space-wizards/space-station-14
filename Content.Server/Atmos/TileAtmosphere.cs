#nullable disable warnings
#nullable enable annotations
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Reactions;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.Interfaces;
using Content.Server.Utility;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos
{
    public class TileAtmosphere : IGasMixtureHolder
    {
        [Robust.Shared.IoC.Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Robust.Shared.IoC.Dependency] private readonly IEntityManager _entityManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;
        private readonly GridTileLookupSystem _gridTileLookupSystem = default!;


        private static readonly TileAtmosphereComparer Comparer = new();

        [ViewVariables] private int _archivedCycle;
        [ViewVariables] private int _currentCycle;

        [ViewVariables]
        public float Temperature { get; private set; } = Atmospherics.T20C;

        [ViewVariables]
        private float _temperatureArchived = Atmospherics.T20C;

        // I know this being static is evil, but I seriously can't come up with a better solution to sound spam.
        private static int _soundCooldown;

        [ViewVariables]
        public TileAtmosphere? PressureSpecificTarget { get; set; }

        [ViewVariables]
        public float PressureDifference { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float HeatCapacity { get; set; } = 1f;

        [ViewVariables]
        public float ThermalConductivity { get; set; } = 0.05f;

        [ViewVariables]
        public bool Excited { get; set; }

        [ViewVariables]
        private readonly GridAtmosphereComponent _gridAtmosphereComponent;

        /// <summary>
        ///     Adjacent tiles in the same order as <see cref="AtmosDirection"/>. (NSEW)
        /// </summary>
        [ViewVariables]
        private readonly TileAtmosphere[] _adjacentTiles = new TileAtmosphere[Atmospherics.Directions];

        private AtmosDirection _adjacentBits = AtmosDirection.Invalid;

        [ViewVariables, UsedImplicitly]
        private int AdjacentBitsInt => (int)_adjacentBits;

        [ViewVariables]
        private TileAtmosInfo _tileAtmosInfo;

        [ViewVariables]
        public Hotspot Hotspot;

        private AtmosDirection _pressureDirection;

        // I'm assuming there's a good reason the original variable was made private, but this information is also important.
        public AtmosDirection PressureDirectionForDebugOverlay => _pressureDirection;

        [ViewVariables, UsedImplicitly]
        private int PressureDirectionInt => (int)_pressureDirection;

        [ViewVariables]
        public GridId GridIndex { get; }

        [ViewVariables]
        public TileRef? Tile => GridIndices.GetTileRef(GridIndex);

        [ViewVariables]
        public Vector2i GridIndices { get; }

        [ViewVariables]
        public ExcitedGroup? ExcitedGroup { get; set; }

        /// <summary>
        /// The air in this tile. If null, this tile is completely airblocked.
        /// This can be immutable if the tile is spaced.
        /// </summary>
        [ViewVariables]
        public GasMixture? Air { get; set; }

        [ViewVariables, UsedImplicitly]
        private int _blockedAirflow => (int)BlockedAirflow;

        public AtmosDirection BlockedAirflow { get; set; } = AtmosDirection.Invalid;

        [ViewVariables]
        public bool BlocksAllAir => BlockedAirflow == AtmosDirection.All;

        public TileAtmosphere(GridAtmosphereComponent atmosphereComponent, GridId gridIndex, Vector2i gridIndices, GasMixture? mixture = null, bool immutable = false)
        {
            IoCManager.InjectDependencies(this);
            _gridAtmosphereComponent = atmosphereComponent;
            _gridTileLookupSystem = _entityManager.EntitySysManager.GetEntitySystem<GridTileLookupSystem>();
            GridIndex = gridIndex;
            GridIndices = gridIndices;
            Air = mixture;

            if(immutable)
                Air?.MarkImmutable();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Archive(int fireCount)
        {
            Air?.Archive();
            _archivedCycle = fireCount;
            _temperatureArchived = Temperature;
        }

        public void HighPressureMovements()
        {
            // TODO ATMOS finish this

            if(PressureDifference > 15)
            {
                if(_soundCooldown == 0)
                {
                    var coordinates = GridIndices.ToEntityCoordinates(GridIndex, _mapManager);
                    SoundSystem.Play(Filter.Pvs(coordinates), "/Audio/Effects/space_wind.ogg",
                        coordinates, AudioHelpers.WithVariation(0.125f).WithVolume(MathHelper.Clamp(PressureDifference / 10, 10, 100)));
                }
            }

            foreach (var entity in _gridTileLookupSystem.GetEntitiesIntersecting(GridIndex, GridIndices))
            {
                if (!entity.TryGetComponent(out IPhysBody physics)
                    || !entity.IsMovedByPressure(out var pressure)
                    || entity.IsInContainer())
                    continue;

                var pressureMovements = physics.Owner.EnsureComponent<MovedByPressureComponent>();
                if (pressure.LastHighPressureMovementAirCycle < _gridAtmosphereComponent.UpdateCounter)
                {
                    pressureMovements.ExperiencePressureDifference(_gridAtmosphereComponent.UpdateCounter, PressureDifference, _pressureDirection, 0, PressureSpecificTarget?.GridIndices.ToEntityCoordinates(GridIndex, _mapManager) ?? EntityCoordinates.Invalid);
                }

            }

            if (PressureDifference > 100)
            {
                // TODO ATMOS Do space wind graphics here!
            }

            _soundCooldown++;
            if (_soundCooldown > 75)
                _soundCooldown = 0;
        }

        private class TileAtmosphereComparer : IComparer<TileAtmosphere>
        {
            public int Compare(TileAtmosphere a, TileAtmosphere b)
            {
                if (a == null && b == null)
                    return 0;

                if (a == null)
                    return -1;

                if (b == null)
                    return 1;

                return a._tileAtmosInfo.MoleDelta.CompareTo(b._tileAtmosInfo.MoleDelta);
            }
        }

        public void EqualizePressureInZone(int cycleNum)
        {
            if (Air == null || (_tileAtmosInfo.LastCycle >= cycleNum)) return; // Already done.

            _tileAtmosInfo = new TileAtmosInfo();

            var startingMoles = Air.TotalMoles;
            var runAtmos = false;

            // We need to figure if this is necessary
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (!_adjacentBits.IsFlagSet(direction)) continue;
                var other = _adjacentTiles[i];
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

            var queueCycle = ++_gridAtmosphereComponent.EqualizationQueueCycleControl;
            var totalMoles = 0f;
            var tiles = ArrayPool<TileAtmosphere>.Shared.Rent(Atmospherics.ZumosHardTileLimit);
            tiles[0] = this;
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

                for (var j = 0; j < Atmospherics.Directions; j++)
                {
                    var direction = (AtmosDirection) (1 << j);
                    if (!exploring._adjacentBits.IsFlagSet(direction)) continue;
                    var adj = exploring._adjacentTiles[j];
                    if (adj?.Air == null) continue;
                    if(adj._tileAtmosInfo.LastQueueCycle == queueCycle) continue;
                    adj._tileAtmosInfo = new TileAtmosInfo {LastQueueCycle = queueCycle};

                    if(tileCount < Atmospherics.ZumosHardTileLimit)
                        tiles[tileCount++] = adj;
                    if (adj.Air.Immutable)
                    {
                        // Looks like someone opened an airlock to space!
                        ExplosivelyDepressurize(cycleNum);
                        return;
                    }
                }
            }

            if (tileCount > Atmospherics.ZumosTileLimit)
            {
                for (var i = Atmospherics.ZumosTileLimit; i < tileCount; i++)
                {
                    //We unmark them. We shouldn't be pushing/pulling gases to/from them.
                    var tile = tiles[i];
                    if (tile == null) continue;
                    tiles[i]._tileAtmosInfo.LastQueueCycle = 0;
                }

                tileCount = Atmospherics.ZumosTileLimit;
            }

            //tiles = tiles.AsSpan().Slice(0, tileCount).ToArray(); // According to my benchmarks, this is much slower.
            //Array.Resize(ref tiles, tileCount);

            var averageMoles = totalMoles / (tileCount);
            var giverTiles = ArrayPool<TileAtmosphere>.Shared.Rent(tileCount);
            var takerTiles = ArrayPool<TileAtmosphere>.Shared.Rent(tileCount);
            var giverTilesLength = 0;
            var takerTilesLength = 0;

            for (var i = 0; i < tileCount; i++)
            {
                var tile = tiles[i];
                tile._tileAtmosInfo.LastCycle = cycleNum;
                tile._tileAtmosInfo.MoleDelta -= averageMoles;
                if (tile._tileAtmosInfo.MoleDelta > 0)
                {
                    giverTiles[giverTilesLength++] = tile;
                }
                else
                {
                    takerTiles[takerTilesLength++] = tile;
                }
            }

            var logN = MathF.Log2(tileCount);

            // Optimization - try to spread gases using an O(nlogn) algorithm that has a chance of not working first to avoid O(n^2)
            if (giverTilesLength > logN && takerTilesLength > logN)
            {
                // Even if it fails, it will speed up the next part.
                Array.Sort(tiles, 0, tileCount, Comparer);

                for (var i = 0; i < tileCount; i++)
                {
                    var tile = tiles[i];
                    tile._tileAtmosInfo.FastDone = true;
                    if (!(tile._tileAtmosInfo.MoleDelta > 0)) continue;
                    var eligibleDirections = AtmosDirection.Invalid;
                    var eligibleDirectionCount = 0;
                    for (var j = 0; j < Atmospherics.Directions; j++)
                    {
                        var direction = (AtmosDirection) (1 << j);
                        if (!tile._adjacentBits.IsFlagSet(direction)) continue;
                        var tile2 = tile._adjacentTiles[j];

                        // skip anything that isn't part of our current processing block.
                        if (tile2._tileAtmosInfo.FastDone || tile2._tileAtmosInfo.LastQueueCycle != queueCycle)
                            continue;

                        eligibleDirections |= direction;
                        eligibleDirectionCount++;
                    }

                    if (eligibleDirectionCount <= 0)
                        continue; // Oof we've painted ourselves into a corner. Bad luck. Next part will handle this.

                    var molesToMove = tile._tileAtmosInfo.MoleDelta / eligibleDirectionCount;
                    for (var j = 0; j < Atmospherics.Directions; j++)
                    {
                        var direction = (AtmosDirection) (1 << j);
                        if (!eligibleDirections.IsFlagSet(direction)) continue;

                        tile.AdjustEqMovement(direction, molesToMove);
                        tile._tileAtmosInfo.MoleDelta -= molesToMove;
                        tile._adjacentTiles[j]._tileAtmosInfo.MoleDelta += molesToMove;
                    }
                }

                giverTilesLength = 0;
                takerTilesLength = 0;

                for (var i = 0; i < tileCount; i++)
                {
                    var tile = tiles[i];
                    if (tile._tileAtmosInfo.MoleDelta > 0)
                    {
                        giverTiles[giverTilesLength++] = tile;
                    }
                    else
                    {
                        takerTiles[takerTilesLength++] = tile;
                    }
                }
            }

            // This is the part that can become O(n^2).
            if (giverTilesLength < takerTilesLength)
            {
                // as an optimization, we choose one of two methods based on which list is smaller. We really want to avoid O(n^2) if we can.
                var queue = ArrayPool<TileAtmosphere>.Shared.Rent(tileCount);
                for (var j = 0; j < giverTilesLength; j++)
                {
                    var giver = giverTiles[j];
                    giver._tileAtmosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
                    giver._tileAtmosInfo.CurrentTransferAmount = 0;
                    var queueCycleSlow = ++_gridAtmosphereComponent.EqualizationQueueCycleControl;
                    var queueLength = 0;
                    queue[queueLength++] = giver;
                    giver._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                    for (var i = 0; i < queueLength; i++)
                    {
                        if (giver._tileAtmosInfo.MoleDelta <= 0)
                            break; // We're done here now. Let's not do more work than needed.

                        var tile = queue[i];
                        for (var k = 0; k < Atmospherics.Directions; k++)
                        {
                            var direction = (AtmosDirection) (1 << k);
                            if (!tile._adjacentBits.IsFlagSet(direction)) continue;
                            var tile2 = tile._adjacentTiles[k];
                            if (giver._tileAtmosInfo.MoleDelta <= 0) break; // We're done here now. Let's not do more work than needed.
                            if (tile2 == null || tile2._tileAtmosInfo.LastQueueCycle != queueCycle) continue;
                            if (tile2._tileAtmosInfo.LastSlowQueueCycle == queueCycleSlow) continue;

                            queue[queueLength++] = tile2;
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
                    for (var i = queueLength - 1; i >= 0; i--)
                    {
                        var tile = queue[i];
                        if (tile._tileAtmosInfo.CurrentTransferAmount != 0 && tile._tileAtmosInfo.CurrentTransferDirection != AtmosDirection.Invalid)
                        {
                            tile.AdjustEqMovement(tile._tileAtmosInfo.CurrentTransferDirection, tile._tileAtmosInfo.CurrentTransferAmount);
                            tile._adjacentTiles[tile._tileAtmosInfo.CurrentTransferDirection.ToIndex()]
                                ._tileAtmosInfo.CurrentTransferAmount += tile._tileAtmosInfo.CurrentTransferAmount;
                            tile._tileAtmosInfo.CurrentTransferAmount = 0;
                        }
                    }
                }

                ArrayPool<TileAtmosphere>.Shared.Return(queue);
            }
            else
            {
                var queue = ArrayPool<TileAtmosphere>.Shared.Rent(tileCount);
                for (var j = 0; j < takerTilesLength; j++)
                {
                    var taker = takerTiles[j];
                    taker._tileAtmosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
                    taker._tileAtmosInfo.CurrentTransferAmount = 0;
                    var queueCycleSlow = ++_gridAtmosphereComponent.EqualizationQueueCycleControl;
                    var queueLength = 0;
                    queue[queueLength++] = taker;
                    taker._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                    for (var i = 0; i < queueLength; i++)
                    {
                        if (taker._tileAtmosInfo.MoleDelta >= 0)
                            break; // We're done here now. Let's not do more work than needed.

                        var tile = queue[i];
                        for (var k = 0; k < Atmospherics.Directions; k++)
                        {
                            var direction = (AtmosDirection) (1 << k);
                            if (!tile._adjacentBits.IsFlagSet(direction)) continue;
                            var tile2 = tile._adjacentTiles[k];

                            if (taker._tileAtmosInfo.MoleDelta >= 0) break; // We're done here now. Let's not do more work than needed.
                            if (tile2 == null || tile2._tileAtmosInfo.LastQueueCycle != queueCycle) continue;
                            if (tile2._tileAtmosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                            queue[queueLength++] = tile2;
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

                    for (var i = queueLength - 1; i >= 0; i--)
                    {
                        var tile = queue[i];
                        if (tile._tileAtmosInfo.CurrentTransferAmount == 0 || tile._tileAtmosInfo.CurrentTransferDirection == AtmosDirection.Invalid)
                            continue;

                        tile.AdjustEqMovement(tile._tileAtmosInfo.CurrentTransferDirection, tile._tileAtmosInfo.CurrentTransferAmount);

                        tile._adjacentTiles[tile._tileAtmosInfo.CurrentTransferDirection.ToIndex()]
                            ._tileAtmosInfo.CurrentTransferAmount += tile._tileAtmosInfo.CurrentTransferAmount;
                        tile._tileAtmosInfo.CurrentTransferAmount = 0;
                    }
                }

                ArrayPool<TileAtmosphere>.Shared.Return(queue);
            }

            for (var i = 0; i < tileCount; i++)
            {
                var tile = tiles[i];
                tile.FinalizeEq();
            }

            for (var i = 0; i < tileCount; i++)
            {
                var tile = tiles[i];
                for (var j = 0; j < Atmospherics.Directions; j++)
                {
                    var direction = (AtmosDirection) (1 << j);
                    if (!tile._adjacentBits.IsFlagSet(direction)) continue;
                    var tile2 = tile._adjacentTiles[j];
                    if (tile2?.Air?.Compare(Air) == GasMixture.GasCompareResult.NoExchange) continue;
                    _gridAtmosphereComponent.AddActiveTile(tile2);
                    break;
                }
            }

            ArrayPool<TileAtmosphere>.Shared.Return(tiles);
            ArrayPool<TileAtmosphere>.Shared.Return(giverTiles);
            ArrayPool<TileAtmosphere>.Shared.Return(takerTiles);
        }

        private void FinalizeEq()
        {
            Span<float> transferDirections = stackalloc float[Atmospherics.Directions];
            var hasTransferDirs = false;
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var amount = _tileAtmosInfo[i];
                if (amount == 0) continue;
                transferDirections[i] = amount;
                _tileAtmosInfo[i] = 0; // Set them to 0 to prevent infinite recursion.
                hasTransferDirs = true;
            }

            if (!hasTransferDirs) return;

            for(var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (!_adjacentBits.IsFlagSet(direction)) continue;
                var amount = transferDirections[i];
                var tile = _adjacentTiles[i];
                if (tile?.Air == null) continue;
                if (amount > 0)
                {
                    if (Air.TotalMoles < amount)
                        FinalizeEqNeighbors(transferDirections);

                    tile._tileAtmosInfo[direction.GetOpposite()] = 0;
                    tile.Air.Merge(Air.Remove(amount));
                    UpdateVisuals();
                    tile.UpdateVisuals();
                    ConsiderPressureDifference(tile, amount);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinalizeEqNeighbors(ReadOnlySpan<float> transferDirs)
        {
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                var amount = transferDirs[i];
                if(amount < 0 && _adjacentBits.IsFlagSet(direction))
                    _adjacentTiles[i].FinalizeEq();  // A bit of recursion if needed.
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ConsiderPressureDifference(TileAtmosphere other, float difference)
        {
            _gridAtmosphereComponent.AddHighPressureDelta(this);
            if (difference > PressureDifference)
            {
                PressureDifference = difference;
                _pressureDirection = ((Vector2i)(GridIndices - other.GridIndices)).GetDir().ToAtmosDirection();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AdjustEqMovement(AtmosDirection direction, float amount)
        {
            _tileAtmosInfo[direction] += amount;
            _adjacentTiles[direction.ToIndex()]._tileAtmosInfo[direction.GetOpposite()] -= amount;
        }

        public void ProcessCell(int fireCount, bool spaceWind = true)
        {
            // Can't process a tile without air
            if (Air == null)
            {
                _gridAtmosphereComponent.RemoveActiveTile(this);
                return;
            }

            if (_archivedCycle < fireCount)
                Archive(fireCount);

            _currentCycle = fireCount;
            var adjacentTileLength = 0;

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if(_adjacentBits.IsFlagSet(direction))
                    adjacentTileLength++;
            }

            for(var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (!_adjacentBits.IsFlagSet(direction)) continue;
                var enemyTile = _adjacentTiles[i];

                // If the tile is null or has no air, we don't do anything for it.
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
                } else if (Air.Compare(enemyTile.Air) != GasMixture.GasCompareResult.NoExchange)
                {
                    if (!enemyTile.Excited)
                    {
                        _gridAtmosphereComponent.AddActiveTile(enemyTile);
                    }

                    var excitedGroup = ExcitedGroup;
                    excitedGroup ??= enemyTile.ExcitedGroup;

                    if (excitedGroup == null)
                    {
                        excitedGroup = new ExcitedGroup();
                        excitedGroup.Initialize(_gridAtmosphereComponent);
                    }

                    if (ExcitedGroup == null)
                        excitedGroup.AddTile(this);

                    if(enemyTile.ExcitedGroup == null)
                        excitedGroup.AddTile(enemyTile);

                    shouldShareAir = true;
                }

                if (shouldShareAir)
                {
                    var difference = Air.Share(enemyTile.Air, adjacentTileLength);

                    if (spaceWind)
                    {
                        if (difference > 0)
                        {
                            ConsiderPressureDifference(enemyTile, difference);
                        }
                        else
                        {
                            enemyTile.ConsiderPressureDifference(this, -difference);
                        }
                    }

                    LastShareCheck();
                }
            }

            React();
            UpdateVisuals();

            var remove = true;

            if(Air.Temperature > Atmospherics.MinimumTemperatureStartSuperConduction)
                if (ConsiderSuperconductivity(true))
                    remove = false;

            if(ExcitedGroup == null && remove)
                _gridAtmosphereComponent.RemoveActiveTile(this);
        }

        public void ProcessHotspot()
        {
            if (!Hotspot.Valid)
            {
                _gridAtmosphereComponent.RemoveHotspotTile(this);
                return;
            }

            if (!Excited)
            {
                _gridAtmosphereComponent.AddActiveTile(this);
            }

            if (!Hotspot.SkippedFirstProcess)
            {
                Hotspot.SkippedFirstProcess = true;
                return;
            }

            ExcitedGroup?.ResetCooldowns();

            if ((Hotspot.Temperature < Atmospherics.FireMinimumTemperatureToExist) || (Hotspot.Volume <= 1f)
                || Air == null || Air.Gases[(int)Gas.Oxygen] < 0.5f || (Air.Gases[(int)Gas.Plasma] < 0.5f && Air.GetMoles(Gas.Tritium) < 0.5f))
            {
                Hotspot = new Hotspot();
                UpdateVisuals();
                return;
            }

            PerformHotspotExposure();

            if (Hotspot.Bypassing)
            {
                Hotspot.State = 3;
                _gridAtmosphereComponent.BurnTile(GridIndices);

                if (Air.Temperature > Atmospherics.FireMinimumTemperatureToSpread)
                {
                    var radiatedTemperature = Air.Temperature * Atmospherics.FireSpreadRadiosityScale;
                    foreach (var tile in _adjacentTiles)
                    {
                        if(!tile.Hotspot.Valid)
                            tile.HotspotExpose(radiatedTemperature, Atmospherics.CellVolume/4);
                    }
                }
            }
            else
            {
                Hotspot.State = (byte) (Hotspot.Volume > Atmospherics.CellVolume * 0.4f ? 2 : 1);
            }

            if (Hotspot.Temperature > MaxFireTemperatureSustained)
                MaxFireTemperatureSustained = Hotspot.Temperature;

            // TODO ATMOS Maybe destroy location here?
        }

        public float MaxFireTemperatureSustained { get; private set; }

        private void PerformHotspotExposure()
        {
            if (Air == null || !Hotspot.Valid) return;

            Hotspot.Bypassing = Hotspot.SkippedFirstProcess && Hotspot.Volume > Air.Volume*0.95f;

            if (Hotspot.Bypassing)
            {
                Hotspot.Volume = Air.ReactionResults[GasReaction.Fire] * Atmospherics.FireGrowthRate;
                Hotspot.Temperature = Air.Temperature;
            }
            else
            {
                var affected = Air.RemoveRatio(Hotspot.Volume / Air.Volume);
                affected.Temperature = Hotspot.Temperature;
                affected.React(this);
                Hotspot.Temperature = affected.Temperature;
                Hotspot.Volume = affected.ReactionResults[GasReaction.Fire] * Atmospherics.FireGrowthRate;
                AssumeAir(affected);
            }

            var tileRef = GridIndices.GetTileRef(GridIndex);

            foreach (var entity in tileRef.GetEntitiesInTileFast(_gridTileLookupSystem))
            {
                foreach (var fireAct in entity.GetAllComponents<IFireAct>())
                {

                    fireAct.FireAct(Hotspot.Temperature, Hotspot.Volume);
                }
            }
        }

        public void HotspotExpose(float exposedTemperature, float exposedVolume, bool soh = false)
        {
            if (Air == null)
                return;

            var oxygen = Air.GetMoles(Gas.Oxygen);

            if (oxygen < 0.5f)
                return;

            var plasma = Air.GetMoles(Gas.Plasma);
            var tritium = Air.GetMoles(Gas.Tritium);

            if (Hotspot.Valid)
            {
                if (soh)
                {
                    if (plasma > 0.5f || tritium > 0.5f)
                    {
                        if (Hotspot.Temperature < exposedTemperature)
                            Hotspot.Temperature = exposedTemperature;
                        if (Hotspot.Volume < exposedVolume)
                            Hotspot.Volume = exposedVolume;
                    }
                }

                return;
            }

            if ((exposedTemperature > Atmospherics.PlasmaMinimumBurnTemperature) && (plasma > 0.5f || tritium > 0.5f))
            {
                Hotspot = new Hotspot
                {
                    Volume = exposedVolume * 25f,
                    Temperature = exposedTemperature,
                    SkippedFirstProcess = _currentCycle > _gridAtmosphereComponent.UpdateCounter
                };

                Hotspot.Start();

                _gridAtmosphereComponent.AddActiveTile(this);
                _gridAtmosphereComponent.AddHotspotTile(this);
            }
        }

        private bool ConsiderSuperconductivity()
        {
            if (ThermalConductivity == 0f)
                return false;

            _gridAtmosphereComponent.AddSuperconductivityTile(this);
            return true;
        }

        private bool ConsiderSuperconductivity(bool starting)
        {
            if (Air.Temperature < (starting
                ? Atmospherics.MinimumTemperatureStartSuperConduction
                : Atmospherics.MinimumTemperatureForSuperconduction))
                return false;

            return !(Air.HeatCapacity < Atmospherics.MCellWithRatio) && ConsiderSuperconductivity();
        }

        public void Superconduct()
        {
            var directions = ConductivityDirections();

            for(var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (!directions.IsFlagSet(direction)) continue;

                var adjacent = _adjacentTiles[direction.ToIndex()];

                // TODO ATMOS handle adjacent being null.
                if (adjacent == null || adjacent.ThermalConductivity == 0f)
                    continue;

                if(adjacent._archivedCycle < _gridAtmosphereComponent.UpdateCounter)
                    adjacent.Archive(_gridAtmosphereComponent.UpdateCounter);

                adjacent.NeighborConductWithSource(this);

                adjacent.ConsiderSuperconductivity();
            }

            RadiateToSpace();

            FinishSuperconduction();
        }

        private void FinishSuperconduction()
        {
            // Conduct with air on my tile if I have it
            if (!BlocksAllAir)
            {
                Temperature = Air.TemperatureShare(ThermalConductivity, Temperature, HeatCapacity);
            }

            FinishSuperconduction(BlocksAllAir ? Temperature : Air.Temperature);
        }

        private void FinishSuperconduction(float temperature)
        {
            // Make sure it's still hot enough to continue conducting.
            if (temperature < Atmospherics.MinimumTemperatureForSuperconduction)
            {
                _gridAtmosphereComponent.RemoveSuperconductivityTile(this);
            }
        }

        private void NeighborConductWithSource(TileAtmosphere other)
        {
            if (BlocksAllAir)
            {
                if (!other.BlocksAllAir)
                {
                    other.TemperatureShareOpenToSolid(this);
                }
                else
                {
                    other.TemperatureShareMutualSolid(this, ThermalConductivity);
                }

                TemperatureExpose(null, Temperature, _gridAtmosphereComponent.GetVolumeForCells(1));
                return;
            }

            if (!other.BlocksAllAir)
            {
                other.Air.TemperatureShare(Air, Atmospherics.WindowHeatTransferCoefficient);
            }
            else
            {
                TemperatureShareOpenToSolid(other);
            }

            _gridAtmosphereComponent.AddActiveTile(this);
        }

        private void TemperatureShareOpenToSolid(TileAtmosphere other)
        {
            other.Temperature =
                Air.TemperatureShare(other.ThermalConductivity, other.Temperature, other.HeatCapacity);
        }

        private void TemperatureShareMutualSolid(TileAtmosphere other, float conductionCoefficient)
        {
            var deltaTemperature = (_temperatureArchived - other._temperatureArchived);
            if (MathF.Abs(deltaTemperature) > Atmospherics.MinimumTemperatureDeltaToConsider
                && HeatCapacity != 0f && other.HeatCapacity != 0f)
            {
                var heat = conductionCoefficient * deltaTemperature *
                           (HeatCapacity * other.HeatCapacity / (HeatCapacity + other.HeatCapacity));

                Temperature -= heat / HeatCapacity;
                other.Temperature += heat / other.HeatCapacity;
            }
        }

        public void RadiateToSpace()
        {
            // Considering 0ºC as the break even point for radiation in and out.
            if (Temperature > Atmospherics.T0C)
            {
                // Hardcoded space temperature.
                var deltaTemperature = (_temperatureArchived - Atmospherics.TCMB);
                if ((HeatCapacity > 0) && (MathF.Abs(deltaTemperature) > Atmospherics.MinimumTemperatureDeltaToConsider))
                {
                    var heat = ThermalConductivity * deltaTemperature * (HeatCapacity *
                        Atmospherics.HeatCapacityVacuum / (HeatCapacity + Atmospherics.HeatCapacityVacuum));

                    Temperature -= heat;
                }
            }
        }

        public AtmosDirection ConductivityDirections()
        {
            if(BlocksAllAir)
            {
                if(_archivedCycle < _gridAtmosphereComponent.UpdateCounter)
                    Archive(_gridAtmosphereComponent.UpdateCounter);
                return AtmosDirection.All;
            }

            // TODO ATMOS check if this is correct
            return AtmosDirection.All;
        }

        public void ExplosivelyDepressurize(int cycleNum)
        {
            if (Air == null) return;

            const int limit = Atmospherics.ZumosHardTileLimit;

            var totalGasesRemoved = 0f;
            var queueCycle = ++_gridAtmosphereComponent.EqualizationQueueCycleControl;
            var tiles = ArrayPool<TileAtmosphere>.Shared.Rent(limit);
            var spaceTiles = ArrayPool<TileAtmosphere>.Shared.Rent(limit);

            var tileCount = 0;
            var spaceTileCount = 0;

            tiles[tileCount++] = this;

            _tileAtmosInfo = new TileAtmosInfo {LastQueueCycle = queueCycle};

            for (var i = 0; i < tileCount; i++)
            {
                var tile = tiles[i];
                tile._tileAtmosInfo.LastCycle = cycleNum;
                tile._tileAtmosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
                if (tile.Air.Immutable)
                {
                    spaceTiles[spaceTileCount++] = tile;
                    tile.PressureSpecificTarget = tile;
                }
                else
                {
                    for (var j = 0; j < Atmospherics.Directions; j++)
                    {
                        var direction = (AtmosDirection) (1 << j);
                        if (!tile._adjacentBits.IsFlagSet(direction)) continue;
                        var tile2 = tile._adjacentTiles[j];
                        if (tile2.Air == null) continue;
                        if (tile2._tileAtmosInfo.LastQueueCycle == queueCycle) continue;

                        tile.ConsiderFirelocks(tile2);

                        // The firelocks might have closed on us.
                        if (!tile._adjacentBits.IsFlagSet(direction)) continue;
                        tile2._tileAtmosInfo = new TileAtmosInfo {LastQueueCycle = queueCycle};
                        tiles[tileCount++] = tile2;
                    }
                }

                if (tileCount >= limit || spaceTileCount >= limit)
                    break;
            }

            var queueCycleSlow = ++_gridAtmosphereComponent.EqualizationQueueCycleControl;
            var progressionOrder = ArrayPool<TileAtmosphere>.Shared.Rent(limit * 2);
            var progressionCount = 0;

            for (var i = 0; i < spaceTileCount; i++)
            {
                var tile = spaceTiles[i];
                progressionOrder[progressionCount++] = tile;
                tile._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                tile._tileAtmosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
            }

            for (var i = 0; i < progressionCount; i++)
            {
                var tile = progressionOrder[i];
                for (var j = 0; j < Atmospherics.Directions; j++)
                {
                    var direction = (AtmosDirection) (1 << j);
                    // TODO ATMOS This is a terrible hack that accounts for the mess that are space TileAtmospheres.
                    if (!tile._adjacentBits.IsFlagSet(direction) && !tile.Air.Immutable) continue;
                    var tile2 = tile._adjacentTiles[j];
                    if (tile2?._tileAtmosInfo.LastQueueCycle != queueCycle) continue;
                    if (tile2._tileAtmosInfo.LastSlowQueueCycle == queueCycleSlow) continue;
                    if(tile2.Air?.Immutable ?? false) continue;
                    tile2._tileAtmosInfo.CurrentTransferDirection = direction.GetOpposite();
                    tile2._tileAtmosInfo.CurrentTransferAmount = 0;
                    tile2.PressureSpecificTarget = tile.PressureSpecificTarget;
                    tile2._tileAtmosInfo.LastSlowQueueCycle = queueCycleSlow;
                    progressionOrder[progressionCount++] = tile2;
                }
            }

            for (var i = progressionCount - 1; i >= 0; i--)
            {
                var tile = progressionOrder[i];
                if (tile._tileAtmosInfo.CurrentTransferDirection == AtmosDirection.Invalid) continue;
                _gridAtmosphereComponent.AddHighPressureDelta(tile);
                _gridAtmosphereComponent.AddActiveTile(tile);
                var tile2 = tile._adjacentTiles[tile._tileAtmosInfo.CurrentTransferDirection.ToIndex()];
                if (tile2?.Air == null) continue;
                var sum = tile2.Air.TotalMoles;
                totalGasesRemoved += sum;
                tile._tileAtmosInfo.CurrentTransferAmount += sum;
                tile2._tileAtmosInfo.CurrentTransferAmount += tile._tileAtmosInfo.CurrentTransferAmount;
                tile.PressureDifference = tile._tileAtmosInfo.CurrentTransferAmount;
                tile._pressureDirection = tile._tileAtmosInfo.CurrentTransferDirection;

                if (tile2._tileAtmosInfo.CurrentTransferDirection == AtmosDirection.Invalid)
                {
                    tile2.PressureDifference = tile2._tileAtmosInfo.CurrentTransferAmount;
                    tile2._pressureDirection = tile._tileAtmosInfo.CurrentTransferDirection;
                }

                tile.Air.Clear();
                tile.UpdateVisuals();
                tile.HandleDecompressionFloorRip(sum);
            }

            ArrayPool<TileAtmosphere>.Shared.Return(tiles);
            ArrayPool<TileAtmosphere>.Shared.Return(spaceTiles);
            ArrayPool<TileAtmosphere>.Shared.Return(progressionOrder);
        }

        private void HandleDecompressionFloorRip(float sum)
        {
            var chance = MathHelper.Clamp(sum / 500, 0.005f, 0.5f);
            if (sum > 20 && _robustRandom.Prob(chance))
                _gridAtmosphereComponent.PryTile(GridIndices);
        }

        private void ConsiderFirelocks(TileAtmosphere other)
        {
            var reconsiderAdjacent = false;

            foreach (var entity in GridIndices.GetEntitiesInTileFast(GridIndex, _gridAtmosphereComponent.GridTileLookupSystem))
            {
                if (!entity.TryGetComponent(out FirelockComponent firelock)) continue;
                reconsiderAdjacent |= firelock.EmergencyPressureStop();
            }

            foreach (var entity in other.GridIndices.GetEntitiesInTileFast(other.GridIndex, _gridAtmosphereComponent.GridTileLookupSystem))
            {
                if (!entity.TryGetComponent(out FirelockComponent firelock)) continue;
                reconsiderAdjacent |= firelock.EmergencyPressureStop();
            }

            if (reconsiderAdjacent)
            {
                UpdateAdjacent();
                other.UpdateAdjacent();
            }
        }

        private void React()
        {
            // TODO ATMOS I think this is enough? gotta make sure...
            Air?.React(this);
        }

        public bool AssumeAir(GasMixture giver)
        {
            if (Air == null) return false;

            Air.Merge(giver);

            UpdateVisuals();

            if (!Excited)
            {
                _gridAtmosphereComponent.AddActiveTile(this);
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateVisuals()
        {
            if (Air == null) return;

            _gridAtmosphereComponent.GasTileOverlaySystem.Invalidate(GridIndex, GridIndices);
        }

        public void UpdateAdjacent()
        {
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);

                var otherIndices = GridIndices.Offset(direction.ToDirection());

                var isSpace = _gridAtmosphereComponent.IsSpace(GridIndices);
                var adjacent = _gridAtmosphereComponent.GetTile(otherIndices, !isSpace);
                _adjacentTiles[direction.ToIndex()] = adjacent;
                adjacent?.UpdateAdjacent(direction.GetOpposite());

                if (adjacent != null && !BlockedAirflow.IsFlagSet(direction) && !_gridAtmosphereComponent.IsAirBlocked(adjacent.GridIndices, direction.GetOpposite()))
                {
                    _adjacentBits |= direction;
                }
            }
        }

        public void UpdateAdjacent(AtmosDirection direction)
        {
            _adjacentTiles[direction.ToIndex()] = _gridAtmosphereComponent.GetTile(GridIndices.Offset(direction.ToDirection()));

            if (!BlockedAirflow.IsFlagSet(direction) && !_gridAtmosphereComponent.IsAirBlocked(GridIndices.Offset(direction.ToDirection()), direction.GetOpposite()))
            {
                _adjacentBits |= direction;
            }
            else
            {
                _adjacentBits &= ~direction;
            }
        }

        /// <summary>
        ///     Calls <see cref="GridAtmosphereComponent.Invalidate"/> on this tile atmosphere's position.
        /// </summary>
        public void Invalidate()
        {
            _gridAtmosphereComponent.Invalidate(GridIndices);
        }

        private void LastShareCheck()
        {
            var lastShare = Air.LastShare;
            if (lastShare > Atmospherics.MinimumAirToSuspend)
            {
                ExcitedGroup.ResetCooldowns();
            } else if (lastShare > Atmospherics.MinimumMolesDeltaToMove)
            {
                ExcitedGroup.DismantleCooldown = 0;
            }
        }

        public void TemperatureExpose(GasMixture air, float temperature, float volume)
        {
            // TODO ATMOS do this
        }
    }
}
