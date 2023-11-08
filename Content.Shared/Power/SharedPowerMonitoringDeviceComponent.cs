using Robust.Shared.GameStates;

namespace Content.Shared.Power;

/// <summary>
///     Used to flag any entities that should appear on a power monitoring console
/// </summary>
[NetworkedComponent]
public abstract partial class SharedPowerMonitoringDeviceComponent : Component
{
    /// <summary>
    ///    Determines what power monitoring group this entity should belong to 
    /// </summary>
    [DataField("group", required: true)]
    public PowerMonitoringConsoleGroup Group;
}
