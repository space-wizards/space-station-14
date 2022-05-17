using Content.Shared.Bed;
using Robust.Client.GameObjects;

namespace Content.Client.Bed
{
    public sealed class StasisBedSystem : VisualizerSystem<StasisBedVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, StasisBedVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (TryComp(uid, out SpriteComponent? sprite)
                && args.Component.TryGetData(StasisBedVisuals.IsOn, out bool isOn))
            {
                sprite.LayerSetVisible(StasisBedVisualLayers.IsOn, isOn);
            }
        }
    }

    public enum StasisBedVisualLayers : byte
    {
        IsOn,
    }
}
