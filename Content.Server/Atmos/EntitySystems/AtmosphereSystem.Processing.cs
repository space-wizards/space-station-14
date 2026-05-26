using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.NodeContainer.NodeGroups;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        /// <summary>
        ///     Revalidates all invalid coordinates in a grid atmosphere.
        ///     I.e., process any tiles that have had their airtight blockers modified.
        /// </summary>
        /// <param name="ent">The grid atmosphere in question.</param>
        /// <returns>Whether the process succeeded or got paused due to time constrains.</returns>
        private bool ProcessRevalidate(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
        {
            if (ent.Comp4.MapUid == null)
            {
                Log.Error($"Attempted to process atmosphere on a map-less grid? Grid: {ToPrettyString(ent)}");
                return true;
            }

            var (uid, atmosphere, visuals, grid, xform) = ent;

            var runtime = atmosphere.Processing;

            if (!runtime.ProcessingPaused
                && atmosphere.InvalidatedCoords.Count == 0
                && atmosphere.PossiblyDisconnectedTiles.Count == 0)
            {
                return true;
            }

            var volume = GetVolumeForTiles(grid);
            TryComp(xform.MapUid, out MapAtmosphereComponent? mapAtmos);

            if (!runtime.ProcessingPaused)
            {
                runtime.CurrentRunInvalidatedTiles.Clear();
                runtime.CurrentRunInvalidatedTiles.EnsureCapacity(atmosphere.InvalidatedCoords.Count);
                foreach (var indices in atmosphere.InvalidatedCoords)
                {
                    var tile = GetOrNewTile(uid, atmosphere, indices, invalidateNew: false);
                    runtime.CurrentRunInvalidatedTiles.Enqueue(tile);

                    // Update tile.IsSpace and tile.MapAtmosphere, and tile.AirtightData.
                    UpdateTileData(ent, mapAtmos, tile);
                }
                atmosphere.InvalidatedCoords.Clear();

                if (BudgetExhausted)
                    return false;
            }

            if (!DrainScratch(
                    runtime.CurrentRunInvalidatedTiles,
                    RevalidateTileWorker,
                    (ent, volume),
                    InvalidCoordinatesLagCheckIterations))
            {
                return false;
            }

            TrimDisconnectedMapTiles(ent);
            return true;
        }

        private static readonly QueueWorker<(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> Ent, float Volume), TileAtmosphere> RevalidateTileWorker =
            static (self, ctx, tile) =>
            {
                DebugTools.Assert(ctx.Ent.Comp1.Tiles.GetValueOrDefault(tile.GridIndices) == tile);
                self.UpdateAdjacentTiles(ctx.Ent, tile, activate: true);
                self.UpdateTileAir(ctx.Ent, tile, ctx.Volume);
                self.InvalidateVisuals(ctx.Ent, tile);
            };

        private static readonly TileWorker<(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> Ent, int UpdateCounter)> EqualizeTileWorker =
            static (self, ctx, tile) => self.EqualizePressureInZone(ctx.Ent, tile, ctx.UpdateCounter);

        private bool ProcessTileEqualize(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
            => DrainTilesBatched(
                ent.Comp1,
                ent.Comp1.Processing.EqualizeRun,
                ent.Comp1.ActiveTiles,
                EqualizeTileWorker,
                (ent, ent.Comp1.UpdateCounter));

        private static readonly TileWorker<(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> Ent, int UpdateCounter)> ActiveTileWorker =
            static (self, ctx, tile) => self.ProcessCell(ctx.Ent, tile, ctx.UpdateCounter);

        private bool ProcessActiveTiles(
            Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
            => DrainTilesBatched(
                ent.Comp1,
                ent.Comp1.Processing.ActiveTilesRun,
                ent.Comp1.ActiveTiles,
                ActiveTileWorker,
                (ent, ent.Comp1.UpdateCounter));

        private static readonly QueueWorker<Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>, ExcitedGroup> ExcitedGroupWorker =
            static (self, ent, excitedGroup) =>
            {
                excitedGroup.BreakdownCooldown++;
                excitedGroup.DismantleCooldown++;

                if (excitedGroup.BreakdownCooldown > Atmospherics.ExcitedGroupBreakdownCycles)
                    self.ExcitedGroupSelfBreakdown(ent, excitedGroup);
                else if (excitedGroup.DismantleCooldown > Atmospherics.ExcitedGroupsDismantleCycles)
                    self.DeactivateGroupTiles(ent.Comp1, excitedGroup);
            };

        private bool ProcessExcitedGroups(
            Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
            => DrainQueueBatched(
                ent.Comp1,
                ent.Comp1.ExcitedGroups,
                ent.Comp1.Processing.CurrentRunExcitedGroups,
                ExcitedGroupWorker,
                ent);

        private static readonly TileWorker<Entity<GridAtmosphereComponent>> HighPressureDeltaTileWorker =
            static (self, ent, tile) =>
            {
                self.HighPressureMovements(ent, tile);
                tile.PressureDifference = 0f;
                tile.LastPressureDirection = tile.PressureDirection;
                tile.PressureDirection = AtmosDirection.Invalid;
                tile.PressureSpecificTarget = null;
                ent.Comp.HighPressureDelta.Remove(tile);
            };

        // Note: HighPressureDelta is still processed even if space wind is turned off since this handles playing the sounds.
        private bool ProcessHighPressureDelta(Entity<GridAtmosphereComponent> ent)
            => DrainTilesBatched(
                ent.Comp,
                ent.Comp.Processing.HighPressureDeltaRun,
                ent.Comp.HighPressureDelta,
                HighPressureDeltaTileWorker,
                ent);

        private static readonly TileWorker<Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>> HotspotTileWorker =
            static (self, ent, tile) => self.ProcessHotspot(ent, tile);

        private bool ProcessHotspots(
            Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
            => DrainTilesBatched(
                ent.Comp1,
                ent.Comp1.Processing.HotspotRun,
                ent.Comp1.HotspotTiles,
                HotspotTileWorker,
                ent);

        private static readonly TileWorker<GridAtmosphereComponent> SuperconductTileWorker =
            static (self, atmos, tile) => self.Superconduct(atmos, tile);

        private bool ProcessSuperconductivity(GridAtmosphereComponent atmosphere)
            => DrainTilesBatched(
                atmosphere,
                atmosphere.Processing.SuperconductRun,
                atmosphere.SuperconductivityTiles,
                SuperconductTileWorker,
                atmosphere);

        /// <summary>
        /// Processes all entities with a <see cref="DeltaPressureComponent"/>, doing damage to them
        /// depending on certain pressure differential conditions.
        /// </summary>
        /// <returns>True if we've finished processing all entities that required processing this run,
        /// otherwise, false.</returns>
        private bool ProcessDeltaPressure(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
        {
            var atmosphere = ent.Comp1;
            var runtime = atmosphere.Processing;
            var count = atmosphere.DeltaPressureEntities.Count;
            if (!runtime.ProcessingPaused)
            {
                runtime.DeltaPressureCursor = 0;
                runtime.DeltaPressureDamageResults.Clear();
                _deltaPressureInvalidEntityQueue.Clear();
            }

            var timeCheck1 = 0;
            while (runtime.DeltaPressureCursor < count)
            {
                var remaining = count - runtime.DeltaPressureCursor;
                var toProcess = Math.Min(DeltaPressureParallelProcessPerIteration, remaining);

                var job = new DeltaPressureParallelBulkJob(this,
                    atmosphere,
                    runtime.DeltaPressureCursor,
                    DeltaPressureParallelBatchSize);
                _parallel.ProcessNow(job, toProcess);

                runtime.DeltaPressureCursor += toProcess;

                if (OverBudget(ref timeCheck1))
                    return false;
            }

            // ConcurrentQueue, not the Queue<T> the drain helpers take, so the loop stays bespoke; the budget gate is shared.
            var timeCheck2 = 0;
            while (runtime.DeltaPressureDamageResults.TryDequeue(out var result))
            {
                PerformDamage(result.Ent,
                    result.Pressure,
                    result.DeltaPressure);

                if (OverBudget(ref timeCheck2))
                    return false;
            }

            // Ents may have been invalidated (missing AirtightComp) during parallel processing.
            // Since we can't touch the ent list during parallel processing, we queue them up here to be removed.
            while (_deltaPressureInvalidEntityQueue.TryDequeue(out var invalidEnt))
            {
                TryRemoveDeltaPressureEntity(ent.AsNullable(), invalidEnt);
            }

            return true;
        }

        private static readonly QueueWorker<GridAtmosphereComponent, IPipeNet> PipeNetWorker =
            static (_, _, pipenet) => pipenet.Update();

        private bool ProcessPipeNets(GridAtmosphereComponent atmosphere)
            => DrainQueueBatched(
                atmosphere,
                atmosphere.PipeNets,
                atmosphere.Processing.CurrentRunPipeNet,
                PipeNetWorker,
                atmosphere);

        private static readonly QueueWorker<(AtmosDeviceUpdateEvent Ev, TimeSpan Time), Entity<AtmosDeviceComponent>> AtmosDeviceWorker =
            static (self, ctx, device) =>
            {
                var ev = ctx.Ev;
                self.RaiseLocalEvent(device, ref ev);
                device.Comp.LastProcess = ctx.Time;
            };

        private bool ProcessAtmosDevices(
            Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
            Entity<MapAtmosphereComponent?> map)
        {
            var atmosphere = ent.Comp1;
            var runtime = atmosphere.Processing;
            if (!runtime.ProcessingPaused)
            {
                // Snapshot dt even when no devices, so the next drain's dt stays accurate.
                runtime.CurrentRunDeviceDt = runtime.TimeSinceLastDeviceUpdate;
                runtime.TimeSinceLastDeviceUpdate = 0f;
            }

            var ev = new AtmosDeviceUpdateEvent(runtime.CurrentRunDeviceDt, (ent, ent.Comp1, ent.Comp2), map);
            return DrainQueueBatched(
                atmosphere,
                atmosphere.AtmosDevices,
                runtime.CurrentRunAtmosDevices,
                AtmosDeviceWorker,
                (ev, _gameTiming.CurTime));
        }
    }
}
