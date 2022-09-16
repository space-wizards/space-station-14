using Content.Client.Power;
using Content.Shared.Fax;
using Content.Shared.Power;
using Robust.Client.GameObjects;

namespace Content.Client.Fax;

public sealed class FaxMachineSystem : VisualizerSystem<FaxMachineVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, FaxMachineVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.Component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) &&
            args.Sprite.LayerMapTryGet(PowerDeviceVisualLayers.Powered, out _))
        {
            args.Sprite.LayerSetVisible(PowerDeviceVisualLayers.Powered, powered);
        }
        // TODO: Add all states
    }
}
