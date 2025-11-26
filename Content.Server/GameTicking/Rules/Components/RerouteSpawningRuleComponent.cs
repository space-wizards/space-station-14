using System.ComponentModel.DataAnnotations;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// When this game rule is active, each spawning player will be rerouted to their own personal map.
/// </summary>
[RegisterComponent]
public sealed partial class RerouteSpawningRuleComponent : Component
{
    /// <summary>
    /// The list of reroute prototypes to choose from
    /// </summary>
    [DataField, Required]
    public List<string> Prototypes;

    //TODO It's not yet possible for the player to choose from the available options.
    //RerouteSpawningSystem always uses the first one
}
