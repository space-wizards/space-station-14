using Robust.Shared.GameStates;

namespace Content.Shared.GameTicking.Components;

/// <summary>
/// Map-placed component to automatically add and/or start specific game rules
/// when the round progresses. Place on Map Entity in Resources/Maps.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MapAutoGameRuleComponent : Component
{
    /// <summary>
    /// List of game rule prototype IDs to add/start.
    /// </summary>
    [DataField("rules")] public List<string> Rules = new();

    /// <summary>
    /// Add the rules during PreRoundLobby. This queues them as 'added' before round start.
    /// </summary>
    [DataField("addInLobby")] public bool AddInLobby = true;

    /// <summary>
    /// Start the rules when entering InRound. If true, will call StartGameRule for each rule on round start.
    /// </summary>
    [DataField("startOnRoundStart")] public bool StartOnRoundStart = true;
}
