using Content.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AtmosMonitoringConsoleDeviceComponent : Component
{
    /// <summary>
    /// Prototype ID for the nav map blip
    /// </summary>
    [DataField, ViewVariables]
    public ProtoId<NavMapBlipPrototype> NavMapBlip { get; private set; }
}
