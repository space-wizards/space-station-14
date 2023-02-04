using Content.Shared.Shuttles.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Shuttles;

public sealed class ThrusterVisualizerSystem : VisualizerSystem<ThrusterVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ThrusterVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if(!AppearanceSystem.TryGetData<bool>(uid, ThrusterVisualState.State, out var state, args.Component))
            return;

        switch (state)
        {
            case true:
                args.Sprite.LayerSetVisible(ThrusterVisualLayers.ThrustOn, true);

                if (AppearanceSystem.TryGetData<bool>(uid, ThrusterVisualState.Thrusting, out var thrusting, args.Component) && thrusting)
                {
                    if (args.Sprite.LayerMapTryGet(ThrusterVisualLayers.Thrusting, out var thrustingLayer))
                    {
                        args.Sprite.LayerSetVisible(thrustingLayer, true);
                    }

                    if (args.Sprite.LayerMapTryGet(ThrusterVisualLayers.ThrustingUnshaded, out var unshadedLayer))
                    {
                        args.Sprite.LayerSetVisible(unshadedLayer, true);
                    }
                }
                else
                {
                    DisableThrusting(uid, args.Component, args.Sprite);
                }

                break;
            case false:
                args.Sprite.LayerSetVisible(ThrusterVisualLayers.ThrustOn, false);
                DisableThrusting(uid, args.Component, args.Sprite);
                break;
        }
    }

    private void DisableThrusting(EntityUid uid, AppearanceComponent appearance, SpriteComponent sprite)
    {
        if (sprite.LayerMapTryGet(ThrusterVisualLayers.Thrusting, out var thrustingLayer))
        {
            sprite.LayerSetVisible(thrustingLayer, false);
        }

        if (sprite.LayerMapTryGet(ThrusterVisualLayers.ThrustingUnshaded, out var unshadedLayer))
        {
            sprite.LayerSetVisible(unshadedLayer, false);
        }
    }
}

public enum ThrusterVisualLayers : byte
{
    Base,
    ThrustOn,
    Thrusting,
    ThrustingUnshaded,
}
