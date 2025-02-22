using Robust.Client.GameObjects;
using Content.Shared._Impstation.CosmicCult.Components;

namespace Content.Client._Impstation.CosmicCult;

/// <summary>
/// Visualizer for The Monument of the Cosmic Cult.
/// </summary>
public sealed class MonumentVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MonumentComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(EntityUid uid, MonumentComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        args.Sprite.LayerMapTryGet(MonumentVisualLayers.TransformLayer, out var transformLayer);
        args.Sprite.LayerMapTryGet(MonumentVisualLayers.MonumentLayer, out var baseLayer);
        _appearance.TryGetData<bool>(uid, MonumentVisuals.Transforming, out var transforming, args.Component);
        _appearance.TryGetData<bool>(uid, MonumentVisuals.Tier3, out var tier3, args.Component);
        if (!tier3)
            args.Sprite.LayerSetState(transformLayer, "transform-stage2");
        else
            args.Sprite.LayerSetState(transformLayer, "transform-stage3");
        if (transforming && HasComp<MonumentTransformingComponent>(uid))
        {
            args.Sprite.LayerSetAnimationTime(transformLayer, 0f);
            args.Sprite.LayerSetVisible(transformLayer, true);
            args.Sprite.LayerSetVisible(baseLayer, false);
        }
        else
        {
            args.Sprite.LayerSetVisible(transformLayer, false);
            args.Sprite.LayerSetVisible(baseLayer, true);
        }
    }
}
