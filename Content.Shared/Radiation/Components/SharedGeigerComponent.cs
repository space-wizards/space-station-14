using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation.Components;

[NetworkedComponent]
public abstract class SharedGeigerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public float CurrentRadiation;
}
public enum GeigerDangerLevel
{
    None,
    Low,
    Med,
    High,
    Extreme
}

[Serializable, NetSerializable]
public sealed class GeigerComponentState : ComponentState
{
    public float CurrentRadiation;
}
