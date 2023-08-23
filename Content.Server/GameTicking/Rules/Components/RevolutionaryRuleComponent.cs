using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Component for the RevolutionaryRuleSystem that stores info about winning/losing, player counts required for starting, as well as prototypes for Revolutionaries and their gear.
/// </summary>
[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))]
public sealed partial class RevolutionaryRuleComponent : Component
{
    /// <summary>
    /// Stores sessions of Head Revs for end screen.
    /// </summary>
    [DataField("headRevs")]
    public Dictionary<string, string> HeadRevs = new();

    /// <summary>
    /// If all Head Revs die this will be set for the end screen.
    /// </summary>
    [DataField("revsLost")]
    public bool RevsLost = false;

    /// <summary>
    /// If all of command on station dies this will be set for end screen.
    /// </summary>
    [DataField("headsDied")]
    public bool HeadsDied = false;
    /// <summary>
    /// Grace period for Heads to join the game before Revs win by default.
    /// </summary>

    [DataField("gracePeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int GracePeriod = 5;

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
