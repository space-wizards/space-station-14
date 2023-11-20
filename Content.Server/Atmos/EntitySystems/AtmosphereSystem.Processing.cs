using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Maps;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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

        private TileAtmosphere GetOrNewTile(EntityUid owner, GridAtmosphereComponent atmosphere, Vector2i index, float volume)
        {
            var tile = atmosphere.Tiles.GetOrNew(index, out var existing);
            if (!existing)
            {
                tile.GridIndex = owner;
                tile.GridIndices = index;
                tile.Air = new GasMixture(volume) { Temperature = Atmospherics.T20C };
            }

            return tile;
        }

        /// <summary>
        ///     Revalidates all invalid coordinates in a grid atmosphere.
        /// </summary>
        /// <param name="ent">The grid atmosphere in question.</param>
        /// <returns>Whether the process succeeded or got paused due to time constrains.</returns>
        private bool ProcessRevalidate(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
        {
            var (uid, atmosphere, visuals, grid, _) = ent;
            var volume = GetVolumeForTiles(grid);

            if (!atmosphere.ProcessingPaused)
            {
                atmosphere.CurrentRunInvalidatedTiles.Clear();
                atmosphere.CurrentRunInvalidatedTiles.EnsureCapacity(atmosphere.InvalidatedCoords.Count);
                foreach (var indices in atmosphere.InvalidatedCoords)
                {
                    var tile = GetOrNewTile(uid, atmosphere, indices, volume);
                    tile.AirtightDirty = true;
                    atmosphere.CurrentRunInvalidatedTiles.Enqueue(tile);
                }
                atmosphere.InvalidatedCoords.Clear();
            }

            var number = 0;
            while (atmosphere.CurrentRunInvalidatedTiles.TryDequeue(out var tile))
            {
                var indices = tile.GridIndices;
                DebugTools.Assert(atmosphere.Tiles.GetValueOrDefault(indices) == tile);

                DebugTools.Assert(tile.AirtightDirty);
                GridUpdateAdjacent((ent.Owner, ent.Comp1, ent.Comp3, ent.Comp4), tile);

                UpdateTile(ent, tile, volume);

                // We activate the tile.
                AddActiveTile(atmosphere, tile);

                InvalidateVisuals(uid, indices, visuals);

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

        private void UpdateTile(
            Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
            TileAtmosphere tile,
            float volume)
        {
            var idx = tile.GridIndices;

            DebugTools.Assert(!tile.AirtightDirty);
            var data = tile.AirtightData;
            var fullyBlocked = data.BlockedDirections == AtmosDirection.All;

            bool isSpace;
            if (_map.TryGetTileDef(ent.Owner, ent.Comp3, idx, out var tileDef))
            {
                var contentDef = (ContentTileDefinition) tileDef;
                isSpace = contentDef.IsSpace;
                tile.ThermalConductivity = contentDef.ThermalConductivity;
                tile.HeatCapacity = contentDef.HeatCapacity;
            }
            else
            {
                isSpace = true;
                tile.ThermalConductivity =  0.5f;
                tile.HeatCapacity = float.PositiveInfinity;
            }

            if (fullyBlocked && data.NoAirWhenBlocked)
            {
                tile.Air = null;
                Array.Clear(tile.MolesArchived);
                tile.ArchivedCycle = 0;
                tile.LastShare = 0f;
                tile.Hotspot = new Hotspot();
                return;
            }

            if (isSpace)
            {
                var mapUid = ent.Comp4.MapUid;
                tile.Air = GetTileMixture(null, mapUid, idx);
                tile.Space = IsTileSpace(null, mapUid, idx, ent.Comp3);
                return;
            }

            if (tile.Air == null && data.FixVacuum)
            {
                var vacuumEv = new FixTileVacuumMethodEvent(ent.Owner, idx);
                GridFixTileVacuum(ent.Owner, ent.Comp1, ref vacuumEv);
            }

            // Tile used to be space, but isn't anymore.
            if (tile.Air?.Immutable == true)
            {
                tile.Air = null;
                Array.Clear(tile.MolesArchived);
                tile.ArchivedCycle = 0;
                tile.LastShare = 0f;
                tile.Space = false;
            }

            tile.Air ??= new GasMixture(volume){Temperature = Atmospherics.T20C};
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
                        if (!ProcessRevalidate((ent, ent, visuals!, Comp<MapGridComponent>(ent), Transform(ent))))
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
