using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Gamerule component to mess up ai/borg laws when started.
/// </summary>
[RegisterComponent]
public sealed partial class IonStormRuleComponent : Component
{
    [DataField]
    public ProtoId<WeightedRandomPrototype>? LawFormatCorruption;
}
