using Content.Shared.Bed;
using Robust.Client.GameObjects;

namespace Content.Client.Bed;

public sealed class StasisBedSystem : VisualizerSystem<StasisBedVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, StasisBedVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null
            && AppearanceSystem.TryGetData<bool>(uid, StasisBedVisuals.IsOn, out var isOn, args.Component))
        {
            args.Sprite.LayerSetVisible(StasisBedVisualLayers.IsOn, isOn);
        }
    }
}

public enum StasisBedVisualLayers : byte
{
    IsOn,
}
