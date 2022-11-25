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
        public float TemperatureArchived { get; set; } = Atmospherics.T20C;

        [ViewVariables]
        public TileAtmosphere? PressureSpecificTarget { get; set; }

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
        public TileRef? Tile => GridIndices.GetTileRef(GridIndex);

        [ViewVariables]
        public Vector2i GridIndices { get; }

        [ViewVariables]
        public ExcitedGroup? ExcitedGroup { get; set; }

        /// <summary>
        /// The air in this tile. If null, this tile is completely air-blocked.
        /// This can be immutable if the tile is spaced.
        /// </summary>
        [ViewVariables]
        [Access(typeof(AtmosphereSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public GasMixture? Air { get; set; }

        [DataField("lastShare")]
        public float LastShare;

        [ViewVariables]
        public float[]? MolesArchived;

        GasMixture IGasMixtureHolder.Air
        {
            get => Air ?? new GasMixture(Atmospherics.CellVolume){ Temperature = Temperature };
            set => Air = value;
        }

        [ViewVariables]
        public float MaxFireTemperatureSustained { get; set; }

        [ViewVariables]
        public AtmosDirection BlockedAirflow { get; set; } = AtmosDirection.Invalid;

        public TileAtmosphere(EntityUid gridIndex, Vector2i gridIndices, GasMixture? mixture = null, bool immutable = false, bool space = false)
        {
            GridIndex = gridIndex;
            GridIndices = gridIndices;
            Air = mixture;
            Space = space;
            MolesArchived = Air != null ? new float[Atmospherics.AdjustedNumberOfGases] : null;

            if(immutable)
                Air?.MarkImmutable();
        }
    }
}
