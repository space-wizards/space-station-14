using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CableVisComponent : Component
{
    [DataField(required: true)]
    public string Node;
}
