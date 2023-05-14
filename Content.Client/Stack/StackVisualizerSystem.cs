using Content.Shared.Rounding;
using Content.Shared.Stacks;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Stack;

public sealed class StackVisualizerSystem : VisualizerSystem<StackVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StackVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, StackVisualsComponent comp, ComponentInit args)
    {
        if (comp.IsComposite
            && comp.SpriteLayers.Count > 0
            && TryComp<SpriteComponent?>(uid, out var spriteComponent))
        {
            var spritePath = comp.SpritePath ?? spriteComponent.BaseRSI!.Path!;
            foreach (var sprite in comp.SpriteLayers)
            {
                spriteComponent.LayerMapReserveBlank(sprite);
                spriteComponent.LayerSetSprite(sprite, new SpriteSpecifier.Rsi(spritePath, sprite));
                spriteComponent.LayerSetVisible(sprite, false);
            }
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, StackVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (comp.IsComposite)
        {
            ProcessCompositeSprites(uid, comp, args.Component, args.Sprite);
        }
        else
        {
            ProcessOpaqueSprites(uid, comp, args.Component, args.Sprite);
        }
    }

    private void ProcessOpaqueSprites(EntityUid uid, StackVisualsComponent comp, AppearanceComponent appearance, SpriteComponent spriteComponent)
    {
        // Skip processing if no actual
        if (!AppearanceSystem.TryGetData<int>(uid, StackVisuals.Actual, out var actual, appearance))
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, StackVisuals.MaxCount, out var maxCount, appearance))
            maxCount = comp.SpriteLayers.Count;

        var activeLayer = ContentHelpers.RoundToEqualLevels(actual, maxCount, comp.SpriteLayers.Count);
        spriteComponent.LayerSetState(StackVisualsComponent.IconLayer, comp.SpriteLayers[activeLayer]);
    }

    private void ProcessCompositeSprites(EntityUid uid, StackVisualsComponent comp, AppearanceComponent appearance, SpriteComponent spriteComponent)
    {
        // If hidden, don't render any sprites
        if (AppearanceSystem.TryGetData<bool>(uid, StackVisuals.Hide, out var hide, appearance) && hide)
        {
            foreach (var transparentSprite in comp.SpriteLayers)
            {
                spriteComponent.LayerSetVisible(transparentSprite, false);
            }

            return;
        }

        // Skip processing if no actual/maxCount
        if (!AppearanceSystem.TryGetData<int>(uid, StackVisuals.Actual, out var actual, appearance))
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, StackVisuals.MaxCount, out var maxCount, appearance))
            maxCount = comp.SpriteLayers.Count;


        var activeTill = ContentHelpers.RoundToNearestLevels(actual, maxCount, comp.SpriteLayers.Count);
        for (var i = 0; i < comp.SpriteLayers.Count; i++)
        {
            spriteComponent.LayerSetVisible(comp.SpriteLayers[i], i < activeTill);
        }
    }
}
