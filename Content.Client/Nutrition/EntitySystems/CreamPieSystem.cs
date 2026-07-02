using Content.Client.DisplacementMap;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Nutrition.EntitySystems;

public sealed partial class CreamPieSystem : SharedCreamPieSystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CreamPiedComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CreamPiedComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<CreamPiedComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<CreamPiedComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnComponentInit(Entity<CreamPiedComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent);
    }

    private void OnComponentShutdown(Entity<CreamPiedComponent> ent, ref ComponentShutdown args)
    {
        _sprite.RemoveLayer(ent.Owner, CreamPiedVisualLayer.Key);
        _displacement.EnsureDisplacementIsNotOnSprite((ent, Comp<SpriteComponent>(ent)), CreamPiedVisualLayer.Key);
    }

    private void OnAppearanceChange(Entity<CreamPiedComponent> ent, ref AppearanceChangeEvent args)
    {
        UpdateAppearance((ent.Owner, ent.Comp, args.Sprite, args.Component));
    }

    private void OnAfterAutoHandleState(Entity<CreamPiedComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // Update when the sprite datafield is changed so that changelings can transform properly.
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<CreamPiedComponent, SpriteComponent?, AppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false) || !Resolve(ent, ref ent.Comp3, false))
            return;

        var creamPied = ent.Comp1;
        var sprite = ent.Comp2;
        var appearance = ent.Comp3;

        // If there is no sprite to use, remove the layer. Otherwise ensure that it exists and set the visuals accordingly.
        int index;
        if (creamPied.Sprite == null)
        {
            _sprite.RemoveLayer((ent.Owner, sprite), CreamPiedVisualLayer.Key);
            _displacement.EnsureDisplacementIsNotOnSprite((ent.Owner, sprite), CreamPiedVisualLayer.Key);
            return;
        }

        index = _sprite.LayerMapReserve((ent.Owner, sprite), CreamPiedVisualLayer.Key);

        _appearance.TryGetData<bool>(ent.Owner, CreamPiedVisuals.Creamed, out var isCreamPied, appearance);
        _sprite.LayerSetSprite((ent.Owner, sprite), index, creamPied.Sprite);
        _sprite.LayerSetVisible((ent.Owner, sprite), index, isCreamPied);

        if (_prototype.Resolve(ent.Comp1.Displacement, out var displacementProto))
        {
            _displacement.TryAddDisplacement(displacementProto.Displacement, (ent.Owner, sprite), index, CreamPiedVisualLayer.Key, out _);
        }
    }
}
