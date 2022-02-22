using System;
using System.Collections;
using System.Collections.Generic;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Server.Atmos.Components
{
    /// <summary>
    ///     Internal Atmos class. Use <see cref="AtmosphereSystem"/> to interact with atmos instead.
    /// </summary>
    [ComponentReference(typeof(IAtmosphereComponent))]
    [RegisterComponent, Serializable]
    [Virtual]
    public class GridAtmosphereComponent : Component, IAtmosphereComponent, ISerializationHooks
    {
        public virtual bool Simulated => true;

        [ViewVariables]
        public bool ProcessingPaused { get; set; } = false;

        [ViewVariables]
        public float Timer { get; set; } = 0f;

        [ViewVariables]
        public int UpdateCounter { get; set; } = 0;

        [DataField("uniqueMixes")]
        public List<GasMixture>? UniqueMixes;

        [DataField("tiles")]
        public Dictionary<Vector2i, int>? TilesUniqueMixes;

        [ViewVariables]
        public readonly Dictionary<Vector2i, TileAtmosphere> Tiles = new(1000);

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
        public readonly HashSet<AtmosDeviceComponent> AtmosDevices = new();

        [ViewVariables]
        public Queue<TileAtmosphere> CurrentRunTiles = new();

        [ViewVariables]
        public Queue<ExcitedGroup> CurrentRunExcitedGroups = new();

        [ViewVariables]
        public Queue<IPipeNet> CurrentRunPipeNet = new();

        [ViewVariables]
        public Queue<AtmosDeviceComponent> CurrentRunAtmosDevices = new();

        [ViewVariables]
        public readonly HashSet<Vector2i> InvalidatedCoords = new(1000);

        [ViewVariables]
        public Queue<Vector2i> CurrentRunInvalidatedCoordinates = new();

        [ViewVariables]
        public int InvalidatedCoordsCount => InvalidatedCoords.Count;

        [ViewVariables]
        public long EqualizationQueueCycleControl { get; set; }

        [ViewVariables]
        public AtmosphereProcessingState State { get; set; } = AtmosphereProcessingState.TileEqualize;

        void ISerializationHooks.BeforeSerialization()
        {
            var uniqueMixes = new List<GasMixture>();
            var uniqueMixHash = new Dictionary<GasMixture, int>();
            var tiles = new Dictionary<Vector2i, int>();

            foreach (var (indices, tile) in Tiles)
            {
                if (tile.Air == null) continue;

                if (uniqueMixHash.TryGetValue(tile.Air, out var index))
                {
                    tiles[indices] = index;
                    continue;
                }

                uniqueMixes.Add(tile.Air);
                var newIndex = uniqueMixes.Count - 1;
                uniqueMixHash[tile.Air] = newIndex;
                tiles[indices] = newIndex;
            }

            if (uniqueMixes.Count == 0) uniqueMixes = null;
            if (tiles.Count == 0) tiles = null;

            UniqueMixes = uniqueMixes;
            TilesUniqueMixes = tiles;
        }
    }
}
