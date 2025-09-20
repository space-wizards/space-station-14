using Content.Server.Atmos.Components;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;

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
        var processingPaused = state switch
        {
            AtmosphereProcessingState.Revalidate => ProcessRevalidate(ent),
            AtmosphereProcessingState.TileEqualize => ProcessTileEqualize(ent),
            AtmosphereProcessingState.ActiveTiles => ProcessActiveTiles(ent),
            AtmosphereProcessingState.ExcitedGroups => ProcessExcitedGroups(ent),
            AtmosphereProcessingState.HighPressureDelta => ProcessHighPressureDelta(ent),
            AtmosphereProcessingState.DeltaPressure => ProcessDeltaPressure(ent),
            AtmosphereProcessingState.Hotspots => ProcessHotspots(ent),
            AtmosphereProcessingState.Superconductivity => ProcessSuperconductivity(ent),
            AtmosphereProcessingState.PipeNet => ProcessPipeNets(ent),
            AtmosphereProcessingState.AtmosDevices => mapEnt is not null
                ? ProcessAtmosDevices(ent, mapEnt.Value)
                : throw new ArgumentException(
                    "An Entity<MapAtmosphereComponent> must be provided when benchmarking ProcessAtmosDevices."),
            _ => throw new ArgumentOutOfRangeException(),
        };
        ent.Comp1.ProcessingPaused = !processingPaused;

        return processingPaused;
    }

    /// <summary>
    /// Fully runs one <see cref="GridAtmosphereComponent"/> entity through the entire Atmos processing loop.
    /// </summary>
    /// <param name="ent">The entity to simulate.</param>
    /// <param name="mapAtmosphere">The <see cref="MapAtmosphereComponent"/> that belongs to the grid's map.</param>
    /// <param name="frameTime">Elapsed time to simulate. Recommended value is <see cref="AtmosTickRate"/>.</param>
    public void RunProcessingFull(Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        Entity<MapAtmosphereComponent?> mapAtmosphere,
        float frameTime)
    {
        while (ProcessAtmosphere(ent, mapAtmosphere, frameTime) != AtmosphereProcessingCompletionState.Finished) { }
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
}
