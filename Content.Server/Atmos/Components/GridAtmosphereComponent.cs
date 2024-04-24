using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Serialization;
using Content.Server.NodeContainer.NodeGroups;

namespace Content.Server.Atmos.Components
{
    /// <summary>
    ///     Internal Atmos class. Use <see cref="AtmosphereSystem"/> to interact with atmos instead.
    /// </summary>
    [RegisterComponent, Serializable,
     Access(typeof(AtmosphereSystem), typeof(GasTileOverlaySystem), typeof(AtmosDebugOverlaySystem))]
    public sealed partial class GridAtmosphereComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Simulated { get; set; } = true;

        [ViewVariables]
        public bool ProcessingPaused { get; set; } = false;

        [ViewVariables]
        public float Timer { get; set; } = 0f;

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
