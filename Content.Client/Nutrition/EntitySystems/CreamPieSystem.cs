using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Nutrition.EntitySystems;

public sealed class CreamPieSystem : SharedCreamPieSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CreamPiedComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CreamPiedComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<CreamPiedComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnComponentInit(Entity<CreamPiedComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.Sprite == null
            || !TryComp<SpriteComponent>(ent, out var sprite)
            || !TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        // Add the sprite layer for the cream pied face that is specified in the component.
        _sprite.LayerMapReserve((ent.Owner, sprite), CreamPiedVisualLayer.Key);
        _sprite.LayerSetVisible((ent.Owner, sprite), CreamPiedVisualLayer.Key, false);
        _sprite.LayerSetSprite((ent.Owner, sprite), CreamPiedVisualLayer.Key, ent.Comp.Sprite);

        UpdateAppearance(ent, sprite, appearance);
    }

    private void OnComponentShutdown(Entity<CreamPiedComponent> ent, ref ComponentShutdown args)
    {
        _sprite.RemoveLayer(ent.Owner, CreamPiedVisualLayer.Key);
    }

    private void OnAppearanceChange(Entity<CreamPiedComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null)
            UpdateAppearance(ent, args.Sprite, args.Component);
    }

    private void UpdateAppearance(Entity<CreamPiedComponent> ent, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!_sprite.LayerMapTryGet((ent.Owner, sprite), CreamPiedVisualLayer.Key, out var index, false))
            return;

        _appearance.TryGetData<bool>(ent.Owner, CreamPiedVisuals.Creamed, out var creamPied, appearance);
        _sprite.LayerSetVisible((ent.Owner, sprite), index, creamPied);
    }
}
