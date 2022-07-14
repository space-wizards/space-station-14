using Content.Shared.Morgue;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Morgue.Visualizers;

public sealed class CrematoriumVisualizerSystem : VisualizerSystem<CrematoriumVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void OnAppearanceChange(EntityUid uid, CrematoriumVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        string? lightState = null;
        if (args.Component.TryGetData(CrematoriumVisuals.Burning, out bool isBurning) && isBurning)
            lightState = component.LightBurning;
        else if (args.Component.TryGetData(StorageVisuals.HasContents, out bool hasContents) && hasContents)
            lightState = component.LightContents;

        if (lightState != null)
        {
            args.Sprite.LayerSetState(CrematoriumVisualLayers.Light, lightState);
            args.Sprite.LayerSetVisible(CrematoriumVisualLayers.Light, true);
        }
        else
        {
            args.Sprite.LayerSetVisible(CrematoriumVisualLayers.Light, false);
        }
    }
}
