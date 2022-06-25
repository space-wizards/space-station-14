using Content.Client.Atmos.Components;
using Content.Shared.Atmos.Piping;

namespace Content.Client.Atmos.EntitySystems;

public sealed class ThermoMachineVisualizerSystem : EnabledAtmosDeviceVisualizerSystem<ThermoMachineVisualsComponent>
{
    protected override object LayerMap => ThermoMachineVisualLayers.Enabled;
    protected override Enum DataKey => ThermoMachineVisuals.Enabled;
}

public enum ThermoMachineVisualLayers : byte
{
    Enabled,
}
