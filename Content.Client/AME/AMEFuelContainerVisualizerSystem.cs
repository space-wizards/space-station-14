using Content.Client.AME.Components;
using Content.Shared.AME;
using Robust.Client.GameObjects;

namespace Content.Client.AME;
public sealed class AMEFuelContainerVisualizerSystem : VisualizerSystem<AMEFuelContainerVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, AMEFuelContainerVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.Component.TryGetData(AMEFuelContainerVisuals.IsOpen, out bool isOpen))
        {
            args.Sprite.LayerSetVisible(AMEFuelContainerVisualLayers.Seal, !isOpen);
        }
    }

    public enum AMEFuelContainerVisualLayers
    {
        Seal
    }
}

