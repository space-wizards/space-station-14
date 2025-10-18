using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Gamerule component to mess up ai/borg laws when started.
/// </summary>
[RegisterComponent]
public sealed partial class IonStormRuleComponent : Component
{
    /// <summary>
    /// Which formats (and with what weights) will be applied to newly generated corrupted laws.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype>? CorruptedLawFormattings;
}
