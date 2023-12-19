using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private readonly Stopwatch _simulationStopwatch = new();

        /// <summary>
        ///     Check current execution time every n instances processed.
        /// </summary>
        private const int LagCheckIterations = 30;

        /// <summary>
        ///     Check current execution time every n instances processed.
        /// </summary>
        private const int InvalidCoordinatesLagCheckIterations = 50;

        private int _currentRunAtmosphereIndex;
        private bool _simulationPaused;

        private readonly List<Entity<GridAtmosphereComponent>> _currentRunAtmosphere = new();

        /// <summary>
        ///     Revalidates all invalid coordinates in a grid atmosphere.
        /// </summary>
        /// <param name="ent">The grid atmosphere in question.</param>
        /// <returns>Whether the process succeeded or got paused due to time constrains.</returns>
        private bool ProcessRevalidate(Entity<GridAtmosphereComponent> ent, GasTileOverlayComponent? visuals)
        {
            var (owner, atmosphere) = ent;
            if (!atmosphere.ProcessingPaused)
            {
                atmosphere.CurrentRunInvalidatedCoordinates.Clear();
                atmosphere.CurrentRunInvalidatedCoordinates.EnsureCapacity(atmosphere.InvalidatedCoords.Count);
                foreach (var tile in atmosphere.InvalidatedCoords)
                {
                    atmosphere.CurrentRunInvalidatedCoordinates.Enqueue(tile);
                }
                atmosphere.InvalidatedCoords.Clear();
            }

            if (!TryComp(owner, out MapGridComponent? mapGridComp))
                return true;

            var mapUid = _mapManager.GetMapEntityIdOrThrow(Transform(owner).MapID);

            var volume = GetVolumeForTiles(mapGridComp);

            var number = 0;
            while (atmosphere.CurrentRunInvalidatedCoordinates.TryDequeue(out var indices))
            {
                if (!atmosphere.Tiles.TryGetValue(indices, out var tile))
                {
                    tile = new TileAtmosphere(owner, indices,
                        new GasMixture(volume) { Temperature = Atmospherics.T20C });
                    atmosphere.Tiles[indices] = tile;
                }

                var airBlockedEv = new IsTileAirBlockedMethodEvent(owner, indices, MapGridComponent:mapGridComp);
                GridIsTileAirBlocked(owner, atmosphere, ref airBlockedEv);
                var isAirBlocked = airBlockedEv.Result;

                var oldBlocked = tile.BlockedAirflow;
                var updateAdjacentEv = new UpdateAdjacentMethodEvent(owner, indices, mapGridComp);
                GridUpdateAdjacent(owner, atmosphere, ref updateAdjacentEv);

                // Blocked airflow changed, rebuild excited groups!
                if (tile.Excited && tile.BlockedAirflow != oldBlocked)
                {
                    RemoveActiveTile(atmosphere, tile);
                }

                // Call this instead of the grid method as the map has a say on whether the tile is space or not.
                if ((!mapGridComp.TryGetTileRef(indices, out var t) || t.IsSpace(_tileDefinitionManager)) && !isAirBlocked)
                {
                    tile.Air = GetTileMixture(null, mapUid, indices);
                    tile.MolesArchived = tile.Air != null ? new float[Atmospherics.AdjustedNumberOfGases] : null;
                    tile.Space = IsTileSpace(null, mapUid, indices, mapGridComp);
                }
                else if (isAirBlocked)
                {
                    if (airBlockedEv.NoAir)
                    {
                        tile.Air = null;
                        tile.MolesArchived = null;
                        tile.ArchivedCycle = 0;
                        tile.LastShare = 0f;
                        tile.Hotspot = new Hotspot();
                    }
                }
                else
                {
                    if (tile.Air == null && NeedsVacuumFixing(mapGridComp, indices))
                    {
                        var vacuumEv = new FixTileVacuumMethodEvent(owner, indices);
                        GridFixTileVacuum(owner, atmosphere, ref vacuumEv);
                    }

                    // Tile used to be space, but isn't anymore.
                    if (tile.Space || (tile.Air?.Immutable ?? false))
                    {
                        tile.Air = null;
                        tile.MolesArchived = null;
                        tile.ArchivedCycle = 0;
                        tile.LastShare = 0f;
                        tile.Space = false;
                    }

                    tile.Air ??= new GasMixture(volume){Temperature = Atmospherics.T20C};
                    tile.MolesArchived ??= new float[Atmospherics.AdjustedNumberOfGases];
                }

                // We activate the tile.
                AddActiveTile(atmosphere, tile);

                // TODO ATMOS: Query all the contents of this tile (like walls) and calculate the correct thermal conductivity and heat capacity
                var tileDef = mapGridComp.TryGetTileRef(indices, out var tileRef)
                    ? tileRef.GetContentTileDefinition(_tileDefinitionManager)
                    : null;

                tile.ThermalConductivity = tileDef?.ThermalConductivity ?? 0.5f;
                tile.HeatCapacity = tileDef?.HeatCapacity ?? float.PositiveInfinity;
                InvalidateVisuals(owner, indices, visuals);

                for (var i = 0; i < Atmospherics.Directions; i++)
                {
                    var direction = (AtmosDirection) (1 << i);
                    var otherIndices = indices.Offset(direction);

                    if (atmosphere.Tiles.TryGetValue(otherIndices, out var otherTile))
                        AddActiveTile(atmosphere, otherTile);
                }

                if (number++ < InvalidCoordinatesLagCheckIterations)
                    continue;

                number = 0;
                // Process the rest next time.
                if (_simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime)
                {
                    return false;
                }
            }

            return true;
        }

        private void QueueRunTiles(
            Queue<TileAtmosphere> queue,
            HashSet<TileAtmosphere> tiles)
        {

            queue.Clear();
            queue.EnsureCapacity(tiles.Count);
            foreach (var tile in tiles)
            {
                queue.Enqueue(tile);
            }
        }

        private bool ProcessTileEqualize(Entity<GridAtmosphereComponent> ent, GasTileOverlayComponent? visuals)
        {
            var (uid, atmosphere) = ent;
            if (!atmosphere.ProcessingPaused)
                QueueRunTiles(atmosphere.CurrentRunTiles, atmosphere.ActiveTiles);

            if (!TryComp(uid, out MapGridComponent? mapGridComp))
                throw new Exception("Tried to process a grid atmosphere on an entity that isn't a grid!");

            var number = 0;
            while (atmosphere.CurrentRunTiles.TryDequeue(out var tile))
            {
                EqualizePressureInZone((uid, mapGridComp, atmosphere), tile, atmosphere.UpdateCounter, visuals);

                if (number++ < LagCheckIterations)
                    continue;

                number = 0;
                // Process the rest next time.
                if (_simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ProcessActiveTiles(GridAtmosphereComponent atmosphere, GasTileOverlayComponent? visuals)
        {
            if(!atmosphere.ProcessingPaused)
                QueueRunTiles(atmosphere.CurrentRunTiles, atmosphere.ActiveTiles);

            var number = 0;
            while (atmosphere.CurrentRunTiles.TryDequeue(out var tile))
            {
                ProcessCell(atmosphere, tile, atmosphere.UpdateCounter, visuals);

                if (number++ < LagCheckIterations)
                    continue;

                number = 0;
                // Process the rest next time.
                if (_simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ProcessExcitedGroups(GridAtmosphereComponent gridAtmosphere)
        {
            if (!gridAtmosphere.ProcessingPaused)
            {
                gridAtmosphere.CurrentRunExcitedGroups.Clear();
                gridAtmosphere.CurrentRunExcitedGroups.EnsureCapacity(gridAtmosphere.ExcitedGroups.Count);
                foreach (var group in gridAtmosphere.ExcitedGroups)
                {
                    gridAtmosphere.CurrentRunExcitedGroups.Enqueue(group);
                }
            }

            var number = 0;
            while (gridAtmosphere.CurrentRunExcitedGroups.TryDequeue(out var excitedGroup))
            {
                excitedGroup.BreakdownCooldown++;
                excitedGroup.DismantleCooldown++;

                if(excitedGroup.BreakdownCooldown > Atmospherics.ExcitedGroupBreakdownCycles)
                    ExcitedGroupSelfBreakdown(gridAtmosphere, excitedGroup);

                else if(excitedGroup.DismantleCooldown > Atmospherics.ExcitedGroupsDismantleCycles)
                    ExcitedGroupDismantle(gridAtmosphere, excitedGroup);

                if (number++ < LagCheckIterations)
                    continue;

                number = 0;
                // Process the rest next time.
                if (_simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ProcessHighPressureDelta(Entity<GridAtmosphereComponent> ent)
        {
            var atmosphere = ent.Comp;
            if (!atmosphere.ProcessingPaused)
                QueueRunTiles(atmosphere.CurrentRunTiles, atmosphere.HighPressureDelta);

            // Note: This is still processed even if space wind is turned off since this handles playing the sounds.

            var number = 0;
            var bodies = EntityManager.GetEntityQuery<PhysicsComponent>();
            var xforms = EntityManager.GetEntityQuery<TransformComponent>();
            var metas = EntityManager.GetEntityQuery<MetaDataComponent>();
            var pressureQuery = EntityManager.GetEntityQuery<MovedByPressureComponent>();

            while (atmosphere.CurrentRunTiles.TryDequeue(out var tile))
            {
                HighPressureMovements(ent, tile, bodies, xforms, pressureQuery, metas);
                tile.PressureDifference = 0f;
                tile.LastPressureDirection = tile.PressureDirection;
                tile.PressureDirection = AtmosDirection.Invalid;
                tile.PressureSpecificTarget = null;
                atmosphere.HighPressureDelta.Remove(tile);

                if (number++ < LagCheckIterations)
                    continue;
                number = 0;
                // Process the rest next time.
                if (_simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ProcessHotspots(GridAtmosphereComponent atmosphere)
        {
            if(!atmosphere.ProcessingPaused)
                QueueRunTiles(atmosphere.CurrentRunTiles, atmosphere.HotspotTiles);

            var number = 0;
            while (atmosphere.CurrentRunTiles.TryDequeue(out var hotspot))
            {
                ProcessHotspot(atmosphere, hotspot);

                if (number++ < LagCheckIterations)
                    continue;

                number = 0;
                // Process the rest next time.
                if (_simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ProcessSuperconductivity(GridAtmosphereComponent atmosphere)
        {
            if(!atmosphere.ProcessingPaused)
                QueueRunTiles(atmosphere.CurrentRunTiles, atmosphere.SuperconductivityTiles);

            var number = 0;
            while (atmosphere.CurrentRunTiles.TryDequeue(out var superconductivity))
            {
                Superconduct(atmosphere, superconductivity);

                if (number++ < LagCheckIterations)
                    continue;

                number = 0;
                // Process the rest next time.
                if (_simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ProcessPipeNets(GridAtmosphereComponent atmosphere)
        {
            if (!atmosphere.ProcessingPaused)
            {
                atmosphere.CurrentRunPipeNet.Clear();
                atmosphere.CurrentRunPipeNet.EnsureCapacity(atmosphere.PipeNets.Count);
                foreach (var net in atmosphere.PipeNets)
                {
                    atmosphere.CurrentRunPipeNet.Enqueue(net);
                }
            }

            var number = 0;
            while (atmosphere.CurrentRunPipeNet.TryDequeue(out var pipenet))
            {
                pipenet.Update();

                if (number++ < LagCheckIterations)
                    continue;

                number = 0;
                // Process the rest next time.
                if (_simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime)
                {
                    return false;
                }
            }

            return true;
        }

        /**
         * UpdateProcessing() takes a different number of calls to go through all of atmos
         * processing depending on what options are enabled. This returns the actual effective time
         * between atmos updates that devices actually experience.
         */
        public float RealAtmosTime()
        {
            int num = (int)AtmosphereProcessingState.NumStates;
            if (!MonstermosEqualization)
                num--;
            if (!ExcitedGroups)
                num--;
            if (!Superconduction)
                num--;
            return num * AtmosTime;
        }

        private bool ProcessAtmosDevices(GridAtmosphereComponent atmosphere)
        {
            if (!atmosphere.ProcessingPaused)
            {
                atmosphere.CurrentRunAtmosDevices.Clear();
                atmosphere.CurrentRunAtmosDevices.EnsureCapacity(atmosphere.AtmosDevices.Count);
                foreach (var device in atmosphere.AtmosDevices)
                {
                    atmosphere.CurrentRunAtmosDevices.Enqueue(device);
                }
            }

            var time = _gameTiming.CurTime;
            var number = 0;
            while (atmosphere.CurrentRunAtmosDevices.TryDequeue(out var device))
            {
                RaiseLocalEvent(device, new AtmosDeviceUpdateEvent(RealAtmosTime()));
                device.Comp.LastProcess = time;

                if (number++ < LagCheckIterations)
                    continue;

                number = 0;
                // Process the rest next time.
                if (_simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime)
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdateProcessing(float frameTime)
        {
            _simulationStopwatch.Restart();

            if (!_simulationPaused)
            {
                _currentRunAtmosphereIndex = 0;
                _currentRunAtmosphere.Clear();

                var query = EntityQueryEnumerator<GridAtmosphereComponent>();
                while (query.MoveNext(out var uid, out var grid))
                {
                    _currentRunAtmosphere.Add((uid, grid));
                }
            }

            // We set this to true just in case we have to stop processing due to time constraints.
            _simulationPaused = true;

            for (; _currentRunAtmosphereIndex < _currentRunAtmosphere.Count; _currentRunAtmosphereIndex++)
            {
                var ent = _currentRunAtmosphere[_currentRunAtmosphereIndex];
                var (owner, atmosphere) = ent;
                TryComp(owner, out GasTileOverlayComponent? visuals);

                if (!TryComp(owner, out TransformComponent? x)
                    || x.MapUid == null
                    || TerminatingOrDeleted(x.MapUid.Value)
                    || x.MapID == MapId.Nullspace)
                {
                    Log.Error($"Attempting to process atmos without a map? Entity: {ToPrettyString(owner)}");
                    _simulationPaused = false;
                    _currentRunAtmosphere.Clear();
                    continue;
                }

                if (atmosphere.LifeStage >= ComponentLifeStage.Stopping || Paused(owner) || !atmosphere.Simulated)
                    continue;

                atmosphere.Timer += frameTime;

                if (atmosphere.Timer < AtmosTime)
                    continue;

                // We subtract it so it takes lost time into account.
                atmosphere.Timer -= AtmosTime;

                switch (atmosphere.State)
                {
                    case AtmosphereProcessingState.Revalidate:
                        if (!ProcessRevalidate(ent, visuals))
                        {
                            atmosphere.ProcessingPaused = true;
                            return;
                        }

                        atmosphere.ProcessingPaused = false;
                        // Next state depends on whether monstermos equalization is enabled or not.
                        // Note: We do this here instead of on the tile equalization step to prevent ending it early.
                        //       Therefore, a change to this CVar might only be applied after that step is over.
                        atmosphere.State = MonstermosEqualization
                            ? AtmosphereProcessingState.TileEqualize
                            : AtmosphereProcessingState.ActiveTiles;
                        continue;
                    case AtmosphereProcessingState.TileEqualize:
                        if (!ProcessTileEqualize(ent, visuals))
                        {
                            atmosphere.ProcessingPaused = true;
                            return;
                        }

                        atmosphere.ProcessingPaused = false;
                        atmosphere.State = AtmosphereProcessingState.ActiveTiles;
                        continue;
                    case AtmosphereProcessingState.ActiveTiles:
                        if (!ProcessActiveTiles(atmosphere, visuals))
                        {
                            atmosphere.ProcessingPaused = true;
                            return;
                        }

                        atmosphere.ProcessingPaused = false;
                        // Next state depends on whether excited groups are enabled or not.
                        atmosphere.State = ExcitedGroups ? AtmosphereProcessingState.ExcitedGroups : AtmosphereProcessingState.HighPressureDelta;
                        continue;
                    case AtmosphereProcessingState.ExcitedGroups:
                        if (!ProcessExcitedGroups(atmosphere))
                        {
                            atmosphere.ProcessingPaused = true;
                            return;
                        }

                        atmosphere.ProcessingPaused = false;
                        atmosphere.State = AtmosphereProcessingState.HighPressureDelta;
                        continue;
                    case AtmosphereProcessingState.HighPressureDelta:
                        if (!ProcessHighPressureDelta(ent))
                        {
                            atmosphere.ProcessingPaused = true;
                            return;
                        }

                        atmosphere.ProcessingPaused = false;
                        atmosphere.State = AtmosphereProcessingState.Hotspots;
                        continue;
                    case AtmosphereProcessingState.Hotspots:
                        if (!ProcessHotspots(atmosphere))
                        {
                            atmosphere.ProcessingPaused = true;
                            return;
                        }

                        atmosphere.ProcessingPaused = false;
                        // Next state depends on whether superconduction is enabled or not.
                        // Note: We do this here instead of on the tile equalization step to prevent ending it early.
                        //       Therefore, a change to this CVar might only be applied after that step is over.
                        atmosphere.State = Superconduction
                            ? AtmosphereProcessingState.Superconductivity
                            : AtmosphereProcessingState.PipeNet;
                        continue;
                    case AtmosphereProcessingState.Superconductivity:
                        if (!ProcessSuperconductivity(atmosphere))
                        {
                            atmosphere.ProcessingPaused = true;
                            return;
                        }

                        atmosphere.ProcessingPaused = false;
                        atmosphere.State = AtmosphereProcessingState.PipeNet;
                        continue;
                    case AtmosphereProcessingState.PipeNet:
                        if (!ProcessPipeNets(atmosphere))
                        {
                            atmosphere.ProcessingPaused = true;
                            return;
                        }

                        atmosphere.ProcessingPaused = false;
                        atmosphere.State = AtmosphereProcessingState.AtmosDevices;
                        continue;
                    case AtmosphereProcessingState.AtmosDevices:
                        if (!ProcessAtmosDevices(atmosphere))
                        {
                            atmosphere.ProcessingPaused = true;
                            return;
                        }

                        atmosphere.ProcessingPaused = false;
                        atmosphere.State = AtmosphereProcessingState.Revalidate;

                        // We reached the end of this atmosphere's update tick. Break out of the switch.
                        break;
                }

                // And increase the update counter.
                atmosphere.UpdateCounter++;
            }

            // We finished processing all atmospheres successfully, therefore we won't be paused next tick.
            _simulationPaused = false;
        }
    }

    public enum AtmosphereProcessingState : byte
    {
        Revalidate,
        TileEqualize,
        ActiveTiles,
        ExcitedGroups,
        HighPressureDelta,
        Hotspots,
        Superconductivity,
        PipeNet,
        AtmosDevices,
        NumStates
    }
}
