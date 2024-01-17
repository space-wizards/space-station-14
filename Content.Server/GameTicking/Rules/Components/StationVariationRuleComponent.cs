using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class StationVariationRuleComponent : Component
{
    /// <summary>
    ///     The list of rules that will be started once the map is spawned.
    /// </summary>
    [DataField(required: true)]
    public HashSet<EntProtoId> Rules = new();
}
