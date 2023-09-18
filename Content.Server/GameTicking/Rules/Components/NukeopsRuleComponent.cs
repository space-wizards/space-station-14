using Content.Server.NPC.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.Dataset;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(NukeopsRuleSystem), typeof(LoneOpsSpawnRule))]
public sealed partial class NukeopsRuleComponent : Component
{
    /// <summary>
    /// The minimum needed amount of players
    /// </summary>
    [DataField("minPlayers")]
    public int MinPlayers = 20;

    /// <summary>
    ///     This INCLUDES the operatives. So a value of 3 is satisfied by 2 players & 1 operative
    /// </summary>
    [DataField("playersPerOperative")]
    public int PlayersPerOperative = 10;

    [DataField("maxOps")]
    public int MaxOperatives = 5;

    /// <summary>
    /// Whether or not all of the nuclear operatives dying will end the round. Used by LoneOpsSpawn event.
    /// </summary>
    [DataField("endsRound")]
    public bool EndsRound = true;

    /// <summary>
    /// Whether or not to spawn the nuclear operative outpost. Used by LoneOpsSpawn event.
    /// </summary>
    [DataField("spawnOutpost")]
    public bool SpawnOutpost = true;

    /// <summary>
    /// Whether or not nukie left their outpost
    /// </summary>
    [DataField("leftOutpost")]
    public bool LeftOutpost = false;

    /// <summary>
    ///     Enables opportunity to get extra TC for war declaration
    /// </summary>
    [DataField("canEnableWarOps")]
    public bool CanEnableWarOps = true;

    /// <summary>
    ///     Indicates time when war has been declared, null if not declared
    /// </summary>
    [DataField("warDeclaredTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? WarDeclaredTime;

    /// <summary>
    ///     This amount of TC will be given to each nukie
    /// </summary>
    [DataField("warTCAmountPerNukie")]
    public int WarTCAmountPerNukie = 40;

    /// <summary>
    ///     Time allowed for declaration of war
    /// </summary>
    [DataField("warDeclarationDelay")]
    public TimeSpan WarDeclarationDelay = TimeSpan.FromMinutes(6);

    /// <summary>
    ///     Delay between war declaration and nuke ops arrival on station map. Gives crew time to prepare
    /// </summary>
    [DataField("warNukieArriveDelay")]
    public TimeSpan? WarNukieArriveDelay = TimeSpan.FromMinutes(15);

    /// <summary>
    ///     Minimal operatives count for war declaration
    /// </summary>
    [DataField("warDeclarationMinOps")]
    public int WarDeclarationMinOps = 4;

    [DataField("spawnPointProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnPointPrototype = "SpawnPointNukies";

    [DataField("ghostSpawnPointProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string GhostSpawnPointProto = "SpawnPointGhostNukeOperative";

    [DataField("commanderRoleProto", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string CommanderRolePrototype = "NukeopsCommander";

    [DataField("operativeRoleProto", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string OperativeRoleProto = "Nukeops";

    [DataField("medicRoleProto", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string MedicRoleProto = "NukeopsMedic";

    [DataField("commanderStartingGearProto", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string CommanderStartGearPrototype = "SyndicateCommanderGearFull";

    [DataField("medicStartGearProto", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string MedicStartGearPrototype = "SyndicateOperativeMedicFull";

    [DataField("operativeStartGearProto", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string OperativeStartGearPrototype = "SyndicateOperativeGearFull";

    [DataField("eliteNames", customTypeSerializer: typeof(PrototypeIdSerializer<DatasetPrototype>))]
    public string EliteNames = "SyndicateNamesElite";

    [DataField("normalNames", customTypeSerializer: typeof(PrototypeIdSerializer<DatasetPrototype>))]
    public string NormalNames = "SyndicateNamesNormal";

    [DataField("outpostMap", customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath NukieOutpostMap = new("/Maps/nukieplanet.yml");

    [DataField("shuttleMap", customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath NukieShuttleMap = new("/Maps/infiltrator.yml");

    [DataField("winType")]
    public WinType WinType = WinType.Neutral;

    [DataField("winConditions")]
    public List<WinCondition> WinConditions = new ();

    public MapId? NukiePlanet;

    // TODO: use components, don't just cache entity UIDs
    // There have been (and probably still are) bugs where these refer to deleted entities from old rounds.
    public EntityUid? NukieOutpost;
    public EntityUid? NukieShuttle;
    public EntityUid? TargetStation;

    /// <summary>
    ///     Cached starting gear prototypes.
    /// </summary>
    [DataField("startingGearPrototypes")]
    public Dictionary<string, StartingGearPrototype> StartingGearPrototypes = new ();

    /// <summary>
    ///     Cached operator name prototypes.
    /// </summary>
    [DataField("operativeNames")]
    public Dictionary<string, List<string>> OperativeNames = new();

    /// <summary>
    ///     Data to be used in <see cref="OnMindAdded"/> for an operative once the Mind has been added.
    /// </summary>
    [DataField("operativeMindPendingData")]
    public Dictionary<EntityUid, string> OperativeMindPendingData = new();

    /// <summary>
    ///     Players who played as an operative at some point in the round.
    ///     Stores the mind as well as the entity name
    /// </summary>
    [DataField("operativePlayers")]
    public Dictionary<string, EntityUid> OperativePlayers = new();

    [DataField("faction", customTypeSerializer: typeof(PrototypeIdSerializer<NpcFactionPrototype>), required: true)]
    public string Faction = default!;
}

public enum WinType : byte
{
    /// <summary>
    ///     Operative major win. This means they nuked the station.
    /// </summary>
    OpsMajor,
    /// <summary>
    ///     Minor win. All nukies were alive at the end of the round.
    ///     Alternatively, some nukies were alive, but the disk was left behind.
    /// </summary>
    OpsMinor,
    /// <summary>
    ///     Neutral win. The nuke exploded, but on the wrong station.
    /// </summary>
    Neutral,
    /// <summary>
    ///     Crew minor win. The nuclear authentication disk escaped on the shuttle,
    ///     but some nukies were alive.
    /// </summary>
    CrewMinor,
    /// <summary>
    ///     Crew major win. This means they either killed all nukies,
    ///     or the bomb exploded too far away from the station, or on the nukie moon.
    /// </summary>
    CrewMajor
}

public enum WinCondition : byte
{
    NukeExplodedOnCorrectStation,
    NukeExplodedOnNukieOutpost,
    NukeExplodedOnIncorrectLocation,
    NukeActiveInStation,
    NukeActiveAtCentCom,
    NukeDiskOnCentCom,
    NukeDiskNotOnCentCom,
    NukiesAbandoned,
    AllNukiesDead,
    SomeNukiesAlive,
    AllNukiesAlive
}
