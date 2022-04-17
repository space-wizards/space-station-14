using Content.Client.Wall;
using Content.Client.Wall.Components;
using Robust.Client.GameObjects;

namespace Content.Client.IconSmoothing;

public sealed partial class IconSmoothSystem
{
    private void OnReinforcedStartup(EntityUid uid, ReinforcedWallComponent component, ComponentStartup args)
    {
        UpdateSmoothPos(uid, component);

        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            var state0 = $"{component.ReinforcedStateBase}0";
            sprite.LayerMapSet(ReinforcedCornerLayers.SE, sprite.AddLayerState(state0));
            sprite.LayerSetDirOffset(ReinforcedCornerLayers.SE, SpriteComponent.DirectionOffset.None);
            sprite.LayerMapSet(ReinforcedCornerLayers.NE, sprite.AddLayerState(state0));
            sprite.LayerSetDirOffset(ReinforcedCornerLayers.NE, SpriteComponent.DirectionOffset.CounterClockwise);
            sprite.LayerMapSet(ReinforcedCornerLayers.NW, sprite.AddLayerState(state0));
            sprite.LayerSetDirOffset(ReinforcedCornerLayers.NW, SpriteComponent.DirectionOffset.Flip);
            sprite.LayerMapSet(ReinforcedCornerLayers.SW, sprite.AddLayerState(state0));
            sprite.LayerSetDirOffset(ReinforcedCornerLayers.SW, SpriteComponent.DirectionOffset.Clockwise);
            sprite.LayerMapSet(ReinforcedWallVisualLayers.Deconstruction, sprite.AddBlankLayer());
        }
    }
}
