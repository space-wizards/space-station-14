using Content.Server.RoundEnd;
using Content.Shared.Dataset;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(NukeopsRuleSystem))]
public sealed partial class NukeopsRuleComponent : Component
{
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
    public TimeSpan EvacShuttleTime = TimeSpan.FromMinutes(3);

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
    public int WarTcAmountPerNukie = 40;

    /// <summary>
    ///     Delay between war declaration and nuke ops arrival on station map. Gives crew time to prepare
    /// </summary>
    [DataField]
    public TimeSpan WarNukieArriveDelay = TimeSpan.FromMinutes(15);

    /// <summary>
    ///     Minimal operatives count for war declaration
    /// </summary>
    [DataField]
    public int WarDeclarationMinOps = 4;

    [DataField]
    public WinType WinType = WinType.Neutral;

    [DataField]
    public List<WinCondition> WinConditions = new ();

    [DataField]
    public EntityUid? TargetStation;

    [DataField]
    public ProtoId<NpcFactionPrototype> Faction = "Syndicate";

    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/nukeops_start.ogg");
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
