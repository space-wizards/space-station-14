using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Engineering.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpawnAfterInteractComponent : Component
{
    [DataField(required: true)]
    public EntProtoId? Prototype { get; private set; }

    [DataField]
    public bool IgnoreDistance { get; private set; }

    [DataField("doAfter")]
    public float DoAfterTime = 0;

    [DataField]
    public bool RemoveOnInteract;
}
