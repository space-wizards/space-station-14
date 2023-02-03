using Content.Shared.Shuttles.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Shuttles;

public sealed class ThrusterVisualizerSystem : VisualizerSystem<ThrusterVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ThrusterVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if(!AppearanceSystem.TryGetData(uid, ThrusterVisualState.State, out bool state, args.Component))
            return;

        switch (state)
        {
            case true:
                args.Sprite.LayerSetVisible(ThrusterVisualLayers.ThrustOn, true);

                if (AppearanceSystem.TryGetData(uid, ThrusterVisualState.Thrusting, out bool thrusting, args.Component) && thrusting)
                {
                    if (args.Sprite.LayerMapTryGet(ThrusterVisualLayers.Thrusting, out _))
                    {
                        args.Sprite.LayerSetVisible(ThrusterVisualLayers.Thrusting, true);
                    }

                    if (args.Sprite.LayerMapTryGet(ThrusterVisualLayers.ThrustingUnshaded, out _))
                    {
                        args.Sprite.LayerSetVisible(ThrusterVisualLayers.ThrustingUnshaded, true);
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
        if (sprite.LayerMapTryGet(ThrusterVisualLayers.Thrusting, out _))
        {
            sprite.LayerSetVisible(ThrusterVisualLayers.Thrusting, false);
        }

        if (sprite.LayerMapTryGet(ThrusterVisualLayers.ThrustingUnshaded, out _))
        {
            sprite.LayerSetVisible(ThrusterVisualLayers.ThrustingUnshaded, false);
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
