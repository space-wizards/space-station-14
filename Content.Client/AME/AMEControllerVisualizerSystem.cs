using Content.Client.AME.Components;
using Robust.Client.GameObjects;
using static Content.Shared.AME.SharedAMEControllerComponent;

namespace Content.Client.AME;

public sealed class AMEControllerVisualizerSystem : VisualizerSystem<AMEControllerVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AMEControllerVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, AMEControllerVisualsComponent component, ComponentInit args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.LayerMapSet(AMEControllerVisualLayers.Display, sprite.AddLayerState("control_on"));
            sprite.LayerSetVisible(AMEControllerVisualLayers.Display, false);
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, AMEControllerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (args.Sprite == null
            || !AppearanceSystem.TryGetData<string>(uid, AMEControllerVisuals.DisplayState, out var state, args.Component))
        {
            return;
        }

        switch (state)
        {
            case "on":
                args.Sprite.LayerSetState(AMEControllerVisualLayers.Display, "control_on");
                args.Sprite.LayerSetVisible(AMEControllerVisualLayers.Display, true);
                break;
            case "critical":
                args.Sprite.LayerSetState(AMEControllerVisualLayers.Display, "control_critical");
                args.Sprite.LayerSetVisible(AMEControllerVisualLayers.Display, true);
                break;
            case "fuck":
                args.Sprite.LayerSetState(AMEControllerVisualLayers.Display, "control_fuck");
                args.Sprite.LayerSetVisible(AMEControllerVisualLayers.Display, true);
                break;
            case "off":
                args.Sprite.LayerSetVisible(AMEControllerVisualLayers.Display, false);
                break;
            default:
                args.Sprite.LayerSetVisible(AMEControllerVisualLayers.Display, false);
                break;
        }
    }
}

public enum AMEControllerVisualLayers : byte
{
    Display
}

