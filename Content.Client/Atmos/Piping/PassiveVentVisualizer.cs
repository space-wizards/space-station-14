using System;
using Content.Shared.Atmos.Piping;
using JetBrains.Annotations;

namespace Content.Client.Atmos.Piping
{
    [UsedImplicitly]
    public class PassiveVentVisualizer : EnabledAtmosDeviceVisualizer
    {
        protected override object LayerMap => Layers.Enabled;
        protected override Enum DataKey => PassiveVentVisuals.Enabled;

        enum Layers
        {
            Enabled,
        }
    }
}
