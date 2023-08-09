using Content.Shared.Sticky.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Sticky.Visualizers;

public sealed class StickyVisualizerSystem : VisualizerSystem<StickyVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StickyVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, StickyVisualizerComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        component.DefaultDrawDepth = sprite.DrawDepth;
    }

    protected override void OnAppearanceChange(EntityUid uid, StickyVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, StickyVisuals.IsStuck, out var isStuck, args.Component))
            return;

        var drawDepth = isStuck ? component.StuckDrawDepth : component.DefaultDrawDepth;
        args.Sprite.DrawDepth = drawDepth;

    }
}
