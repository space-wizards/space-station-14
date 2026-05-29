using Content.Server.Atmos.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /*
     Helper methods to assist in getting very low overhead profiling of individual stages of the atmospherics simulation.
     Ideal for benchmarking and performance testing.
     These methods obviously aren't to be used in production code. Don't call them. They know my voice.
     */

    /// <summary>
    /// Runs the grid entity through a single processing stage of the atmosphere simulation.
    /// Ideal for benchmarking single stages of the simulation.
    /// </summary>
    /// <param name="ent">The entity to profile Atmospherics with.</param>
    /// <param name="state">The state to profile on the entity.</param>
    /// <param name="mapEnt">The optional mapEntity to provide when benchmarking ProcessAtmosDevices.</param>
    /// <returns>True if the processing stage completed, false if the processing stage had to pause processing due to time constraints.</returns>
    public bool RunProcessingStage(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        AtmosphereProcessingState state,
        Entity<MapAtmosphereComponent?>? mapEnt = null)
    {
        if ((uint)state >= (uint)AtmosphereProcessingState.NumStates)
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown atmosphere phase.");
        if (state == AtmosphereProcessingState.AtmosDevices && mapEnt is null)
            throw new ArgumentException(
                "An Entity<MapAtmosphereComponent> must be provided when benchmarking ProcessAtmosDevices.",
                nameof(mapEnt));

        _simulationStopwatch.Restart();
        var completed = PhaseTable[(int)state].Runner(this, ent, mapEnt.GetValueOrDefault());
        ent.Comp1.Processing.ProcessingPaused = !completed;
        return completed;
    }

    /// <summary>
    /// Fully runs one <see cref="GridAtmosphereComponent"/> entity through the entire Atmos processing loop.
    /// </summary>
    /// <param name="ent">The entity to simulate.</param>
    /// <param name="mapAtmosphere">The <see cref="MapAtmosphereComponent"/> that belongs to the grid's map.</param>
    /// <param name="frameTime">Elapsed time to simulate. Must be at least <see cref="AtmosTime"/>.</param>
    /// <remarks>Test/benchmark only. Restarts the stopwatch each iteration, hiding budget-related scheduling bugs. Use <see cref="ProcessAtmosphereOnce"/> to observe production pause/resume.</remarks>
    public void RunProcessingFull(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        Entity<MapAtmosphereComponent?> mapAtmosphere,
        float frameTime)
    {
        DebugTools.Assert(frameTime >= AtmosTime,
            $"RunProcessingFull requires frameTime >= AtmosTime ({AtmosTime}) to make progress; got {frameTime}.");

        // Catches a phase that fails to make progress on resume (would otherwise spin forever).
        const int maxIterations = 10_000;

        AtmosphereProcessingCompletionState state;
        var iterations = 0;
        do
        {
            _simulationStopwatch.Restart();
            state = ProcessAtmosphere(ent, mapAtmosphere, iterations == 0 ? frameTime : 0f);
            if (++iterations > maxIterations)
                throw new InvalidOperationException($"Cycle didn't complete after {maxIterations} iterations; a phase is failing to make progress.");
        } while (state == AtmosphereProcessingCompletionState.Return);

        DebugTools.Assert(state == AtmosphereProcessingCompletionState.Finished,
            "RunProcessingFull completed without finishing a cycle; the grid may have been deleted mid-run.");
    }

    /// <summary>
    /// Allows or disallows atmosphere simulation on a <see cref="GridAtmosphereComponent"/>.
    /// </summary>
    /// <param name="ent">The atmosphere to pause or unpause processing.</param>
    /// <param name="simulate">The state to set. True means that the atmosphere is allowed to simulate, false otherwise.</param>
    public void SetAtmosphereSimulation(Entity<GridAtmosphereComponent> ent, bool simulate)
    {
        ent.Comp.Simulated = simulate;
    }

    /// <summary>
    /// Test-only: stamps a phase into the cursor after clearing all cycle scratch state.
    /// Wall-time bookkeeping is preserved so dt accounting carries across the stamp.
    /// </summary>
    public void SetProcessingState(Entity<GridAtmosphereComponent> ent, AtmosphereProcessingState state)
    {
        ResetCycleScratch(ent.Comp);
        ent.Comp.Processing.CycleCursor = new AtmosphereCycleCursor(state, SnapshotPhaseFlags());
    }

    /// <summary>
    /// One <see cref="ProcessAtmosphere"/> call with a fresh stopwatch, for tests that need per-call observation.
    /// </summary>
    public AtmosphereProcessingCompletionState ProcessAtmosphereOnce(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        Entity<MapAtmosphereComponent?> map,
        float frameTime)
    {
        _simulationStopwatch.Restart();
        return ProcessAtmosphere(ent, map, frameTime);
    }
}
