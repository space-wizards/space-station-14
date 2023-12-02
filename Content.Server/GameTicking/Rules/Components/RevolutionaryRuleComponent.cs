using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Component for the RevolutionaryRuleSystem that stores info about winning/losing, player counts required for starting, as well as prototypes for Revolutionaries and their gear.
/// </summary>
[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))]
public sealed partial class RevolutionaryRuleComponent : Component
{
    /// <summary>
    /// When the round will end if all the command or head revs are dead (or exiled)
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan RoundCheck;

    /// <summary>
    /// The amount of time between each check for command check.
    /// </summary>
    [DataField]
    public TimeSpan TimerWait = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Stores players minds
    /// </summary>
    [DataField]
    public Dictionary<string, EntityUid> HeadRevs = new();

    [DataField]
    public ProtoId<AntagPrototype> HeadRevPrototypeId = "HeadRev";

    [DataField]
    public ProtoId<AntagPrototype> RevPrototypeId = "Rev";

    /// <summary>
    /// Sound that plays when you are chosen as Rev. (Placeholder until I find something cool I guess)
    /// </summary>
    [DataField]
    public SoundSpecifier HeadRevStartSound = new SoundPathSpecifier("/Audio/Ambience/Antag/headrev_start.ogg");

    /// <summary>
    /// Min players needed for Revolutionary gamemode to start.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MinPlayers = 15;

    /// <summary>
    /// Max Head Revs allowed during selection.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxHeadRevs = 3;

    /// <summary>
    /// The amount of Head Revs that will spawn per this amount of players.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int PlayersPerHeadRev = 15;

    /// <summary>
    /// The gear head revolutionaries are given on spawn.
    /// </summary>
    [DataField]
    public List<EntProtoId> StartingGear = new()
    {
        "Flash",
        "ClothingEyesGlassesSunglasses"
    };

    /// <summary>
    /// The time it takes after the last head is killed for the shuttle to arrive.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ShuttleCallTime = TimeSpan.FromMinutes(3);

    /// <summary>
    /// The text that will be sent to the shuttle when it is called.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string ShuttleCallText = "rev-heads-were-killed-shuttle-call-text";

    /// <summary>
    /// Sender of the shuttle call text.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string ShuttleCallTextSender = "comms-console-announcement-title-centcom";


}
