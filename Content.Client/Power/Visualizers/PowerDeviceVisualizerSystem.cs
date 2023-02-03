using Content.Shared.Power;
using Robust.Client.GameObjects;

namespace Content.Client.Power;

public sealed class PowerDeviceVisualizerSystem : VisualizerSystem<PowerDeviceVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PowerDeviceVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        
        var powered = AppearanceSystem.TryGetData(uid, PowerDeviceVisuals.Powered, out bool poweredVar, args.Component) && poweredVar;
        args.Sprite.LayerSetVisible(PowerDeviceVisualLayers.Powered, powered);
    }
}

public enum PowerDeviceVisualLayers : byte
{
    Powered
}
