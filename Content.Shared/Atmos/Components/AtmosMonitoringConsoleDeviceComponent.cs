using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AtmosMonitoringConsoleDeviceComponent : Component
{
    /// <summary>
    /// The group that the entity belongs to
    /// </summary>
    [DataField, ViewVariables]
    public AtmosMonitoringConsoleGroup Group;

    /// <summary>
    /// Indicates whether the cardinal facing of this entity affects it nav map sprite
    /// </summary>
    [DataField, ViewVariables]
    public bool Rotatable = false;
}
