using Content.Shared.NPC.Prototypes;
using Content.Shared.Radio;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(SuspicionRuleSystem))]
public sealed partial class SuspicionRuleComponent : Component
{
    #region State management

    public SuspicionGameState GameState = SuspicionGameState.Preparing;

    /// <summary>
    /// Defines when the round will end.
    /// </summary>
    public TimeSpan EndAt = TimeSpan.MinValue;

    public List<int> AnnouncedTimeLeft = new List<int>();

    #endregion

    /// <summary>
    /// Percentage of total players that will be a traitor.
    /// The number of players will be multiplied by this number, and then rounded down.
    /// If the result is less than 1 or more than the player count, it is clamped to those values.
    /// </summary>
    [DataField]
    public float TraitorPercentage = 0.25f;

    /// <summary>
    /// Percentage of total players that will be a detective (detective innocent). Handled similar to traitor percentage (rounded down etc).
    /// </summary>
    [DataField]
    public float DetectivePercentage = 0.13f;

    /// <summary>
    /// How long to wait before the game starts after the round starts.
    /// </summary>
    [DataField]
    public int PreparingDuration = 30;

    /// <summary>
    /// How long the round lasts in seconds.
    /// </summary>
    [DataField]
    public int RoundDuration = 480;

    /// <summary>
    /// How long to add to the round time when a player is killed.
    /// </summary>
    [DataField]
    public int TimeAddedPerKill = 30;

    /// <summary>
    /// How long to wait before restarting the round after the summary is displayed.
    /// </summary>
    [DataField]
    public int PostRoundDuration = 30;

    /// <summary>
    /// The gear all players spawn with.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string Gear = "SuspicionGear";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string UplinkImplant = "SusTraitorUplinkImplant";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string DetectiveImplant = "SusDetectiveUplinkImplant";


    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
    public string TraitorRadio = "Syndicate";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<NpcFactionPrototype>))]
    public string TraitorFaction = "Syndicate";

    /// <summary>
    /// How much TC to give to traitors/detectives for their performance
    /// </summary>
    [DataField]
    public int AmountAddedPerKill = 1;

}

public enum SuspicionGameState
{
    /// <summary>
    /// The game is preparing to start. No roles have been assigned yet and new joining players will be spawned in.
    /// </summary>
    Preparing,

    /// <summary>
    /// The game is in progress. Roles have been assigned and players are hopefully killing each other. New joining players will be forced to spectate.
    /// </summary>
    InProgress,

    /// <summary>
    /// The game has ended. The summary is being displayed and players are waiting for the round to restart.
    /// </summary>
    PostRound
}
