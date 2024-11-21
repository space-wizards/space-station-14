using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    /// <summary>
    ///     Internal Atmos class that stores data about the atmosphere in a grid.
    ///     You shouldn't use this directly, use <see cref="AtmosphereSystem"/> instead.
    /// </summary>
    [Access(typeof(AtmosphereSystem), typeof(GasTileOverlaySystem), typeof(AtmosDebugOverlaySystem))]
    public sealed class TileAtmosphere : IGasMixtureHolder
    {
        [ViewVariables]
        public int ArchivedCycle;

        [ViewVariables]
        public int CurrentCycle;

        [ViewVariables]
        public float Temperature { get; set; } = Atmospherics.T20C;

        [ViewVariables]
        public TileAtmosphere? PressureSpecificTarget { get; set; }

        /// <summary>
        /// This is either the pressure difference, or the quantity of moles transferred if monstermos is enabled.
        /// </summary>
        [ViewVariables]
        public float PressureDifference { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float HeatCapacity { get; set; } = Atmospherics.MinimumHeatCapacity;

        [ViewVariables]
        public float ThermalConductivity { get; set; } = 0.05f;

        [ViewVariables]
        public bool Excited { get; set; }

        /// <summary>
        ///     Whether this tile should be considered space.
        /// </summary>
        [ViewVariables]
        public bool Space { get; set; }

        /// <summary>
        ///     Adjacent tiles in the same order as <see cref="AtmosDirection"/>. (NSEW)
        /// </summary>
        [ViewVariables]
        public readonly TileAtmosphere?[] AdjacentTiles = new TileAtmosphere[Atmospherics.Directions];

        /// <summary>
        /// Neighbouring tiles to which air can flow. This is a combination of this tile's unblocked direction, and the
        /// unblocked directions on adjacent tiles.
        /// </summary>
        [ViewVariables]
        public AtmosDirection AdjacentBits = AtmosDirection.Invalid;

        [ViewVariables, Access(typeof(AtmosphereSystem), Other = AccessPermissions.ReadExecute)]
        public MonstermosInfo MonstermosInfo;

        [ViewVariables]
        public Hotspot Hotspot;

        [ViewVariables]
        public AtmosDirection PressureDirection;

        // For debug purposes.
        [ViewVariables]
        public AtmosDirection LastPressureDirection;

        [ViewVariables]
        [Access(typeof(AtmosphereSystem))]
        public EntityUid GridIndex { get; set; }

        [ViewVariables]
        public Vector2i GridIndices;

        [ViewVariables]
        public ExcitedGroup? ExcitedGroup { get; set; }

        /// <summary>
        /// The air in this tile. If null, this tile is completely air-blocked.
        /// This can be immutable if the tile is spaced.
        /// </summary>
        [ViewVariables]
        [Access(typeof(AtmosphereSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public GasMixture? Air { get; set; }

        /// <summary>
        /// Like Air, but a copy stored each atmos tick before tile processing takes place. This lets us update Air
        /// in-place without affecting the results based on update order.
        /// </summary>
        [ViewVariables]
        public GasMixture? AirArchived;

        [DataField("lastShare")]
        public float LastShare;

        GasMixture IGasMixtureHolder.Air
        {
            get => Air ?? new GasMixture(Atmospherics.CellVolume){ Temperature = Temperature };
            set => Air = value;
        }

        [ViewVariables]
        public float MaxFireTemperatureSustained { get; set; }

        /// <summary>
        /// If true, then this tile is directly exposed to the map's atmosphere, either because the grid has no tile at
        /// this position, or because the tile type is not airtight.
        /// </summary>
        [ViewVariables]
        public bool MapAtmosphere;

        /// <summary>
        /// If true, this tile does not actually exist on the grid, it only exists to represent the map's atmosphere for
        /// adjacent grid tiles.
        /// </summary>
        [ViewVariables]
        public bool NoGridTile;

        /// <summary>
        /// If true, this tile is queued for processing in <see cref="GridAtmosphereComponent.PossiblyDisconnectedTiles"/>
        /// </summary>
        [ViewVariables]
        public bool TrimQueued;

        /// <summary>
        /// Cached information about airtight entities on this tile. This gets updated anytime a tile gets invalidated
        /// (i.e., gets added to <see cref="GridAtmosphereComponent.InvalidatedCoords"/>).
        /// </summary>
        public AtmosphereSystem.AirtightData AirtightData;

        public TileAtmosphere(EntityUid gridIndex, Vector2i gridIndices, GasMixture? mixture = null, bool immutable = false, bool space = false)
        {
            GridIndex = gridIndex;
            GridIndices = gridIndices;
            Air = mixture;
            AirArchived = Air != null ? Air.Clone() : null;
            Space = space;

            if(immutable)
                Air?.MarkImmutable();
        }

        public TileAtmosphere(TileAtmosphere other)
        {
            GridIndex = other.GridIndex;
            GridIndices = other.GridIndices;
            Space = other.Space;
            NoGridTile = other.NoGridTile;
            MapAtmosphere = other.MapAtmosphere;
            Air = other.Air?.Clone();
            AirArchived = Air != null ? Air.Clone() : null;
        }

        public TileAtmosphere()
        {
        }
    }
}
