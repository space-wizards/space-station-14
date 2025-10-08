using Content.Server.StationEvents.Events;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(MadnessEventRule))]
public sealed partial class MadnessEventRuleComponent : Component
{
    [DataField("chanceOfMadness")]
    public float ChanceOfMadness = 0.30f;
}
