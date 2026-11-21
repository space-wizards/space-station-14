using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.Power.Components;

/// <summary>
/// allows the input and output breakers of a PowerNetworkBatteryComponent
/// </summary>
[RegisterComponent, Access(typeof(PowerDeviceSignalControlSystem))]
public sealed partial class BatterySignalControlComponent : Component
{
    /// <summary>
    /// the port that toggles whether the battery can charge when invoked
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> ToggleInputPort = "ToggleInput";

    /// <summary>
    /// the port that toggles whether the battery can discharge when invoked
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> ToggleOutputPort = "ToggleOutput";
}
