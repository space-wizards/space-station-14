using System.ComponentModel.DataAnnotations;
using Content.Server.GameTicking.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// When this game rule is active, each spawning player will be rerouted to their own solitary map.
/// </summary>
[RegisterComponent]
public sealed partial class SolitarySpawningRuleComponent : Component
{
    /// <summary>
    /// The list of spawn profiles that the system can pick from.
    /// </summary>
    [DataField, Required]
    public List<ProtoId<SolitarySpawningPrototype>> Prototypes;

    //TODO blacklist/whitelist for which player is covered by this rule
    // Possible use cases: sending blacklisted players to the moon
}
