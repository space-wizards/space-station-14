using Content.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Entities with this component appear on the 
/// nav maps of atmos monitoring consoles
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AtmosMonitoringConsoleDeviceComponent : Component
{
    /// <summary>
    /// Prototype ID for the blip used to represent this
    /// entity on the atmos monitoring console nav map.
    /// If null, no blip is drawn (i.e., null for pipes)
    /// </summary>
    [DataField, ViewVariables]
    public ProtoId<NavMapBlipPrototype>? NavMapBlip = null;
}
