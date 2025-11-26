using System.ComponentModel.DataAnnotations;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// When this gamerule is active, each spawning player will be rerouted to their own personal map.
/// Roundstart spawns will have a prototype picked for them, late joiners can choose from the available options.
/// </summary>
[RegisterComponent]
public sealed partial class RerouteSpawningRuleComponent : Component
{
    /// <summary>
    /// The list of reroute prototypes to choose from
    /// </summary>
    [DataField, Required]
    public List<string> Prototypes;
}
