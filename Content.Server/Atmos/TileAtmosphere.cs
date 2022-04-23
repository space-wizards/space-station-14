using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos
{
    /// <summary>
    ///     Internal Atmos class that stores data about the atmosphere in a grid.
    ///     You shouldn't use this directly, use <see cref="AtmosphereSystem"/> instead.
    /// </summary>
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
        ///     Adjacent tiles in the same order as <see cref="AtmosDirection"/>. (NSEW)
        /// </summary>
        [ViewVariables]
        public readonly TileAtmosphere?[] AdjacentTiles = new TileAtmosphere[Atmospherics.Directions];

        [ViewVariables]
        public AtmosDirection AdjacentBits = AtmosDirection.Invalid;

        [ViewVariables]
        public MonstermosInfo MonstermosInfo;

        [ViewVariables]
        public Hotspot Hotspot;

        [ViewVariables]
        public AtmosDirection PressureDirection;

        [ViewVariables]
        public GridId GridIndex { get; }

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
        public GasMixture? Air { get; set; }

        GasMixture IGasMixtureHolder.Air
        {
            get => Air ?? new GasMixture(Atmospherics.CellVolume){ Temperature = Temperature };
            set => Air = value;
        }

        [ViewVariables]
        public float MaxFireTemperatureSustained { get; set; }

        [ViewVariables]
        public AtmosDirection BlockedAirflow { get; set; } = AtmosDirection.Invalid;

        public TileAtmosphere(GridId gridIndex, Vector2i gridIndices, GasMixture? mixture = null, bool immutable = false)
        {
            GridIndex = gridIndex;
            GridIndices = gridIndices;
            Air = mixture;

            if(immutable)
                Air?.MarkImmutable();
        }
    }
}
