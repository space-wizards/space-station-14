using System;
using Content.Shared.Atmos.Piping;
using JetBrains.Annotations;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public class ThermoMachineVisualizer : EnabledAtmosDeviceVisualizer
    {
        protected override object LayerMap => Layers.Enabled;
        protected override Enum DataKey => ThermoMachineVisuals.Enabled;

        enum Layers
        {
            Enabled,
        }
    }
}
