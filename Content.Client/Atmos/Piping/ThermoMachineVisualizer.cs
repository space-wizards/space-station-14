using System;
using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;

namespace Content.Client.Atmos.Piping
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
