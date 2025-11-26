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
    //TODO It's not yet possible for the player to choose from the available options, SolitarySpawningSystem always uses the first one
    // Options should be presented in the lobby in place of the normal Join UI
    [DataField, Required]
    public List<ProtoId<SolitarySpawningPrototype>> Prototypes;
}
