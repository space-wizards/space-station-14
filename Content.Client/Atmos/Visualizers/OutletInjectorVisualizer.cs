using System;
using Content.Shared.Atmos.Piping;
using JetBrains.Annotations;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public class OutletInjectorVisualizer : EnabledAtmosDeviceVisualizer
    {
        protected override object LayerMap => Layers.Enabled;
        protected override Enum DataKey => OutletInjectorVisuals.Enabled;

        enum Layers
        {
            Enabled,
        }
    }
}
