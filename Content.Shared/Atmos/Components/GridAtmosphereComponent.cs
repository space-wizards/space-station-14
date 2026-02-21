using System.Collections.Concurrent;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Atmos.Serialization;
using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Shared.Atmos.Components
{
    /// <summary>
    ///     Internal Atmos class. Use <see cref="SharedAtmosphereSystem"/> to interact with atmos instead.
    /// </summary>
    [RegisterComponent, Serializable,
     Access(typeof(SharedAtmosphereSystem), typeof(SharedGasTileOverlaySystem), typeof(SharedAtmosDebugOverlaySystem))]
    public sealed partial class GridAtmosphereComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Simulated { get; set; } = true;

        [ViewVariables]
        public bool ProcessingPaused { get; set; }

        [ViewVariables]
        public float Timer { get; set; }

        [ViewVariables]
        public int UpdateCounter { get; set; } = 1; // DO NOT SET TO ZERO BY DEFAULT! It will break roundstart atmos...

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

        /// <summary>
        /// Integer that indicates the current position in the
        /// <see cref="DeltaPressureEntities"/> list that is being processed.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public int DeltaPressureCursor;

        /// <summary>
        /// Queue of entities that need to have damage applied to them.
        /// </summary>
        [ViewVariables]
        public readonly ConcurrentQueue<DeltaPressureDamageResult> DeltaPressureDamageResults = new();

        [ViewVariables]
        public readonly HashSet<IPipeNet> PipeNets = new();

        [ViewVariables]
        public readonly HashSet<Entity<AtmosDeviceComponent>> AtmosDevices = new();

        [ViewVariables]
        public readonly Queue<TileAtmosphere> CurrentRunTiles = new();

        [ViewVariables]
        public readonly Queue<ExcitedGroup> CurrentRunExcitedGroups = new();

        [ViewVariables]
        public readonly Queue<IPipeNet> CurrentRunPipeNet = new();

        [ViewVariables]
        public readonly Queue<Entity<AtmosDeviceComponent>> CurrentRunAtmosDevices = new();

        [ViewVariables]
        public readonly HashSet<Vector2i> InvalidatedCoords = new(1000);

        [ViewVariables]
        public readonly Queue<TileAtmosphere> CurrentRunInvalidatedTiles = new();

        [ViewVariables]
        public readonly List<TileAtmosphere> PossiblyDisconnectedTiles = new(100);

        [ViewVariables]
        public int InvalidatedCoordsCount => InvalidatedCoords.Count;

        [ViewVariables]
        public long EqualizationQueueCycleControl { get; set; }

        [ViewVariables]
        public AtmosphereProcessingState State { get; set; } = AtmosphereProcessingState.Revalidate;
    }
}
