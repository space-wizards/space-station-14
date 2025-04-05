using Content.Shared.Silicons;
using Robust.Client.GameObjects;

namespace Content.Client.Silicons;

public sealed class RobotFenceSystem : VisualizerSystem<RobotFenceVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RobotFenceVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null
            && AppearanceSystem.TryGetData<bool>(uid, RobotFenceVisuals.IsOn, out var isOn, args.Component))
        {
            args.Sprite.LayerSetVisible(RobotFenceVisualLayers.IsOn, isOn);
        }
    }
}

public enum RobotFenceVisualLayers : byte
{
    IsOn,
}
