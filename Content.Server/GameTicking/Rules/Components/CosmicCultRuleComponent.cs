using Content.Server.RoundEnd;
using Content.Shared.EntityTable;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule component for handling the Cosmic Cult antagonist.
/// </summary>
[RegisterComponent, Access(typeof(CosmicCultRuleSystem))]
[AutoGenerateComponentPause]
public sealed partial class CosmicCultRuleComponent : Component
{
    /// <summary>
    /// What happens if all the cultists die.
    /// </summary>
    [DataField] public RoundEndBehavior RoundEndBehavior = RoundEndBehavior.ShuttleCall;

    /// <summary>
    /// Sender for shuttle call.
    /// </summary>
    [DataField] public LocId RoundEndTextSender = "comms-console-announcement-title-centcom";

    /// <summary>
    /// Text for shuttle call.
    /// </summary>
    [DataField] public LocId RoundEndTextShuttleCall = "cosmiccult-elimination-shuttle-call";

    /// <summary>
    /// Text for announcement.
    /// </summary>
    [DataField] public LocId RoundEndTextAnnouncement = "cosmiccult-elimination-announcement";


    /// <summary>
    /// Time for emergency shuttle arrival.
    /// </summary>
    [DataField] public TimeSpan EvacShuttleTime = TimeSpan.FromMinutes(2);

    [DataField] public HashSet<EntityUid> Cultists = [];

    /// <summary>
    /// The grid EntityUid of the station Cosmic Cult is active on.
    /// </summary>
    [DataField] public EntityUid StationGrid;

    /// <summary>
    /// The grid EntityUid of the station Cosmic Cult is active on.
    /// </summary>
    [DataField] public MapId? VoidMapId;

    /// <summary>
    ///     Is the finale's set-up step done?
    /// </summary>
    [DataField] public bool FinaleSetup;

    /// <summary>
    ///     Amount of present crew.
    /// </summary>
    [DataField] public int TotalCrew;

    /// <summary>
    ///     Amount of cultists.
    /// </summary>
    [DataField] public int TotalCult;

    /// <summary>
    ///     Current "Tier" of the cult.
    /// </summary>
    [DataField] public int Tier = 1;

    /// <summary>
    ///     How much progress the cult has.
    /// </summary>
    [DataField] public double Progress;

    /// <summary>
    ///     Percentage of crew that have been converted into cultists.
    /// </summary>
    [ViewVariables] public float PortionConverted => (float)TotalCult / (float)TotalCrew;

    /// <summary>
    ///     How much entropy has been siphoned by the cult.
    /// </summary>
    [DataField] public int EntropySiphoned;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? CultWinTimer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? FinaleTimer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? Tier3Timer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? Tier2Timer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? RiftTimer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? StigmaTimer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? BreachTimer;

    [DataField] public EntityUid? GoalsContainer;

    [DataField] public ProtoId<EntityTablePrototype> Goals = "CosmicCultGoals";

    [DataField] public bool CultWin = false;

    [DataField] public SoundSpecifier FinaleMusic = new SoundPathSpecifier("/Audio/Cosmic/finale.ogg");
}
