using System;
using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;

namespace Content.Client.GameObjects.Components.Atmos.Piping
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
