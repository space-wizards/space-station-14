using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GasPipeSensorComponent : Component
{

}

[Serializable, NetSerializable]
public enum GasPipeSensorVisuals : byte
{
    State,
    Lights
}

