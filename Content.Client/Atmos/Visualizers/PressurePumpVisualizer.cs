using System;
using Content.Shared.Atmos.Piping;
using JetBrains.Annotations;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public sealed class PressurePumpVisualizer : EnabledAtmosDeviceVisualizer
    {
        protected override object LayerMap => Layers.Enabled;
        protected override Enum DataKey => PumpVisuals.Enabled;

        enum Layers : byte
        {
            Enabled,
        }
    }
}
