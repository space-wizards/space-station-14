using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.NodeContainer.NodeGroups;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems;

// PhaseRunner implementations for each AtmosphereProcessingState entry in PhaseTable.
public sealed partial class AtmosphereSystem
{
    [Dependency] private IGameTiming _gameTiming = default!;

    /// <summary>
    ///     Revalidates all invalid coordinates in a grid atmosphere.
    ///     I.e., process any tiles that have had their airtight blockers modified.
    /// </summary>
    /// <param name="ent">The grid atmosphere in question.</param>
    /// <returns>Whether the process succeeded or got paused due to time constraints.</returns>
    /// <remarks>Must stay before the tile simulation phases, which read what it refreshes.</remarks>
    private bool ProcessRevalidate(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
    {
        if (ent.Comp4.MapUid == null)
        {
            Log.Error($"Attempted to process atmosphere on a map-less grid? Grid: {ToPrettyString(ent)}");
            return true;
        }

        var (uid, atmosphere, _, grid, xform) = ent;

        var runtime = atmosphere.Processing;

        if (!runtime.ProcessingPaused
            && atmosphere.InvalidatedCoords.Count == 0
            && atmosphere.PossiblyDisconnectedTiles.Count == 0)
            return true;

        var volume = GetVolumeForTiles(grid);
        TryComp(xform.MapUid, out MapAtmosphereComponent? mapAtmos);

        if (!runtime.ProcessingPaused)
        {
            runtime.CurrentRunInvalidatedTiles.Clear();
            runtime.CurrentRunInvalidatedTiles.EnsureCapacity(atmosphere.InvalidatedCoords.Count);
            foreach (var indices in atmosphere.InvalidatedCoords)
            {
                var tile = GetOrNewTile((uid, atmosphere), indices, invalidateNew: false);
                runtime.CurrentRunInvalidatedTiles.Enqueue(tile);
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

        // PossiblyDisconnectedTiles entries are queued during tile-data refresh above and may
        // sit until the drain finishes. MapTiles can lag by however long the drain takes.
        TrimDisconnectedMapTiles(ent);
        return true;
    }

    /// <summary>
    /// Revalidates one tile's adjacency, air mixture, and visuals. Airtight, space, and
    /// map-atmosphere data are refreshed in the enqueue pass before this worker runs.
    /// </summary>
    private static readonly QueueWorker<
        (Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> Ent, float Volume),
        TileAtmosphere> RevalidateTileWorker =
        static (self, ctx, tile) =>
        {
            DebugTools.Assert(ctx.Ent.Comp1.Tiles.GetValueOrDefault(tile.GridIndices) == tile);
            self.UpdateAdjacentTiles(ctx.Ent, tile, activate: true);
            self.UpdateTileAir(ctx.Ent, tile, ctx.Volume);
            self.InvalidateVisuals(ctx.Ent, tile);
        };

    /// <summary>
    /// Runs Monstermos equalization for one tile from the current phase snapshot.
    /// </summary>
    private static readonly TileWorker<
        (Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> Ent,
        int UpdateCounter)> EqualizeTileWorker =
        static (self, ctx, tile) => self.EqualizePressureInZone(ctx.Ent, tile, ctx.UpdateCounter);

    /// <summary>
    /// Runs Monstermos equalization over the active tiles, pausing if we run out of time.
    /// </summary>
    /// <param name="ent">The grid atmosphere whose active tiles we're equalizing.</param>
    /// <returns>True if the phase finished this run; false if it paused on the time budget.</returns>
    private bool ProcessTileEqualize(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
        => DrainTilesBatched(
            ent.Comp1,
            ent.Comp1.Processing.EqualizeRun,
            ent.Comp1.ActiveTiles,
            EqualizeTileWorker,
            (ent, ent.Comp1.UpdateCounter));

    /// <summary>
    /// Runs LINDA cell sharing and reactions for one active tile from the current phase snapshot.
    /// </summary>
    private static readonly TileWorker<
        (Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> Ent,
        int UpdateCounter)> ActiveTileWorker =
        static (self, ctx, tile) => self.ProcessCell(ctx.Ent, tile, ctx.UpdateCounter);

    /// <summary>
    /// Runs LINDA cell sharing and reactions over the active tiles, pausing if we run out of time.
    /// </summary>
    /// <param name="ent">The grid atmosphere whose active tiles we're sharing.</param>
    /// <returns>True if the phase finished this run; false if it paused on the time budget.</returns>
    private bool ProcessActiveTiles(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
        => DrainTilesBatched(
            ent.Comp1,
            ent.Comp1.Processing.ActiveTilesRun,
            ent.Comp1.ActiveTiles,
            ActiveTileWorker,
            (ent, ent.Comp1.UpdateCounter));

    /// <summary>
    /// Advances one excited group's cooldown state from the current phase queue.
    /// </summary>
    private static readonly QueueWorker<
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>,
        ExcitedGroup> ExcitedGroupWorker =
        static (self, ent, excitedGroup) =>
        {
            excitedGroup.BreakdownCooldown++;
            excitedGroup.DismantleCooldown++;

            if (excitedGroup.BreakdownCooldown > Atmospherics.ExcitedGroupBreakdownCycles)
                self.ExcitedGroupSelfBreakdown(ent, excitedGroup);
            else if (excitedGroup.DismantleCooldown > Atmospherics.ExcitedGroupsDismantleCycles)
                self.DeactivateGroupTiles(ent.Comp1, excitedGroup);
        };

    /// <summary>
    /// Advances every excited group's cooldown, breaking down or dismantling the stale ones,
    /// pausing if we run out of time.
    /// </summary>
    /// <param name="ent">The grid atmosphere whose excited groups we're aging.</param>
    /// <returns>True if the phase finished this run; false if it paused on the time budget.</returns>
    private bool ProcessExcitedGroups(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
        => DrainQueueBatched(
            ent.Comp1,
            ent.Comp1.ExcitedGroups,
            ent.Comp1.Processing.CurrentRunExcitedGroups,
            ExcitedGroupWorker,
            ent);

    /// <summary>
    /// Handles one high-pressure delta tile and clears its per-cycle pressure movement state.
    /// </summary>
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

    /// <summary>
    /// Processes the high-pressure delta tiles and clears their per-cycle pressure state,
    /// pausing if we run out of time.
    /// </summary>
    /// <remarks>Still runs when space wind is off, since this also plays the pressure sounds.</remarks>
    /// <param name="ent">The grid atmosphere whose high-pressure deltas we're resolving.</param>
    /// <returns>True if the phase finished this run; false if it paused on the time budget.</returns>
    private bool ProcessHighPressureDelta(Entity<GridAtmosphereComponent> ent)
        => DrainTilesBatched(
            ent.Comp,
            ent.Comp.Processing.HighPressureDeltaRun,
            ent.Comp.HighPressureDelta,
            HighPressureDeltaTileWorker,
            ent);

    /// <summary>
    /// Processes one hotspot tile from the current phase snapshot.
    /// </summary>
    private static readonly TileWorker<
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>> HotspotTileWorker =
        static (self, ent, tile) => self.ProcessHotspot(ent, tile);

    /// <summary>
    /// Runs fire processing over the hotspot tiles, pausing if we run out of time.
    /// </summary>
    /// <param name="ent">The grid atmosphere whose hotspots we're burning.</param>
    /// <returns>True if the phase finished this run; false if it paused on the time budget.</returns>
    private bool ProcessHotspots(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
        => DrainTilesBatched(
            ent.Comp1,
            ent.Comp1.Processing.HotspotRun,
            ent.Comp1.HotspotTiles,
            HotspotTileWorker,
            ent);

    /// <summary>
    /// Applies superconductivity to one tile from the current phase snapshot.
    /// </summary>
    private static readonly TileWorker<GridAtmosphereComponent> SuperconductTileWorker =
        static (self, atmos, tile) => self.Superconduct(atmos, tile);

    /// <summary>
    /// Runs heat transfer over the superconductivity tiles, pausing if we run out of time.
    /// </summary>
    /// <param name="atmosphere">The grid atmosphere whose superconducting tiles we're conducting heat through.</param>
    /// <returns>True if the phase finished this run; false if it paused on the time budget.</returns>
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
    /// Uses a bespoke loop rather than the drain helpers because results come back on a
    /// <see cref="System.Collections.Concurrent.ConcurrentQueue{T}"/>.
    /// </summary>
    /// <param name="ent">The grid atmosphere whose delta-pressure entities we're damaging.</param>
    /// <returns>True if we've finished processing all entities that required processing this run,
    /// otherwise, false.</returns>
    private bool ProcessDeltaPressure(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
    {
        var atmosphere = ent.Comp1;
        var runtime = atmosphere.Processing;
        if (!runtime.ProcessingPaused)
        {
            runtime.DeltaPressureCursor = 0;
            runtime.DeltaPressureSnapshot.Clear();
            runtime.DeltaPressureSnapshot.AddRange(atmosphere.DeltaPressureEntities);
            runtime.DeltaPressureDamageResults.Clear();
            _deltaPressureInvalidEntityQueue.Clear();
        }

        var count = runtime.DeltaPressureSnapshot.Count;
        var timeCheck1 = 0;
        while (runtime.DeltaPressureCursor < count)
        {
            var remaining = count - runtime.DeltaPressureCursor;
            var toProcess = Math.Min(DeltaPressureParallelProcessPerIteration, remaining);

            var job = new DeltaPressureParallelBulkJob(this,
                atmosphere,
                runtime.DeltaPressureSnapshot,
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

    /// <summary>
    /// Updates one pipe net from the current phase queue.
    /// </summary>
    private static readonly QueueWorker<GridAtmosphereComponent, IPipeNet> PipeNetWorker =
        static (_, _, pipenet) => pipenet.Update();

    /// <summary>
    /// Updates every pipe net once for this cycle, pausing if we run out of time.
    /// </summary>
    /// <param name="atmosphere">The grid atmosphere whose pipe nets we're updating.</param>
    /// <returns>True if the phase finished this run; false if it paused on the time budget.</returns>
    private bool ProcessPipeNets(GridAtmosphereComponent atmosphere)
        => DrainQueueBatched(
            atmosphere,
            atmosphere.PipeNets,
            atmosphere.Processing.CurrentRunPipeNet,
            PipeNetWorker,
            atmosphere);

    /// <summary>
    /// Raises one atmos device update from the current phase queue.
    /// </summary>
    private static readonly QueueWorker<
        (AtmosDeviceUpdateEvent Ev, TimeSpan Time),
        Entity<AtmosDeviceComponent>> AtmosDeviceWorker =
        static (self, ctx, device) =>
        {
            var ev = ctx.Ev;
            self.RaiseLocalEvent(device, ref ev);
            device.Comp.LastProcess = ctx.Time;
        };

    /// <summary>
    /// Updates every atmos device once with a stable dt snapshot, pausing if we run out of time.
    /// dt is captured at phase entry and held stable across pause/resume so all devices in a cycle see the same value.
    /// </summary>
    /// <remarks>Runs after the gas and pipe work so devices see the finished cycle state.</remarks>
    /// <param name="ent">The grid atmosphere whose devices we're updating.</param>
    /// <param name="map">The map atmosphere the grid sits on; the device event carries this so devices can see it.</param>
    /// <returns>True if the phase finished this run; false if it paused on the time budget.</returns>
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
