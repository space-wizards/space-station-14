using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This handles starting various roundstart variation rules after a station has been loaded.
/// </summary>
[RegisterComponent]
public sealed partial class RoundstartStationVariationRuleComponent : Component
{
    /// <summary>
    ///     The list of rules that will be started once the map is spawned.
    ///     Uses <see cref="EntitySpawnEntry"/> to support probabilities for various rules
    ///     without having to hardcode the probability directly in the rule's logic.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> Rules = new();
}
