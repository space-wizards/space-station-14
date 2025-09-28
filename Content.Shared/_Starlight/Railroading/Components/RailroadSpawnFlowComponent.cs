using Content.Shared.Destructible.Thresholds;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RailroadSpawnFlowComponent : Component
{
    [DataField]
    public MinMax Count = new(1, 1);

    [DataField]
    public float Probability = 1f;

    [DataField]
    public ProtoId<JobPrototype>? JobPrototype;
}
