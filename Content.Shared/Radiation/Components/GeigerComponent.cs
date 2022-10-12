using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation.Components;

[RegisterComponent, NetworkedComponent]
public sealed class GeigerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public float CurrentRadiation;


}

[Serializable, NetSerializable]
public sealed class GeigerComponentState : ComponentState
{
    public float CurrentRadiation;
}
