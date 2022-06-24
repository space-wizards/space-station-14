using Content.Client.Atmos.Components;
using Content.Shared.Atmos.Piping;

namespace Content.Client.Atmos.EntitySystems;

public sealed class PressurePumpVisualizerSystem : EnabledAtmosDeviceVisualizerSystem<PressurePumpVisualsComponent>
{
    protected override object LayerMap => PressurePumpVisualLayers.Enabled;
    protected override Enum DataKey => PumpVisuals.Enabled;
}

public enum PressurePumpVisualLayers : byte
{
    Enabled,
}
