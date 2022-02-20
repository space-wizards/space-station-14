using System;
using Content.Shared.Atmos.Piping;
using JetBrains.Annotations;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public sealed class PressurePumpVisualizer : EnabledAtmosDeviceVisualizer
    {
        protected override object LayerMap => Layers.Enabled;
        protected override Enum DataKey => PressurePumpVisuals.Enabled;

        enum Layers : byte
        {
            Enabled,
        }
    }
}
