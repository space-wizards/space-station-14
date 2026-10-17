using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.Power.Components;

/// <summary>
/// allows the main breaker of an ApcComponent to be toggled by a device link signal
/// </summary>
[RegisterComponent, Access(typeof(PowerDeviceSignalControlSystem))]
public sealed partial class ApcSignalControlComponent : Component
{
    /// <summary>
    /// the port that toggles the APC main breaker when invoked
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> ToggleOutputPort = "ToggleOutput";
}
