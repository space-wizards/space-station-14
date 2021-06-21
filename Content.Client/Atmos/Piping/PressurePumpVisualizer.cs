using System;
using Content.Shared.Atmos.Piping;
using JetBrains.Annotations;

namespace Content.Client.Atmos.Piping
{
    [UsedImplicitly]
    public class PressurePumpVisualizer : EnabledAtmosDeviceVisualizer
    {
        protected override object LayerMap => Layers.Enabled;
        protected override Enum DataKey => PressurePumpVisuals.Enabled;

        enum Layers
        {
            Enabled,
        }
    }
}
