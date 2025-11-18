using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.Atmos;

/// <summary>
/// Internal Atmospherics class that stores data on an atmosphere in a single tile.
/// You should not be using these directly outside of <see cref="AtmosphereSystem"/>.
/// Use the public APIs in <see cref="AtmosphereSystem"/> instead.
/// </summary>
[Access(typeof(AtmosphereSystem), typeof(GasTileOverlaySystem), typeof(AtmosDebugOverlaySystem))]
public sealed class TileAtmosphere : IGasMixtureHolder
{
    /// <summary>
    /// The last cycle this tile's air was archived into <see cref="AirArchived"/>.
    /// See <see cref="AirArchived"/> for more info on archival.
    /// </summary>
    [ViewVariables]
    public int ArchivedCycle;

    /// <summary>
    /// Current cycle this tile was processed.
    /// Used to prevent double-processing in a single cycle in many processing stages.
    /// </summary>
    [ViewVariables]
    public int CurrentCycle;

    /// <summary>
    /// Current temperature of this tile, in Kelvin.
    /// Used for Superconduction.
    /// This is not the temperature of the attached <see cref="GasMixture"/>!
    /// </summary>
    [ViewVariables]
    public float Temperature = Atmospherics.T20C;

    /// <summary>
    /// The current target tile for pressure movement for the current cycle.
    /// Gas will be moved towards this tile during pressure equalization.
    /// Also see <see cref="PressureDifference"/>.
    /// </summary>
    [ViewVariables]
    public TileAtmosphere? PressureSpecificTarget;

    /// <summary>
    /// The current pressure difference (delta) between this tile and its pressure target.
    /// If Monstermos is enabled, this value represents the quantity of moles transferred.
    /// </summary>
    [ViewVariables]
    public float PressureDifference;

    /// <summary>
    /// The current heat capacity of this tile.
    /// Used for Superconduction.
    /// This is not the heat capacity of the attached <see cref="GasMixture"/>!
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float HeatCapacity = Atmospherics.MinimumHeatCapacity;

    /// <summary>
    /// The current thermal conductivity of this tile.
    /// Describes how well heat moves between this tile and adjacent tiles during superconduction.
    /// </summary>
    [ViewVariables]
    public float ThermalConductivity = 0.05f;

    /// <summary>
    /// Designates whether this tile is currently excited for processing in an excited group or LINDA.
    /// </summary>
    [ViewVariables]
    public bool Excited;

    /// <summary>
    /// Whether this tile should be considered space.
    /// </summary>
    [ViewVariables]
    public bool Space;

    /// <summary>
    /// Cached adjacent <see cref="TileAtmosphere"/> tiles for this tile.
    /// Ordered in the same order as <see cref="Atmospherics.Directions"/>
    /// (should be North, South, East, West).
    /// Adjacent tiles can be null if air cannot flow to them.
    /// </summary>
    [ViewVariables]
    public readonly TileAtmosphere?[] AdjacentTiles = new TileAtmosphere[Atmospherics.Directions];

    /// <summary>
    /// Neighbouring tiles to which air can flow. This is a combination of this tile's unblocked direction, and the
    /// unblocked directions on adjacent tiles.
    /// </summary>
    [ViewVariables]
    public AtmosDirection AdjacentBits = AtmosDirection.Invalid;

    /// <summary>
    /// Current <see cref="MonstermosInfo"/> information for this tile.
    /// </summary>
    [ViewVariables]
    [Access(typeof(AtmosphereSystem), Other = AccessPermissions.ReadExecute)]
    public MonstermosInfo MonstermosInfo;

    /// <summary>
    /// Current <see cref="Hotspot"/> information for this tile.
    /// </summary>
    [ViewVariables]
    public Hotspot Hotspot;

    /// <summary>
    /// Points to the direction of the recipient tile for pressure equalization logic
    /// (Monstermos or HighPressureDelta otherwise).
    /// </summary>
    [ViewVariables]
    public AtmosDirection PressureDirection;

    /// <summary>
    /// Last cycle's <see cref="PressureDirection"/> for debugging purposes.
    /// </summary>
    [ViewVariables]
    public AtmosDirection LastPressureDirection;

    /// <summary>
    /// Grid entity this tile belongs to.
    /// </summary>
    [ViewVariables]
    [Access(typeof(AtmosphereSystem))]
    public EntityUid GridIndex;

    /// <summary>
    /// The grid indices of this tile.
    /// </summary>
    [ViewVariables]
    public Vector2i GridIndices;

    /// <summary>
    /// The excited group this tile belongs to, if any.
    /// </summary>
    [ViewVariables]
    public ExcitedGroup? ExcitedGroup;

    /// <summary>
    /// The air in this tile. If null, this tile is completely air-blocked.
    /// This can be immutable if the tile is spaced.
    /// </summary>
    [ViewVariables]
    [Access(typeof(AtmosphereSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
    public GasMixture? Air;

    /// <summary>
    /// A copy of the air in this tile from the last time it was archived at <see cref="ArchivedCycle"/>.
    /// LINDA archives the air before doing any necessary processing and uses this to perform its calculations,
    /// making the results of LINDA independent of the order in which tiles are processed.
    /// </summary>
    [ViewVariables]
    public GasMixture? AirArchived;

    /// <summary>
    /// The amount of gas last shared to adjacent tiles during LINDA processing.
    /// Used to determine when LINDA should dismantle an excited group
    /// or extend its time alive.
    /// </summary>
    [DataField("lastShare")]
    public float LastShare;

    /// <summary>
    /// Implementation of <see cref="IGasMixtureHolder.Air"/>.
    /// </summary>
    GasMixture IGasMixtureHolder.Air
    {
        get => Air ?? new GasMixture(Atmospherics.CellVolume){ Temperature = Temperature };
        set => Air = value;
    }

    /// <summary>
    /// The maximum temperature this tile has sustained during hotspot fire processing.
    /// Used for debugging.
    /// </summary>
    [ViewVariables]
    public float MaxFireTemperatureSustained;

    /// <summary>
    /// If true, then this tile is directly exposed to the map's atmosphere, either because the grid has no tile at
    /// this position, or because the tile type is not airtight.
    /// </summary>
    [ViewVariables]
    public bool MapAtmosphere;

    /// <summary>
    /// If true, this tile does not actually exist on the grid, it only exists to represent the map's atmosphere for
    /// adjacent grid tiles.
    /// This tile often has immutable air and is sitting off the edge of the grid, where there is no grid.
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

    /// <summary>
    /// Creates a new TileAtmosphere.
    /// </summary>
    /// <param name="gridIndex">The grid entity this tile belongs to.</param>
    /// <param name="gridIndices">>The grid indices of this tile.</param>
    /// <param name="mixture">The gas mixture of this tile.</param>
    /// <param name="immutable">If true, the gas mixture will be marked immutable.</param>
    /// <param name="space">If true, this tile is considered space.</param>
    public TileAtmosphere(EntityUid gridIndex, Vector2i gridIndices, GasMixture? mixture = null, bool immutable = false, bool space = false)
    {
        GridIndex = gridIndex;
        GridIndices = gridIndices;
        Air = mixture;
        AirArchived = Air?.Clone();
        Space = space;

        if(immutable)
            Air?.MarkImmutable();
    }

    /// <summary>
    /// Creates a copy of another TileAtmosphere.
    /// </summary>
    /// <param name="other">The TileAtmosphere to copy.</param>
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

    /// <summary>
    /// Creates a new empty TileAtmosphere.
    /// </summary>
    public TileAtmosphere()
    {
    }
}
