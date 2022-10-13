using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation.Components;

[NetworkedComponent]
public abstract class SharedGeigerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public float CurrentRadiation;
}

[Serializable, NetSerializable]
public sealed class GeigerComponentState : ComponentState
{
    public float CurrentRadiation;
}

[Serializable, NetSerializable]
public enum GeigerDangerLevel : byte
{
    None,
    Low,
    Med,
    High,
    Extreme
}

[Serializable, NetSerializable]
public enum GeigerLayers : byte
{
    Base,
    Screen
}

[Serializable, NetSerializable]
public enum GeigerVisuals : byte
{
    DangerLevel
}
