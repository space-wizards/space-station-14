using System.Collections.Concurrent;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Serialization;
using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Internal <see cref="SharedAtmosphereSystem"/> class for storing all grid data.
/// If you need to access this data, use the API methods in the <see cref="SharedAtmosphereSystem"/>/server
/// instead of trying to scour this component or others for the data you need.
/// </summary>
[RegisterComponent, Serializable,
 Access(typeof(SharedAtmosphereSystem), typeof(SharedGasTileOverlaySystem), typeof(SharedAtmosDebugOverlaySystem))]
public sealed partial class GridAtmosphereComponent : Component
{
    /// <summary>
    /// Whether the grid is being updated by Atmospherics.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Simulated = true;

    /// <summary>
    /// Mutable runtime state for the current atmos processing cycle. This is not serialized and is rebuilt as
    /// needed when processing restarts or a grid atmosphere is rebuilt.
    /// </summary>
    [NonSerialized, ViewVariables]
    public readonly AtmosphereProcessingRuntime Processing = new();

    /// <summary>
    /// Current processing phase for view variables and diagnostics.
    /// </summary>
    [ViewVariables]
    public AtmosphereProcessingState State => Processing.CycleCursor?.Phase ?? AtmosphereProcessingState.Revalidate;

    /// <summary>
    /// Monotonic processing-cycle counter. Advanced when an atmos cycle starts so
    /// per-tile cycle fields can distinguish stale work from the current cycle,
    /// including after abandoned cycles.
    /// </summary>
    /// <remarks>Do not set to zero by default or you will break roundstart atmos.</remarks>
    [ViewVariables]
    public int CycleCounter = 1;

    [ViewVariables]
    [IncludeDataField(customTypeSerializer:typeof(TileAtmosCollectionSerializer))]
    public Dictionary<Vector2i, TileAtmosphere> Tiles = new(1000);

    [ViewVariables]
    public HashSet<TileAtmosphere> MapTiles = new(1000);

    [ViewVariables]
    public readonly HashSet<TileAtmosphere> ActiveTiles = new(1000);

    [ViewVariables]
    public int ActiveTilesCount => ActiveTiles.Count;

    [ViewVariables]
    public readonly HashSet<ExcitedGroup> ExcitedGroups = new(1000);

    [ViewVariables]
    public int ExcitedGroupCount => ExcitedGroups.Count;

    [ViewVariables]
    public readonly HashSet<TileAtmosphere> HotspotTiles = new(1000);

    [ViewVariables]
    public int HotspotTilesCount => HotspotTiles.Count;

    [ViewVariables]
    public readonly HashSet<TileAtmosphere> SuperconductivityTiles = new(1000);

    [ViewVariables]
    public int SuperconductivityTilesCount => SuperconductivityTiles.Count;

    [ViewVariables]
    public HashSet<TileAtmosphere> HighPressureDelta = new(1000);

    [ViewVariables]
    public int HighPressureDeltaCount => HighPressureDelta.Count;

    /// <summary>
    /// A list of entities that have a <see cref="DeltaPressureComponent"/> and are to
    /// be processed by the <see cref="DeltaPressureSystem"/>, if enabled.
    ///
    /// To prevent massive bookkeeping overhead, this list is processed in-place,
    /// with add/remove/find operations helped via a dict.
    /// </summary>
    /// <remarks>If you want to add/remove/find entities in this list,
    /// use the API methods in the Atmospherics API.</remarks>
    [ViewVariables]
    public readonly List<Entity<DeltaPressureComponent>> DeltaPressureEntities =
        new(SharedAtmosphereSystem.DeltaPressurePreAllocateLength);

    /// <summary>
    /// An index lookup for the <see cref="DeltaPressureEntities"/> list.
    /// Used for add/remove/find operations to speed up processing.
    /// </summary>
    public readonly Dictionary<EntityUid, int> DeltaPressureEntityLookup =
        new(SharedAtmosphereSystem.DeltaPressurePreAllocateLength);


    [ViewVariables]
    public readonly HashSet<IPipeNet> PipeNets = new();

    [ViewVariables]
    public readonly HashSet<Entity<AtmosDeviceComponent>> AtmosDevices = new();

    [ViewVariables]
    public readonly HashSet<Vector2i> InvalidatedCoords = new(1000);

    [ViewVariables]
    public readonly List<TileAtmosphere> PossiblyDisconnectedTiles = new(100);

    [ViewVariables]
    public int InvalidatedCoordsCount => InvalidatedCoords.Count;

    [ViewVariables]
    public long EqualizationQueueCycleControl { get; set; }
}

/// <summary>
/// Transient state for a grid's in-flight atmos processing cycle.
/// </summary>
/// <remarks>
/// Phase-owned cursors, snapshots, and queues live here. The server scratch reset clears them while preserving
/// <see cref="Timer"/> and <see cref="TimeSinceLastDeviceUpdate"/> so dt accounting carries across resets.
/// </remarks>
public sealed class AtmosphereProcessingRuntime
{
    /// <summary>
    /// Indicator for if Atmospherics has delegated the processing of this
    /// grid to another tick due to the time budget running out.
    /// </summary>
    /// <example>If true, Atmospherics is not finished
    /// processing the current stage and has yielded processing
    /// to the next tick.</example>
    [ViewVariables]
    public bool ProcessingPaused;

    /// <summary>
    /// Current phase and optional-phase snapshot for the in-flight atmos cycle, or null when no cycle is active.
    /// </summary>
    [ViewVariables]
    public AtmosphereCycleCursor? CycleCursor;

    /// <summary>
    /// Accumulated frame time since atmos devices last received an update.
    /// </summary>
    [ViewVariables]
    public float TimeSinceLastDeviceUpdate;

    /// <summary>
    /// Device update delta captured when the AtmosDevices phase begins. Kept stable while that phase is paused.
    /// </summary>
    [ViewVariables]
    public float CurrentRunDeviceDt;

    /// <summary>
    /// Timer used to delay processing for every AtmosTick.
    /// No, Atmospherics cannot tick every frame.
    /// </summary>
    /// TODO: Replace with TimeSpan please.
    [ViewVariables]
    public float Timer;

    /// <summary>
    /// Tile snapshot and resume cursor for the Monstermos equalization phase.
    /// </summary>
    [ViewVariables]
    public readonly TileRunState EqualizeRun = new();

    /// <summary>
    /// Tile snapshot and resume cursor for the active tile phase.
    /// </summary>
    [ViewVariables]
    public readonly TileRunState ActiveTilesRun = new();

    /// <summary>
    /// Tile snapshot and resume cursor for the high-pressure delta phase.
    /// </summary>
    [ViewVariables]
    public readonly TileRunState HighPressureDeltaRun = new();

    /// <summary>
    /// Tile snapshot and resume cursor for the hotspot phase.
    /// </summary>
    [ViewVariables]
    public readonly TileRunState HotspotRun = new();

    /// <summary>
    /// Tile snapshot and resume cursor for the superconductivity phase.
    /// </summary>
    [ViewVariables]
    public readonly TileRunState SuperconductRun = new();

    /// <summary>
    /// Scratch queue for excited groups.
    /// </summary>
    [ViewVariables]
    public readonly Queue<ExcitedGroup> CurrentRunExcitedGroups = new();

    /// <summary>
    /// Scratch queue for pipe nets.
    /// </summary>
    [ViewVariables]
    public readonly Queue<IPipeNet> CurrentRunPipeNet = new();

    /// <summary>
    /// Scratch queue for atmos devices.
    /// </summary>
    [ViewVariables]
    public readonly Queue<Entity<AtmosDeviceComponent>> CurrentRunAtmosDevices = new();

    /// <summary>
    /// Scratch queue for revalidation tiles.
    /// </summary>
    [ViewVariables]
    public readonly Queue<TileAtmosphere> CurrentRunInvalidatedTiles = new();

    /// <summary>
    /// Integer that indicates the current position in the
    /// <see cref="DeltaPressureSnapshot"/> list that is being processed.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public int DeltaPressureCursor;

    /// <summary>
    /// Snapshot of <see cref="GridAtmosphereComponent.DeltaPressureEntities"/> taken at the
    /// start of the DeltaPressure phase. Prevents swap-removals during a mid-phase pause from
    /// skipping entities or causing out-of-bounds access when the phase resumes.
    /// </summary>
    [ViewVariables]
    public readonly List<Entity<DeltaPressureComponent>> DeltaPressureSnapshot =
        new(SharedAtmosphereSystem.DeltaPressurePreAllocateLength);

    /// <summary>
    /// Queue of entities that need to have damage applied to them.
    /// </summary>
    [ViewVariables]
    public readonly ConcurrentQueue<DeltaPressureDamageResult> DeltaPressureDamageResults = new();
}

/// <summary>
/// Phase-local tile snapshot plus resume cursor.
/// </summary>
/// <remarks>
/// Tile phases iterate the snapshot so live source sets may mutate during processing. <see cref="Cursor"/> points to
/// the next tile after a pause.
/// </remarks>
public sealed class TileRunState
{
    /// <summary>
    /// Phase-local snapshot of tiles to process for the current run.
    /// </summary>
    [ViewVariables]
    public readonly List<TileAtmosphere> Tiles = new();

    /// <summary>
    /// Index of the next tile in <see cref="Tiles"/> to process when resuming.
    /// </summary>
    [ViewVariables]
    public int Cursor;

    /// <summary>
    /// Clears the current tile snapshot and rewinds the resume cursor.
    /// </summary>
    public void Reset()
    {
        Tiles.Clear();
        Cursor = 0;
    }
}
