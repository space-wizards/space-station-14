using Content.Client.Atmos.Components;
using Content.Shared.Atmos.Piping;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.EntitySystems;

public sealed class GasValveVisualizerSystem : EnabledAtmosDeviceVisualizerSystem<GasValveVisualsComponent>
{
    protected override object LayerMap => GasValveVisualLayers.Enabled;
    protected override Enum DataKey => FilterVisuals.Enabled;
}

public enum GasValveVisualLayers : byte
{
    Enabled,
}
