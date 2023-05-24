using Content.Client.AME.Components;
using Robust.Client.GameObjects;
using static Content.Shared.AME.SharedAMEControllerComponent;

namespace Content.Client.AME;

public sealed class AmeControllerVisualizerSystem : VisualizerSystem<AmeControllerVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, AmeControllerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (args.Sprite == null
        || !AppearanceSystem.TryGetData<AmeControllerState>(uid, AmeControllerVisuals.DisplayState, out var state, args.Component))
            return;

        switch (state)
        {
            case AmeControllerState.On:
                args.Sprite.LayerSetState(AmeControllerVisualLayers.Display, component.StateOn);
                args.Sprite.LayerSetVisible(AmeControllerVisualLayers.Display, true);
                break;
            case AmeControllerState.Critical:
                args.Sprite.LayerSetState(AmeControllerVisualLayers.Display, component.StateCritical);
                args.Sprite.LayerSetVisible(AmeControllerVisualLayers.Display, true);
                break;
            case AmeControllerState.Fuck:
                args.Sprite.LayerSetState(AmeControllerVisualLayers.Display, component.StateFuck);
                args.Sprite.LayerSetVisible(AmeControllerVisualLayers.Display, true);
                break;
            case AmeControllerState.Off:
                args.Sprite.LayerSetVisible(AmeControllerVisualLayers.Display, false);
                break;
            default:
                args.Sprite.LayerSetVisible(AmeControllerVisualLayers.Display, false);
                break;
        }
    }
}

public enum AmeControllerVisualLayers : byte
{
    Display
}

