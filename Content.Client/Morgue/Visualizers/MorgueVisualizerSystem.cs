using Content.Shared.Morgue;
using Content.Shared.Storage;
using Robust.Client.GameObjects;

namespace Content.Client.Morgue.Visualizers;

public sealed class MorgueVisualizerSystem : VisualizerSystem<MorgueVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void OnAppearanceChange(EntityUid uid, MorgueVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        string? lightState = null;
        if (args.Component.TryGetData(MorgueVisuals.HasSoul, out bool hasSoul) && hasSoul)
            lightState = component.LightSoul;
        else if (args.Component.TryGetData(MorgueVisuals.HasMob, out bool hasMob) && hasMob)
            lightState = component.LightMob;
        else if (args.Component.TryGetData(StorageVisuals.HasContents, out bool hasContents) && hasContents)
            lightState = component.LightContents;

        if (lightState != null)
        {
            args.Sprite.LayerSetState(MorgueVisualLayers.Light, lightState);
            args.Sprite.LayerSetVisible(MorgueVisualLayers.Light, true);
        }
        else
        {
            args.Sprite.LayerSetVisible(MorgueVisualLayers.Light, false);
        }
    }
}
