using Content.Shared.Nutrition.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Nutrition.Visualizers;

public sealed class CreamPiedVisualizerSystem : VisualizerSystem<CreamPiedVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CreamPiedVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, CreamPiedVisualizerComponent comp, ComponentInit args)
    {
        if(!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerMapReserveBlank(CreamPiedVisualLayers.Pie);
        sprite.LayerSetRSI(CreamPiedVisualLayers.Pie, "Effects/creampie.rsi");
        sprite.LayerSetVisible(CreamPiedVisualLayers.Pie, false);
    }

    protected override void OnAppearanceChange(EntityUid uid, CreamPiedVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if(!AppearanceSystem.TryGetData<bool>(uid, CreamPiedVisuals.Creamed, out var pied, args.Component))
            return;
        
        args.Sprite.LayerSetVisible(CreamPiedVisualLayers.Pie, pied);
        args.Sprite.LayerSetState(CreamPiedVisualLayers.Pie, comp.State);
    }
}

public enum CreamPiedVisualLayers : byte
{
    Pie,
}
