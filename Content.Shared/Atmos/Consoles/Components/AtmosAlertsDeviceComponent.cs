using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent]
[Access([])]
public sealed partial class AtmosAlertsDeviceComponent : Component
{
    /// <summary>
    /// The group that the entity belongs to
    /// </summary>
    [DataField, ViewVariables]
    public AtmosAlertsComputerGroup Group;
}
