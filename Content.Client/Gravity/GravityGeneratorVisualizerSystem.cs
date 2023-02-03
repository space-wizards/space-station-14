using Content.Shared.Gravity;
using Robust.Client.GameObjects;

namespace Content.Client.Gravity;

public sealed class GravityGeneratorVisualizerSystem : VisualizerSystem<GravityGeneratorVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GravityGeneratorVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, GravityGeneratorVisualizerComponent comp, ComponentInit args)
    {
        var sprite = Comp<SpriteComponent>(uid);
        sprite.LayerMapReserveBlank(GravityGeneratorVisualLayers.Base);
        sprite.LayerMapReserveBlank(GravityGeneratorVisualLayers.Core);
    }

    protected override void OnAppearanceChange(EntityUid uid, GravityGeneratorVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData(uid, GravityGeneratorVisuals.State, out GravityGeneratorStatus state, args.Component))
        {
            if (comp.SpriteMap.TryGetValue(state, out var spriteState))
            {
                var layer = args.Sprite.LayerMapGet(GravityGeneratorVisualLayers.Base);
                args.Sprite.LayerSetState(layer, spriteState);
            }
        }

        if (AppearanceSystem.TryGetData(uid, GravityGeneratorVisuals.Charge, out float charge, args.Component))
        {
            var layer = args.Sprite.LayerMapGet(GravityGeneratorVisualLayers.Core);
            switch (charge)
            {
                case < 0.2f:
                    args.Sprite.LayerSetVisible(layer, false);
                    break;
                case >= 0.2f and < 0.4f:
                    args.Sprite.LayerSetVisible(layer, true);
                    args.Sprite.LayerSetState(layer, "startup");
                    break;
                case >= 0.4f and < 0.6f:
                    args.Sprite.LayerSetVisible(layer, true);
                    args.Sprite.LayerSetState(layer, "idle");
                    break;
                case >= 0.6f and < 0.8f:
                    args.Sprite.LayerSetVisible(layer, true);
                    args.Sprite.LayerSetState(layer, "activating");
                    break;
                default:
                    args.Sprite.LayerSetVisible(layer, true);
                    args.Sprite.LayerSetState(layer, "activated");
                    break;
            }
        }
    }
}

public enum GravityGeneratorVisualLayers : byte
{
    Base,
    Core
}
