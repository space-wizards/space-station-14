using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        private readonly Stopwatch _simulationStopwatch = new();

        // How many items a phase drains between budget checks. Reading the Stopwatch in OverBudget
        // is not free, pay for it over batches.
        // Revalidation gets its own batch size; both are tuning constants.
        private const int LagCheckIterations = 30;
        private const int InvalidCoordinatesLagCheckIterations = 50;

        private int _currentRunAtmosphereIndex;
        private bool _simulationPaused;

        private readonly List<Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>> _currentRunAtmosphere = new();

        #region Phase table

        private delegate bool PhaseEnabled(AtmosphereSystem self);

        private delegate bool PhaseRunner(
            AtmosphereSystem self,
            Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
            Entity<MapAtmosphereComponent?> map);

        // Gate == None means the phase always runs.
        private readonly record struct PhaseDescriptor(
            AtmosphereProcessingState Phase,
            AtmosPhases Gate,
            PhaseEnabled Enabled,
            PhaseRunner Runner);

        // Indexed by (int)AtmosphereProcessingState; BuildPhaseTable asserts row-vs-enum alignment at startup.
        private static readonly PhaseDescriptor[] PhaseTable = BuildPhaseTable();

        private static PhaseDescriptor[] BuildPhaseTable()
        {
            var table = new PhaseDescriptor[]
            {
                new(
                    AtmosphereProcessingState.Revalidate,
                    AtmosPhases.None,
                    static _ => true,
                    static (s, e, _) => s.ProcessRevalidate(e)),
                new(
                    AtmosphereProcessingState.TileEqualize,
                    AtmosPhases.MonstermosEqualization,
                    static s => s.MonstermosEqualization,
                    static (s, e, _) => s.ProcessTileEqualize(e)),
                new(
                    AtmosphereProcessingState.ActiveTiles,
                    AtmosPhases.None,
                    static _ => true,
                    static (s, e, _) => s.ProcessActiveTiles(e)),
                new(
                    AtmosphereProcessingState.ExcitedGroups,
                    AtmosPhases.ExcitedGroups,
                    static s => s.ExcitedGroups,
                    static (s, e, _) => s.ProcessExcitedGroups(e)),
                new(
                    AtmosphereProcessingState.HighPressureDelta,
                    AtmosPhases.None,
                    static _ => true,
                    static (s, e, _) => s.ProcessHighPressureDelta(e)),
                new(
                    AtmosphereProcessingState.DeltaPressure,
                    AtmosPhases.DeltaPressureDamage,
                    static s => s.DeltaPressureDamage,
                    static (s, e, _) => s.ProcessDeltaPressure(e)),
                new(
                    AtmosphereProcessingState.Hotspots,
                    AtmosPhases.None,
                    static _ => true,
                    static (s, e, _) => s.ProcessHotspots(e)),
                new(
                    AtmosphereProcessingState.Superconductivity,
                    AtmosPhases.Superconduction,
                    static s => s.Superconduction,
                    static (s, e, _) => s.ProcessSuperconductivity(e.Comp1)),
                new(
                    AtmosphereProcessingState.PipeNet,
                    AtmosPhases.None,
                    static _ => true,
                    static (s, e, _) => s.ProcessPipeNets(e.Comp1)),
                new(
                    AtmosphereProcessingState.AtmosDevices,
                    AtmosPhases.None,
                    static _ => true,
                    static (s, e, m) => s.ProcessAtmosDevices(e, m)),
            };

            DebugTools.Assert(table.Length == (int)AtmosphereProcessingState.NumStates,
                $"Phase table length {table.Length} does not match AtmosphereProcessingState.NumStates {(int)AtmosphereProcessingState.NumStates}. Add/remove a row to match.");
            for (var i = 0; i < table.Length; i++)
            {
                DebugTools.Assert(table[i].Phase == (AtmosphereProcessingState)i,
                    $"Phase table is mis-indexed at row {i}: phase={table[i].Phase}. Rows must be in enum order.");
            }
            return table;
        }

        private static AtmosphereProcessingState GetNextPhase(AtmosphereProcessingState current, AtmosPhases flags)
        {
            for (var idx = (int)current + 1; idx < (int)AtmosphereProcessingState.NumStates; idx++)
            {
                var entry = PhaseTable[idx];
                if (entry.Gate == AtmosPhases.None || (flags & entry.Gate) != 0)
                    return entry.Phase;
            }
            return AtmosphereProcessingState.Revalidate;
        }

        private bool RunPhase(
            AtmosphereProcessingState phase,
            Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
            Entity<MapAtmosphereComponent?> map)
        {
            return PhaseTable[(int)phase].Runner(this, ent, map);
        }

        #endregion

        #region Cycle lifecycle helpers

        private static AtmosphereProcessingCompletionState FinishCycle(GridAtmosphereComponent atmosphere)
        {
            // CycleCounter advances at cycle start; finishing must not advance it again.
            atmosphere.Processing.CycleCursor = null;
            atmosphere.Processing.ProcessingPaused = false;
            return AtmosphereProcessingCompletionState.Finished;
        }

        // Move skipped-grid revalidation work back to the main invalidation set before clearing scratch.
        private static void RequeuePendingRevalidation(GridAtmosphereComponent atmosphere)
        {
            var runtime = atmosphere.Processing;
            foreach (var tile in runtime.CurrentRunInvalidatedTiles)
                atmosphere.InvalidatedCoords.Add(tile.GridIndices);
            runtime.CurrentRunInvalidatedTiles.Clear();
        }

        private static AtmosphereProcessingCompletionState AbortCycle(GridAtmosphereComponent atmosphere)
        {
            atmosphere.Processing.CycleCursor = null;
            atmosphere.Processing.ProcessingPaused = false;
            return AtmosphereProcessingCompletionState.Continue;
        }

        private static AtmosphereProcessingCompletionState RecoverInvalidPhase(GridAtmosphereComponent atmosphere)
            => AbortCycle(atmosphere);

        internal static void ResetCycleScratch(GridAtmosphereComponent atmos)
        {
            // Preserve wall-time fields so dt accounting carries across resets.
            var runtime = atmos.Processing;
            runtime.CycleCursor = null;
            runtime.ProcessingPaused = false;
            runtime.EqualizeRun.Reset();
            runtime.ActiveTilesRun.Reset();
            runtime.HighPressureDeltaRun.Reset();
            runtime.HotspotRun.Reset();
            runtime.SuperconductRun.Reset();
            runtime.CurrentRunExcitedGroups.Clear();
            runtime.CurrentRunPipeNet.Clear();
            runtime.CurrentRunAtmosDevices.Clear();
            runtime.CurrentRunInvalidatedTiles.Clear();
            runtime.DeltaPressureCursor = 0;
            runtime.DeltaPressureSnapshot.Clear();
            runtime.DeltaPressureDamageResults.Clear();
        }

        private AtmosPhases SnapshotPhaseFlags()
        {
            var flags = AtmosPhases.None;
            foreach (var phase in PhaseTable)
            {
                if (phase.Gate != AtmosPhases.None && phase.Enabled(this))
                    flags |= phase.Gate;
            }
            return flags;
        }

        #endregion

        private void UpdateProcessing(float frameTime)
        {
            _simulationStopwatch.Restart();

            if (!_simulationPaused)
            {
                _currentRunAtmosphereIndex = 0;
                _currentRunAtmosphere.Clear();

                var query = EntityQueryEnumerator<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>();
                while (query.MoveNext(out var uid, out var atmos, out var overlay, out var grid, out var xform))
                {
                    _currentRunAtmosphere.Add((uid, atmos, overlay, grid, xform));
                }
            }

            // Pessimistic default: cleared at the bottom if every grid finished this tick.
            _simulationPaused = true;

            for (; _currentRunAtmosphereIndex < _currentRunAtmosphere.Count; _currentRunAtmosphereIndex++)
            {
                var ent = _currentRunAtmosphere[_currentRunAtmosphereIndex];
                var (owner, atmosphere, _, _, xform) = ent;

                if (xform.MapUid == null
                    || TerminatingOrDeleted(xform.MapUid.Value)
                    || xform.MapID == MapId.Nullspace)
                {
                    Log.Error($"Attempted to process atmos without a map? Entity: {ToPrettyString(owner)}. Map: {ToPrettyString(xform?.MapUid)}. MapId: {xform?.MapID}");
                    continue;
                }

                if (atmosphere.LifeStage >= ComponentLifeStage.Stopping || Paused(owner) || !atmosphere.Simulated)
                {
                    if (atmosphere.Processing.CycleCursor is not null)
                    {
                        RequeuePendingRevalidation(atmosphere);
                        ResetCycleScratch(atmosphere);
                    }
                    continue;
                }

                var map = new Entity<MapAtmosphereComponent?>(xform.MapUid.Value, _mapAtmosQuery.CompOrNull(xform.MapUid.Value));

                if (ProcessAtmosphere(ent, map, frameTime) == AtmosphereProcessingCompletionState.Return)
                    return;
            }

            _simulationPaused = false;
        }

        private AtmosphereProcessingCompletionState ProcessAtmosphere(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
            Entity<MapAtmosphereComponent?> mapAtmosphere,
            float frameTime)
        {
            var owner = ent.Owner;
            var atmosphere = ent.Comp1;

            var runtime = atmosphere.Processing;
            // Only grids reached by the scheduler accrue device dt; skipped grids do not build catch-up debt.
            runtime.TimeSinceLastDeviceUpdate += frameTime;

            // Charge Timer only at cycle start.
            if (runtime.CycleCursor is null)
            {
                runtime.Timer += frameTime;
                if (runtime.Timer < AtmosTime)
                    return AtmosphereProcessingCompletionState.Continue;

                runtime.Timer -= AtmosTime;
                runtime.CycleCursor = new AtmosphereCycleCursor(
                    AtmosphereProcessingState.Revalidate,
                    SnapshotPhaseFlags());

                // Advance before phase work so abandoned runs cannot reuse stale tile cycle markers.
                atmosphere.CycleCounter++;
            }

            while (true)
            {
                // Bail if a prior phase deleted the grid.
                if (atmosphere.LifeStage >= ComponentLifeStage.Stopping || TerminatingOrDeleted(owner))
                    return AbortCycle(atmosphere);

                var cursor = runtime.CycleCursor!.Value;

                // Recover from externally-stamped garbage without advancing the freshness marker.
                if ((uint)cursor.Phase >= (uint)AtmosphereProcessingState.NumStates)
                    return RecoverInvalidPhase(atmosphere);

                if (!RunPhase(cursor.Phase, ent, mapAtmosphere))
                {
                    runtime.ProcessingPaused = true;
                    return AtmosphereProcessingCompletionState.Return;
                }
                runtime.ProcessingPaused = false;

                var next = GetNextPhase(cursor.Phase, cursor.Flags);
                if (next == AtmosphereProcessingState.Revalidate)
                    return FinishCycle(atmosphere);

                runtime.CycleCursor = cursor with { Phase = next };

                // CycleCursor now points at the phase that will run on resume.
                if (BudgetExhausted)
                    return AtmosphereProcessingCompletionState.Return;
            }
        }
    }


    /// <summary>
    /// An enum representing the completion state of a <see cref="GridAtmosphereComponent"/>'s processing steps.
    /// The processing of a <see cref="GridAtmosphereComponent"/> spans over multiple stages and sticks,
    /// with the method handling the processing having multiple return types.
    /// </summary>
    public enum AtmosphereProcessingCompletionState : byte
    {
        /// <summary>
        /// Method is returning, ex. due to delegating processing to the next tick.
        /// </summary>
        Return,

        /// <summary>
        /// Method is continuing, ex. due to finishing a single processing stage.
        /// </summary>
        Continue,

        /// <summary>
        /// Method is finished with the GridAtmosphere.
        /// </summary>
        Finished,
    }
}
