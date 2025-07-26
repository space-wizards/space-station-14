using Content.Client.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Reflection;

namespace Content.Client.Sprite;

public sealed class RandomSpriteSystem : SharedRandomSpriteSystem
{
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly ClientClothingSystem _clothing = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomSpriteComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, RandomSpriteComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not RandomSpriteColorComponentState state)
            return;

        if (state.Selected.Equals(component.Selected))
            return;

        component.Selected.Clear();
        component.Selected.EnsureCapacity(state.Selected.Count);

        foreach (var layer in state.Selected)
        {
            component.Selected.Add(layer.Key, layer.Value);
        }

        UpdateSpriteComponentAppearance(uid, component);
        UpdateClothingComponentAppearance(uid, component);
    }

    private void UpdateClothingComponentAppearance(EntityUid uid, RandomSpriteComponent component, ClothingComponent? clothing = null)
    {
        if (!Resolve(uid, ref clothing, false))
            return;

        foreach (var slotPair in clothing.ClothingVisuals)
        {
            foreach (var keyColorPair in component.Selected)
            {
                _clothing.SetLayerColor(clothing, slotPair.Key, keyColorPair.Key, keyColorPair.Value.Color);
                _clothing.SetLayerState(clothing, slotPair.Key, keyColorPair.Key, keyColorPair.Value.State);
            }
        }
    }

    private void UpdateSpriteComponentAppearance(EntityUid uid, RandomSpriteComponent component, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, false))
            return;

        foreach (var layer in component.Selected)
        {
            int index;
            if (_reflection.TryParseEnumReference(layer.Key, out var @enum))
            {
                if (!_sprite.LayerMapTryGet((uid, sprite), @enum, out index, logMissing: true))
                    continue;
            }
            else if (!_sprite.LayerMapTryGet((uid, sprite), layer.Key, out index, false))
            {
                if (layer.Key is not { } strKey || !int.TryParse(strKey, out index))
                {
                    Log.Error($"Invalid key `{layer.Key}` for entity with random sprite {ToPrettyString(uid)}");
                    continue;
                }
            }
            _sprite.LayerSetRsiState((uid, sprite), index, layer.Value.State);
            _sprite.LayerSetColor((uid, sprite), index, layer.Value.Color ?? Color.White);
        }
    }
}
