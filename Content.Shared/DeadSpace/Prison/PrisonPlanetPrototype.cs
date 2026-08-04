using System.Numerics;
using Content.Shared.Atmos;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Markers;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.DeadSpace.Prison;

[Prototype]
public sealed partial class PrisonPlanetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<BiomeTemplatePrototype> Biome = default!;

    [DataField]
    public Color? LightColor = Color.FromHex("#8DA8C6");

    [DataField]
    public GasMixture? Atmosphere;

    [DataField]
    public bool Gravity = true;

    [DataField]
    public string MapName = "Prison Quarry";

    [DataField]
    public int MapHalfSize = 250;

    [DataField]
    public bool BoundaryEnabled = true;

    [DataField]
    public int BoundaryWallWidth = 6;

    [DataField]
    public string BoundaryTile = "FloorChromite";

    [DataField]
    public string BoundaryWallEntity = "WallRockChromitePrisonBoundary";

    [DataField]
    public int LandingPadRadius = 56;

    [DataField]
    public string LandingPadTile = "FloorSnowDug";

    [DataField]
    public bool ResidenceReservationEnabled = true;

    [DataField]
    public int ResidenceReservationSize = 112;

    [DataField]
    public string ResidenceTile = "FloorSnowDug";

    [DataField]
    public ResPath? ResidenceGridPath;

    [DataField]
    public Vector2 ResidenceGridOffset = Vector2.Zero;

    [DataField]
    public string? ResidenceGridName = "Prison Base";

    [DataField]
    public bool FtlEnabled = true;

    [DataField]
    public bool FtlBeaconsOnly = true;

    [DataField]
    public bool RequireCoordinateDisk;

    [DataField]
    public EntityWhitelist? FtlWhitelist;

    [DataField]
    public EntityWhitelist? FtlDockWhitelist;

    [DataField]
    public string FtlBeaconName = "Prison Landing Zone";

    [DataField]
    public Vector2 FtlBeaconOffset = new(-112f, -112f);

    [DataField]
    public float FtlFallbackMinOffset = 8f;

    [DataField]
    public float FtlFallbackMaxOffset = 40f;

    [DataField]
    public List<ProtoId<BiomeMarkerLayerPrototype>> MarkerLayers = new();

    [DataField]
    public bool FaunaEnabled = true;

    [DataField]
    public int FaunaInitialSpawnCount = 48;

    [DataField]
    public int FaunaInitialSpawnBatchSize = 4;

    [DataField]
    public TimeSpan FaunaInitialSpawnInterval = TimeSpan.FromSeconds(5);

    [DataField]
    public int FaunaSoftCap = 48;

    [DataField]
    public int FaunaHardCap = 60;

    [DataField]
    public TimeSpan FaunaUpdateInterval = TimeSpan.FromSeconds(90);

    [DataField]
    public int FaunaSpawnBatchMin = 1;

    [DataField]
    public int FaunaSpawnBatchMax = 3;

    [DataField]
    public int FaunaLowPopulationThreshold = 24;

    [DataField]
    public int FaunaLowPopulationBatchMax = 6;

    [DataField]
    public int FaunaSpawnAttempts = 160;

    [DataField]
    public int FaunaSpawnClearance = 1;

    [DataField]
    public float FaunaMapEdgePadding = 12f;

    [DataField]
    public float FaunaMinPlayerDistance = 24f;

    [DataField]
    public float FaunaResidenceExclusionPadding = 8f;

    [DataField]
    public float FaunaLandingExclusionRadius = 64f;

    [DataField]
    public int FaunaSectorSize = 48;

    [DataField]
    public TimeSpan FaunaSectorCooldown = TimeSpan.FromMinutes(6);

    [DataField]
    public List<PrisonFaunaSpawnEntry> FaunaSpawns = new();
}

[DataDefinition]
public sealed partial class PrisonFaunaSpawnEntry
{
    [DataField(required: true)]
    public EntProtoId Prototype = default;

    [DataField]
    public int Weight = 1;

    [DataField]
    public int MaxCount = 8;

    [DataField]
    public int SentenceReductionMinutes = 1;
}
