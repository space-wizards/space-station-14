using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation.Components;

[NetworkedComponent]
public abstract class SharedGeigerComponent : Component
{
    [DataField("showExamine")]
    public bool ShowExamine = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public float CurrentRadiation;

    [ViewVariables(VVAccess.ReadOnly)]
    public GeigerDangerLevel DangerLevel = GeigerDangerLevel.None;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsEnabled;
}

[Serializable, NetSerializable]
public sealed class GeigerComponentState : ComponentState
{
    public float CurrentRadiation;
    public GeigerDangerLevel DangerLevel;
    public bool IsEnabled;
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
    Screen
}

[Serializable, NetSerializable]
public enum GeigerVisuals : byte
{
    DangerLevel,
    IsEnabled
}
