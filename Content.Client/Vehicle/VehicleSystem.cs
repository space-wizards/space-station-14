using Robust.Client.GameObjects;
using Content.Shared.Vehicle;

namespace Content.Client.Vehicle
{
    /// <summary>
    /// Controls client-side visuals for
    /// vehicles
    /// </summary>
    public sealed class VehicleSystem : VisualizerSystem<VehicleVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, VehicleVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (TryComp(uid, out SpriteComponent? sprite)
                && args.Component.TryGetData(VehicleVisuals.DrawDepth, out int drawDepth) && sprite != null)
            {
                sprite.DrawDepth = drawDepth;
            }
            if (args.Component.TryGetData(VehicleVisuals.AutoAnimate, out bool autoAnimate))
            {
                sprite?.LayerSetAutoAnimated(VehicleVisualLayers.AutoAnimate, autoAnimate);
            }
        }
    }
}
public enum VehicleVisualLayers : byte
{
    DrawDepth,
    AutoAnimate
}
