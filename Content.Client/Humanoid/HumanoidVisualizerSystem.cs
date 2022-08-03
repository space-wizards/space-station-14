using System.Linq;
using Content.Shared.CharacterAppearance;
using Content.Shared.Humanoid;
using Content.Shared.Markings;
using Content.Shared.Species;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Humanoid;

public sealed class HumanoidVisualizerSystem : VisualizerSystem<HumanoidComponent>
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SpriteSystem _spriteSystem = default!;
    [Dependency] private MarkingManager _markingManager = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    // Since this doesn't allow you to discern what you'd want to change
    // without doing a double call, this will always setup all markings/accessories,
    // skin color, etc. upon change. Oops.
    //
    // Alternatively, the 'last changed' items could be cached server-side, and given
    // a null state upon the next state change, that way specific things aren't refreshed
    // every single time something is changed.
    protected override void OnAppearanceChange(EntityUid uid, HumanoidComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        // can't get humanoid visuals without a species
        if (!args.AppearanceData.TryGetValue(HumanoidVisualizerDataKey.Species, out var speciesRaw)
            && speciesRaw is not string speciesId)
        {
            return;
        }

        // TODO: Check species diff

        if (!args.AppearanceData.TryGetValue(HumanoidVisualizerDataKey.SkinColor, out var skinColorRaw)
            && skinColorRaw is not Color skinColor)
        {
            return;
        }

        // TODO: Check skin color diff

        if (!args.AppearanceData.TryGetValue(HumanoidVisualizerDataKey.EyeColor, out var eyeColorRaw)
            && eyeColorRaw is not Color eyeColor)
        {
            return;
        }

        // TODO: Check eye color diff

        if (!args.AppearanceData.TryGetValue(HumanoidVisualizerDataKey.LayerVisibility, out var layerVisRaw)
            && layerVisRaw is not List<HumanoidVisualLayers> layerVis)
        {
            return;
        }

        // TODO: Check diff

        if (!args.AppearanceData.TryGetValue(HumanoidVisualizerDataKey.Markings, out var markingsRaw)
            && markingsRaw is not List<Marking> markings)
        {
            return;
        }

        // TODO: Check diff
    }

    private void ReplaceHiddenLayers(EntityUid uid, HashSet<HumanoidVisualLayers> hiddenLayers,
        HumanoidComponent? humanoid)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        SetSpriteVisibility(uid, hiddenLayers, false);

        humanoid.HiddenLayers.ExceptWith(hiddenLayers);

        SetSpriteVisibility(uid, humanoid.HiddenLayers, true);

        humanoid.HiddenLayers.Clear();
        humanoid.HiddenLayers.UnionWith(hiddenLayers);
    }

    private void SetSpriteVisibility(EntityUid uid, HashSet<HumanoidVisualLayers> layers, bool visibility, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
        {
            return;
        }

        foreach (var layer in layers)
        {
            if (!sprite.LayerMapTryGet(layer, out var index))
            {
                continue;
            }

            sprite[index].Visible = visibility;
        }
    }

    private void ClearAllMarkings(EntityUid uid, HumanoidComponent? humanoid = null,
        SpriteComponent? spriteComp = null)
    {
        if (!Resolve(uid, ref humanoid, ref spriteComp))
        {
            return;
        }

        foreach (var marking in humanoid.CurrentMarkings.GetForwardEnumerator())
        {
            if (!_markingManager.IsValidMarking(marking, out var prototype))
            {
                continue;
            }

            foreach (var sprite in prototype.Sprites)
            {
                if (sprite is not SpriteSpecifier.Rsi rsi)
                {
                    continue;
                }

                var layerId = $"{marking.MarkingId}-{rsi.RsiState}";
                if (!spriteComp.LayerMapTryGet(layerId, out var index))
                {
                    continue;
                }

                spriteComp.LayerMapRemove(layerId);
                spriteComp.RemoveLayer(index);
            }
        }
    }

    private void ApplyMarkings(EntityUid uid,
        List<Marking> markings,
        HumanoidComponent? humanoid = null,
        SpriteComponent? spriteComp = null)
    {
        if (!Resolve(uid, ref spriteComp, ref humanoid))
        {
            return;
        }

        foreach (var marking in new ReverseMarkingEnumerator(markings))
        {
            if (!marking.Visible || !_markingManager.IsValidMarking(marking, out var markingPrototype))
            {
                continue;
            }

            ApplyMarking(uid, markingPrototype, marking.MarkingColors);
        }
    }

    private void ApplyMarking(EntityUid uid,
        MarkingPrototype markingPrototype,
        IReadOnlyList<Color>? colors,
        HumanoidComponent? humanoid = null,
        SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, ref humanoid))
        {
            return;
        }

        if (humanoid.HiddenLayers.Contains(markingPrototype.BodyPart)
            || !sprite.LayerMapTryGet(markingPrototype.BodyPart, out int targetLayer))
        {
            return;
        }

        for (var j = 0; j < markingPrototype.Sprites.Count; j++)
        {
            if (markingPrototype.Sprites[j] is not SpriteSpecifier.Rsi rsi)
            {
                continue;
            }

            var layerId = $"{markingPrototype.ID}-{rsi.RsiState}";

            if (sprite.LayerMapTryGet(layerId, out var existingLayer))
            {
                sprite.RemoveLayer(existingLayer);
                sprite.LayerMapRemove(markingPrototype.ID);
            }

            var layer = sprite.AddLayer(markingPrototype.Sprites[j], targetLayer + j + 1);
            sprite.LayerMapSet(layerId, layer);
            if (markingPrototype.FollowSkinColor || colors == null)
            {
                sprite.LayerSetColor(layerId, humanoid.SkinColor);
            }
            else
            {
                sprite.LayerSetColor(layerId, colors[j]);
            }
        }
    }

    private void ApplySkinColor(EntityUid uid, Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer> sprites,
        Color skinColor, SpriteComponent? spriteComp = null)
    {
        if (!Resolve(uid, ref spriteComp))
        {
            return;
        }

        foreach (var (layer, spriteInfo) in sprites)
        {
            if (!spriteInfo.MatchSkin)
            {
                continue;
            }

            SetBaseLayerColor(uid, layer, skinColor, spriteComp);
        }
    }

    private void SetBaseLayerColor(EntityUid uid, HumanoidVisualLayers layer, Color color,
        SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite)
            || !sprite.LayerMapTryGet(layer, out var index))
        {
            return;
        }

        sprite[index].Color = color;
    }

    private void ApplyBaseSprites(EntityUid uid, Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer> sprites,
        SpriteComponent? spriteComp = null)
    {
        if (!Resolve(uid, ref spriteComp))
        {
            return;
        }

        foreach (var (layer, spriteInfo) in sprites)
        {
            if (spriteInfo.BaseSprite != null && spriteComp.LayerMapTryGet(layer, out var index))
            {
                switch (spriteInfo.BaseSprite)
                {
                    case SpriteSpecifier.Rsi rsi:
                        spriteComp.LayerSetRSI(index, rsi.RsiPath);
                        spriteComp.LayerSetState(index, rsi.RsiState);
                        break;
                    case SpriteSpecifier.Texture texture:
                        spriteComp.LayerSetTexture(index, texture.TexturePath);
                        break;
                }
            }
        }
    }


}
