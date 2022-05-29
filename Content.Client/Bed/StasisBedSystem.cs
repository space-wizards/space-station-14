using Content.Shared.Bed;
using Robust.Client.GameObjects;

namespace Content.Client.Bed
{
    public sealed class StasisBedSystem : VisualizerSystem<StasisBedVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, StasisBedVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite != null
                && args.Component.TryGetData(StasisBedVisuals.IsOn, out bool isOn))
            {
                args.Sprite.LayerSetVisible(StasisBedVisualLayers.IsOn, isOn);
            }
        }
    }

    public enum StasisBedVisualLayers : byte
    {
        IsOn,
    }
}
