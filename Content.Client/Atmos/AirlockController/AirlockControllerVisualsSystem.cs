#region

using Content.Shared.Atmos.AirlockController;
using Content.Shared.Power;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

#endregion

namespace Content.Client.Atmos.AirlockController;

public sealed class AirlockControllerVisualsSystem : VisualizerSystem<AirlockControllerVisualsComponent>
{
    private static readonly AirlockControllerVisualLayers[] HideOnDepowered =
    [
        AirlockControllerVisualLayers.State,
        AirlockControllerVisualLayers.Display,
        AirlockControllerVisualLayers.Error,
        AirlockControllerVisualLayers.Light,
    ];

    protected override void OnAppearanceChange(
        EntityUid uid,
        AirlockControllerVisualsComponent comp,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var sprite = (uid, args.Sprite);

        if (!args.AppearanceData.TryGetValue(PowerDeviceVisuals.Powered, out var poweredObject)
            || poweredObject is not bool powered)
        {
            return;
        }

        foreach (var layer in HideOnDepowered)
        {
            if (SpriteSystem.LayerMapTryGet(sprite, layer, out var hidden, false))
                SpriteSystem.LayerSetVisible(sprite, hidden, powered);
        }

        if (!powered)
            return;

        if (AppearanceSystem.TryGetData<AirlockCycleState>(uid, AirlockControllerVisuals.State, out var state, args.Component)
            && comp.StateSprites.TryGetValue(state, out var stateSprite)
            && SpriteSystem.LayerMapTryGet(sprite, AirlockControllerVisualLayers.State, out var stateLayer, false))
        {
            SpriteSystem.LayerSetRsiState(sprite, stateLayer, new RSI.StateId(stateSprite));
        }

        if (AppearanceSystem.TryGetData<AirlockControllerDisplay>(uid, AirlockControllerVisuals.Display, out var display, args.Component)
            && comp.DisplaySprites.TryGetValue(display, out var displaySprite)
            && SpriteSystem.LayerMapTryGet(sprite, AirlockControllerVisualLayers.Display, out var displayLayer, false))
        {
            SpriteSystem.LayerSetRsiState(sprite, displayLayer, new RSI.StateId(displaySprite));
        }

        if (AppearanceSystem.TryGetData<bool>(uid, AirlockControllerVisuals.Error, out var error, args.Component)
            && SpriteSystem.LayerMapTryGet(sprite, AirlockControllerVisualLayers.Error, out var errorLayer, false))
        {
            SpriteSystem.LayerSetVisible(sprite, errorLayer, error);
        }

        if (AppearanceSystem.TryGetData<bool>(uid, AirlockControllerVisuals.Cycling, out var cycling, args.Component)
            && SpriteSystem.LayerMapTryGet(sprite, AirlockControllerVisualLayers.Light, out var lightLayer, false))
        {
            SpriteSystem.LayerSetRsiState(sprite, lightLayer, new RSI.StateId(cycling ? "light_on" : "light_off"));
        }
    }
}
