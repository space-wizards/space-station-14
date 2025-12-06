using System.ComponentModel.DataAnnotations;
using Content.Server.GameTicking.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// When this game rule is active, each player joining the round will spawn on their own solitary map.
/// </summary>
[RegisterComponent]
public sealed partial class SolitarySpawningRuleComponent : Component
{
    /// <summary>
    /// The list of spawn profiles available. The lobby can be configured to allow the player to pick one of the options.
    /// If no player choice is made (or possible), the first prototoype will be chosen.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<SolitarySpawningPrototype>> Prototypes;

    //TODO blacklist/whitelist for which player is covered by this rule
    // Possible use cases: a solitary spawning rule that sends blacklisted players to the moon
}
