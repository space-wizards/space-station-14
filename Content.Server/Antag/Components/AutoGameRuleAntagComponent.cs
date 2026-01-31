using Content.Server.Antag.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag.Components;

/// <summary>
/// Creates a game rule and makes the object an antagonist from the game rule,
/// either instantly if it has a mind, or when the mind is added.
/// </summary>
[RegisterComponent, Access(typeof(AutoGameRuleAntagSystem))]
public sealed partial class AutoGameRuleAntagComponent : Component
{
    /// <summary>
    /// The GameRule
    /// </summary>
    [DataField]
    public EntProtoId GameRule;
}
