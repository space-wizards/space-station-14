using Content.Client.Atmos.Components;
using Content.Shared.Atmos.Piping;

namespace Content.Client.Atmos.EntitySystems;

public sealed class GasFilterVisualizerSystem : EnabledAtmosDeviceVisualizerSystem<GasFilterVisualsComponent>
{
    protected override object LayerMap => GasFilterVisualLayers.Enabled;
    protected override Enum DataKey => FilterVisuals.Enabled;
}

public enum GasFilterVisualLayers : byte
{
    Enabled,
}
