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
    protected override void OnAppearanceChange(EntityUid uid, HumanoidComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        // can't get humanoid visuals without a species
        if (!args.AppearanceData.TryGetValue(HumanoidVisualizerDataKey.Species, out var speciesRaw))
        {
            return;
        }

        HumanoidSpeciesBaseSpritesPrototype? baseSprites;

        if (speciesRaw is string speciesId)
        {
            if (!_prototypeManager.TryIndex(speciesId, out baseSprites))
            {
                return;
            }

            if (component.Species != speciesId)
            {
                ApplyBaseSprites(uid, baseSprites.Sprites);
                component.Species = speciesId;
            }
        }
        else
        {
            return;
        }

        if (args.AppearanceData.TryGetValue(HumanoidVisualizerDataKey.SkinColor, out var skinColorRaw)
            && skinColorRaw is Color skinColor)
        {
            if (component.SkinColor != skinColor)
            {
                ApplySkinColor(uid, baseSprites.Sprites, skinColor);
                component.SkinColor = skinColor;
            }
        }

        if (args.AppearanceData.TryGetValue(HumanoidVisualizerDataKey.EyeColor, out var eyeColorRaw)
            && eyeColorRaw is Color eyeColor)
        {
            // just set the base sprite's eye color
            SetBaseLayerColor(uid, HumanoidVisualLayers.Eyes, eyeColor);
        }

        if (args.AppearanceData.TryGetValue(HumanoidVisualizerDataKey.LayerVisibility, out var layerVisRaw)
            && layerVisRaw is List<HumanoidVisualLayers> layerVisList)
        {
            var layerVis = layerVisList.ToHashSet();
            ReplaceHiddenLayers(uid, layerVis, component);
        }


        if (args.AppearanceData.TryGetValue(HumanoidVisualizerDataKey.Markings, out var markingsRaw)
            && markingsRaw is List<Marking> markings)
        {
            DiffAndApplyMarkings(uid, markings, baseSprites.Sprites);
        }
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

    private void DiffAndApplyMarkings(EntityUid uid,
        List<Marking> newMarkings,
        Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer> layerSettings,
        HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        var dirtyMarkings = new List<int>();
        var dirtyRangeStart = humanoid.CurrentMarkings.Count == 0 ? 0 : -1;

        for (var i = 0; i < humanoid.CurrentMarkings.Count; i++)
        {
            // if the marking is different here, set the range start to i and break, we need
            // to rebuild all markings starting from i
            if (humanoid.CurrentMarkings[i].MarkingId != newMarkings[i].MarkingId)
            {
                dirtyRangeStart = i;
                break;
            }

            // otherwise, we add the current marking to dirtyMarkings if it has different
            // settings
            if (humanoid.CurrentMarkings[i] != newMarkings[i])
            {
                dirtyMarkings.Add(i);
            }
        }

        foreach (var i in dirtyMarkings)
        {
            if (!_markingManager.IsValidMarking(newMarkings[i], out var dirtyMarking))
            {
                continue;
            }

            ApplyMarking(uid, dirtyMarking, newMarkings[i].MarkingColors, newMarkings[i].Visible, layerSettings);
        }

        if (dirtyRangeStart >= 0)
        {
            var range = newMarkings.GetRange(dirtyRangeStart, newMarkings.Count - 1);
            var oldRange = humanoid.CurrentMarkings.GetRange(dirtyRangeStart, newMarkings.Count - 1);

            ClearMarkings(uid, oldRange, humanoid);
            ApplyMarkings(uid, range, layerSettings, humanoid);
        }

        if (dirtyMarkings.Count > 0 || dirtyRangeStart >= 0)
        {
            humanoid.CurrentMarkings = newMarkings;
        }
    }

    private void ClearAllMarkings(EntityUid uid, HumanoidComponent? humanoid = null,
        SpriteComponent? spriteComp = null)
    {
        if (!Resolve(uid, ref humanoid, ref spriteComp))
        {
            return;
        }

        ClearMarkings(uid, humanoid.CurrentMarkings, humanoid, spriteComp);
    }

    private void ClearMarkings(EntityUid uid, List<Marking> markings, HumanoidComponent? humanoid = null,
        SpriteComponent? spriteComp = null)
    {
        if (!Resolve(uid, ref humanoid, ref spriteComp))
        {
            return;
        }

        foreach (var marking in markings)
        {
            RemoveMarking(uid, marking, humanoid, spriteComp);
        }
    }

    private void RemoveMarking(EntityUid uid, Marking marking, HumanoidComponent? humanoid = null,
        SpriteComponent? spriteComp = null)
    {
        if (!Resolve(uid, ref humanoid, ref spriteComp))
        {
            return;
        }

        if (!_markingManager.IsValidMarking(marking, out var prototype))
        {
            return;
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

    private void ApplyMarkings(EntityUid uid,
        List<Marking> markings,
        Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer> layerSettings,
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

            ApplyMarking(uid, markingPrototype, marking.MarkingColors, marking.Visible, layerSettings);
        }
    }

    private void ApplyMarking(EntityUid uid,
        MarkingPrototype markingPrototype,
        IReadOnlyList<Color>? colors,
        bool visible,
        Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer> layerSettings,
        HumanoidComponent? humanoid = null,
        SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, ref humanoid))
        {
            return;
        }

        if (!sprite.LayerMapTryGet(markingPrototype.BodyPart, out int targetLayer))
        {
            return;
        }

        visible &= !humanoid.HiddenLayers.Contains(markingPrototype.BodyPart);

        for (var j = 0; j < markingPrototype.Sprites.Count; j++)
        {
            if (markingPrototype.Sprites[j] is not SpriteSpecifier.Rsi rsi)
            {
                continue;
            }

            var layerId = $"{markingPrototype.ID}-{rsi.RsiState}";

            if (!sprite.LayerMapTryGet(layerId, out _))
            {
                var layer = sprite.AddLayer(markingPrototype.Sprites[j], targetLayer + j + 1);
                sprite.LayerMapSet(layerId, layer);
                sprite.LayerSetSprite(layerId, rsi);
            }

            sprite.LayerSetVisible(layerId, visible);

            if (!visible)
            {
                continue;
            }

            layerSettings.TryGetValue(markingPrototype.BodyPart, out var setting);

            if (markingPrototype.FollowSkinColor || colors == null)
            {
                var skinColor = humanoid.SkinColor;
                if (setting is { MarkingsMatchSkin: true })
                {
                    skinColor.A = setting.LayerAlpha;
                }

                sprite.LayerSetColor(layerId, skinColor);
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

            var color = skinColor;
            color.A = spriteInfo.LayerAlpha;

            SetBaseLayerColor(uid, layer, color, spriteComp);
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
