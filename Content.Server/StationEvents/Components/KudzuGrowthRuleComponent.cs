using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(KudzuGrowthRule))]
public sealed partial class KudzuGrowthRuleComponent : Component
{
    [DataField]
    public EntProtoId Spawn = "Kudzu";
}
