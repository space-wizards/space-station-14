using System;
using System.Collections.Generic;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.EntitySystems
{
    public partial class AtmosphereSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private readonly AtmosDeviceUpdateEvent _updateEvent = new();
        private readonly Stopwatch _simulationStopwatch = new();

        /// <summary>
        ///     Check current execution time every n instances processed.
        /// </summary>
        private const int LagCheckIterations = 30;

        /// <summary>
        ///     Check current execution time every n instances processed.
        /// </summary>
        private const int InvalidCoordinatesLagCheckIterations = 50;

        private int _currentRunAtmosphereIndex = 0;
        private bool _simulationPaused = false;

        private readonly List<GridAtmosphereComponent> _currentRunAtmosphere = new();

        private bool ProcessTileEqualize(GridAtmosphereComponent atmosphere)
        {
            if(!atmosphere.ProcessingPaused)
                atmosphere.CurrentRunTiles = new Queue<TileAtmosphere>(atmosphere.ActiveTiles);

            if (!TryGetMapGrid(atmosphere, out var mapGrid))
                throw new Exception("Tried to process a grid atmosphere on an entity that isn't a grid!");

            var number = 0;
            while (atmosphere.CurrentRunTiles.TryDequeue(out var tile))
            {
                EqualizePressureInZone(mapGrid, atmosphere, tile, atmosphere.UpdateCounter);

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ProcessActiveTiles(GridAtmosphereComponent atmosphere)
        {
            if(!atmosphere.ProcessingPaused)
                atmosphere.CurrentRunTiles = new Queue<TileAtmosphere>(atmosphere.ActiveTiles);

            var number = 0;
            while (atmosphere.CurrentRunTiles.TryDequeue(out var tile))
            {
                ProcessCell(atmosphere, tile, atmosphere.UpdateCounter);

                if (number++ < LagCheckIterations) continue;
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
            if(!gridAtmosphere.ProcessingPaused)
                gridAtmosphere.CurrentRunExcitedGroups = new Queue<ExcitedGroup>(gridAtmosphere.ExcitedGroups);

            var number = 0;
            while (gridAtmosphere.CurrentRunExcitedGroups.TryDequeue(out var excitedGroup))
            {
                excitedGroup.BreakdownCooldown++;
                excitedGroup.DismantleCooldown++;

                if(excitedGroup.BreakdownCooldown > Atmospherics.ExcitedGroupBreakdownCycles)
                    ExcitedGroupSelfBreakdown(gridAtmosphere, excitedGroup);

                else if(excitedGroup.DismantleCooldown > Atmospherics.ExcitedGroupsDismantleCycles)
                    ExcitedGroupDismantle(gridAtmosphere, excitedGroup);

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ProcessHighPressureDelta(GridAtmosphereComponent atmosphere)
        {
            if(!atmosphere.ProcessingPaused)
                atmosphere.CurrentRunTiles = new Queue<TileAtmosphere>(atmosphere.HighPressureDelta);

            var number = 0;
            while (atmosphere.CurrentRunTiles.TryDequeue(out var tile))
            {
                HighPressureMovements(atmosphere, tile);
                tile.PressureDifference = 0f;
                tile.PressureSpecificTarget = null;
                atmosphere.HighPressureDelta.Remove(tile);

                if (number++ < LagCheckIterations) continue;
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
                atmosphere.CurrentRunTiles = new Queue<TileAtmosphere>(atmosphere.HotspotTiles);

            var number = 0;
            while (atmosphere.CurrentRunTiles.TryDequeue(out var hotspot))
            {
                ProcessHotspot(atmosphere, hotspot);

                if (number++ < LagCheckIterations) continue;
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
                atmosphere.CurrentRunTiles = new Queue<TileAtmosphere>(atmosphere.SuperconductivityTiles);

            var number = 0;
            while (atmosphere.CurrentRunTiles.TryDequeue(out var superconductivity))
            {
                Superconduct(atmosphere, superconductivity);

                if (number++ < LagCheckIterations) continue;
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
            if(!atmosphere.ProcessingPaused)
                atmosphere.CurrentRunPipeNet = new Queue<IPipeNet>(atmosphere.PipeNets);

            var number = 0;
            while (atmosphere.CurrentRunPipeNet.TryDequeue(out var pipenet))
            {
                pipenet.Update();

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ProcessAtmosDevices(GridAtmosphereComponent atmosphere)
        {
            if(!atmosphere.ProcessingPaused)
                atmosphere.CurrentRunAtmosDevices = new Queue<AtmosDeviceComponent>(atmosphere.AtmosDevices);

            var time = _gameTiming.CurTime;
            var number = 0;
            while (atmosphere.CurrentRunAtmosDevices.TryDequeue(out var device))
            {
                RaiseLocalEvent(device.Owner.Uid, _updateEvent, false);
                device.LastProcess = time;

                if (number++ < LagCheckIterations) continue;
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
                _currentRunAtmosphere.AddRange(EntityManager.EntityQuery<GridAtmosphereComponent>());
            }

            // We set this to true just in case we have to stop processing due to time constraints.
            _simulationPaused = true;

            for (; _currentRunAtmosphereIndex < _currentRunAtmosphere.Count; _currentRunAtmosphereIndex++)
            {
                var atmosphere = _currentRunAtmosphere[_currentRunAtmosphereIndex];

                if (atmosphere.Paused || !atmosphere.Simulated || atmosphere.LifeStage >= ComponentLifeStage.Stopping)
                    continue;

                atmosphere.Timer += frameTime;

                if ((atmosphere.InvalidatedCoords.Count != 0 || atmosphere.RevalidatePaused) && TryGetMapGrid(atmosphere, out var mapGrid))
                    if (!GridRevalidate(mapGrid, atmosphere))
                    {
                        atmosphere.RevalidatePaused = true;
                        return;
                    }

                atmosphere.RevalidatePaused = false;

                if (atmosphere.Timer < AtmosTime)
                    continue;

                // We subtract it so it takes lost time into account.
                atmosphere.Timer -= AtmosTime;

                switch (atmosphere.State)
                {
                    case AtmosphereProcessingState.TileEqualize:
                        if (!ProcessTileEqualize(atmosphere))
                        {
                            atmosphere.ProcessingPaused = true;
                            return;
                        }

                        atmosphere.ProcessingPaused = false;
                        atmosphere.State = AtmosphereProcessingState.ActiveTiles;
                        continue;
                    case AtmosphereProcessingState.ActiveTiles:
                        if (!ProcessActiveTiles(atmosphere))
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
                        if (!ProcessHighPressureDelta(atmosphere))
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
                        // Next state depends on whether monstermos equalization is enabled or not.
                        // Note: We do this here instead of on the tile equalization step to prevent ending it early.
                        //       Therefore, a change to this CVar might only be applied after that step is over.
                        atmosphere.State = MonstermosEqualization
                            ? AtmosphereProcessingState.TileEqualize
                            : AtmosphereProcessingState.ActiveTiles;

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
        TileEqualize,
        ActiveTiles,
        ExcitedGroups,
        HighPressureDelta,
        Hotspots,
        Superconductivity,
        PipeNet,
        AtmosDevices,
    }
}
