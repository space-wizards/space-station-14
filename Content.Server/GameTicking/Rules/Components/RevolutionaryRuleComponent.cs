using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Component for the RevolutionaryRuleSystem that stores info about winning/losing, player counts required for starting, as well as prototypes for Revolutionaries and their gear.
/// </summary>
[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))]
public sealed partial class RevolutionaryRuleComponent : Component
{
    /// <summary>
    /// When the round will if all the command are dead (Incase they are in space)
    /// </summary>
    [DataField("commandCheck", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan CommandCheck;

    /// <summary>
    /// The amount of time between each check for command check.
    /// </summary>
    [DataField("timerWait")]
    public TimeSpan TimerWait = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Stores players minds
    /// </summary>
    [DataField("headRevs")]
    public Dictionary<string, EntityUid> HeadRevs = new();

    [DataField("headRevPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string HeadRevPrototypeId = "HeadRev";

    [DataField("headRevGearPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string HeadRevGearPrototypeId = "HeadRevGear";

    [DataField("revPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string RevPrototypeId = "Rev";

    /// <summary>
    /// Sound that plays when you are chosen as Rev. (Placeholder until I find something cool I guess)
    /// </summary>
    [DataField("headRevStartSound")]
    public SoundSpecifier HeadRevStartSound = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");

    /// <summary>
    /// Min players needed for Revolutionary gamemode to start.
    /// </summary>
    [DataField("minPlayers")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int MinPlayers = 15;

    /// <summary>
    /// Max Head Revs allowed during selection.
    /// </summary>
    [DataField("maxHeadRevs")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int MaxHeadRevs = 3;

    /// <summary>
    /// The amount of Head Revs that will spawn per this amount of players.
    /// </summary>
    [DataField("playersPerHeadRev")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int PlayersPerHeadRev = 15;
}
