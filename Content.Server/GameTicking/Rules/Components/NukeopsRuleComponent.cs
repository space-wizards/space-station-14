using Content.Server.NPC.Components;
using Content.Server.RoundEnd;
using Content.Server.StationEvents.Events;
using Content.Shared.Dataset;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(NukeopsRuleSystem), typeof(LoneOpsSpawnRule))]
public sealed partial class NukeopsRuleComponent : Component
{
    // TODO Replace with GameRuleComponent.minPlayers
    /// <summary>
    /// The minimum needed amount of players
    /// </summary>
    [DataField]
    public int MinPlayers = 20;

    /// <summary>
    ///     This INCLUDES the operatives. So a value of 3 is satisfied by 2 players & 1 operative
    /// </summary>
    [DataField]
    public int PlayersPerOperative = 10;

    [DataField]
    public int MaxOps = 5;

    /// <summary>
    /// What will happen if all of the nuclear operatives will die. Used by LoneOpsSpawn event.
    /// </summary>
    [DataField]
    public RoundEndBehavior RoundEndBehavior = RoundEndBehavior.ShuttleCall;

    /// <summary>
    /// Text for shuttle call if RoundEndBehavior is ShuttleCall.
    /// </summary>
    [DataField]
    public string RoundEndTextSender = "comms-console-announcement-title-centcom";

    /// <summary>
    /// Text for shuttle call if RoundEndBehavior is ShuttleCall.
    /// </summary>
    [DataField]
    public string RoundEndTextShuttleCall = "nuke-ops-no-more-threat-announcement-shuttle-call";

    /// <summary>
    /// Text for announcement if RoundEndBehavior is ShuttleCall. Used if shuttle is already called
    /// </summary>
    [DataField]
    public string RoundEndTextAnnouncement = "nuke-ops-no-more-threat-announcement";

    /// <summary>
    /// Time to emergency shuttle to arrive if RoundEndBehavior is ShuttleCall.
    /// </summary>
    [DataField]
    public TimeSpan EvacShuttleTime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Whether or not to spawn the nuclear operative outpost. Used by LoneOpsSpawn event.
    /// </summary>
    [DataField]
    public bool SpawnOutpost = true;

    /// <summary>
    /// Whether or not nukie left their outpost
    /// </summary>
    [DataField]
    public bool LeftOutpost;

    /// <summary>
    ///     Enables opportunity to get extra TC for war declaration
    /// </summary>
    [DataField]
    public bool CanEnableWarOps = true;

    /// <summary>
    ///     Indicates time when war has been declared, null if not declared
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? WarDeclaredTime;

    /// <summary>
    ///     This amount of TC will be given to each nukie
    /// </summary>
    [DataField]
    public int WarTCAmountPerNukie = 40;

    /// <summary>
    ///     Time allowed for declaration of war
    /// </summary>
    [DataField("warDeclarationDelay")]
    public TimeSpan WarDeclarationDelay = TimeSpan.FromMinutes(6);

    /// <summary>
    ///     Delay between war declaration and nuke ops arrival on station map. Gives crew time to prepare
    /// </summary>
    [DataField]
    public TimeSpan? WarNukieArriveDelay = TimeSpan.FromMinutes(15);

    /// <summary>
    ///     Minimal operatives count for war declaration
    /// </summary>
    [DataField]
    public int WarDeclarationMinOps = 4;

    [DataField]
    public EntProtoId SpawnPointProto = "SpawnPointNukies";

    [DataField]
    public EntProtoId GhostSpawnPointProto = "SpawnPointGhostNukeOperative";

    [DataField]
    public ProtoId<AntagPrototype> CommanderRoleProto = "NukeopsCommander";

    [DataField]
    public ProtoId<AntagPrototype> OperativeRoleProto = "Nukeops";

    [DataField]
    public ProtoId<AntagPrototype> MedicRoleProto = "NukeopsMedic";

    [DataField]
    public ProtoId<StartingGearPrototype> CommanderStartGearProto = "SyndicateCommanderGearFull";

    [DataField]
    public ProtoId<StartingGearPrototype> MedicStartGearProto = "SyndicateOperativeMedicFull";

    [DataField]
    public ProtoId<StartingGearPrototype> OperativeStartGearProto = "SyndicateOperativeGearFull";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<DatasetPrototype>))]
    public string EliteNames = "SyndicateNamesElite";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<DatasetPrototype>))]
    public string NormalNames = "SyndicateNamesNormal";

    [DataField(customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath OutpostMap = new("/Maps/nukieplanet.yml");

    [DataField(customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath ShuttleMap = new("/Maps/infiltrator.yml");

    [DataField]
    public WinType WinType = WinType.Neutral;

    [DataField]
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
    [DataField]
    public Dictionary<string, StartingGearPrototype> StartingGearPrototypes = new ();

    /// <summary>
    ///     Cached operator name prototypes.
    /// </summary>
    [DataField]
    public Dictionary<string, List<string>> OperativeNames = new();

    /// <summary>
    ///     Data to be used in <see cref="OnMindAdded"/> for an operative once the Mind has been added.
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, string> OperativeMindPendingData = new();

    /// <summary>
    ///     Players who played as an operative at some point in the round.
    ///     Stores the mind as well as the entity name
    /// </summary>
    [DataField]
    public Dictionary<string, EntityUid> OperativePlayers = new();

    [DataField(required: true)]
    public ProtoId<NpcFactionPrototype> Faction = default!;
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
