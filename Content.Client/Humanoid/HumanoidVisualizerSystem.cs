using System.Linq;
using Content.Shared.CharacterAppearance;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Species;
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
    [Dependency] private MarkingManager _markingManager = default!;

    protected override void OnAppearanceChange(EntityUid uid, HumanoidComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!args.AppearanceData.TryGetValue(HumanoidVisualizerKey.Key, out var dataRaw)
            || dataRaw is not HumanoidVisualizerData data)
        {
            return;
        }

        if (!_prototypeManager.TryIndex(data.Species, out SpeciesPrototype? speciesProto)
            || !_prototypeManager.TryIndex(speciesProto.SpriteSet, out HumanoidSpeciesBaseSpritesPrototype? baseSprites))
        {
            return;
        }

        if (component.Species != data.Species)
        {
            if (data.CustomBaseLayerInfo.Count != 0)
            {
                MergeCustomBaseSprites(uid, baseSprites.Sprites, data.CustomBaseLayerInfo);
            }
            else
            {
                MergeCustomBaseSprites(uid, baseSprites.Sprites, null);
            }

            ApplyBaseSprites(uid);
            component.Species = data.Species;
        }

        if (component.SkinColor != data.SkinColor)
        {
            ApplySkinColor(uid, data.SkinColor);
            component.SkinColor = data.SkinColor;
        }

        if (data.CustomBaseLayerInfo.Count != 0)
        {
            foreach (var (layer, info) in data.CustomBaseLayerInfo)
            {
                SetBaseLayerColor(uid, layer, info.Color);
            }
        }

        var layerVis = data.LayerVisibility.ToHashSet();
        ReplaceHiddenLayers(uid, layerVis, component);


        DiffAndApplyMarkings(uid, data.Markings);
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
            if (!_markingManager.TryGetMarking(newMarkings[i], out var dirtyMarking))
            {
                continue;
            }

            ApplyMarking(uid, dirtyMarking, newMarkings[i].MarkingColors, newMarkings[i].Visible);
        }

        if (dirtyRangeStart >= 0)
        {
            var range = newMarkings.GetRange(dirtyRangeStart, newMarkings.Count - 1);
            var oldRange = humanoid.CurrentMarkings.GetRange(dirtyRangeStart, newMarkings.Count - 1);

            ClearMarkings(uid, oldRange, humanoid);
            ApplyMarkings(uid, range, humanoid);
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

        if (!_markingManager.TryGetMarking(marking, out var prototype))
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
        HumanoidComponent? humanoid = null,
        SpriteComponent? spriteComp = null)
    {
        if (!Resolve(uid, ref spriteComp, ref humanoid))
        {
            return;
        }

        foreach (var marking in new ReverseMarkingEnumerator(markings))
        {
            if (!_markingManager.TryGetMarking(marking, out var markingPrototype))
            {
                continue;
            }

            ApplyMarking(uid, markingPrototype, marking.MarkingColors, marking.Visible);
        }
    }

    private void ApplyMarking(EntityUid uid,
        MarkingPrototype markingPrototype,
        IReadOnlyList<Color>? colors,
        bool visible,
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

            humanoid.BaseLayers.TryGetValue(markingPrototype.BodyPart, out var setting);

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

    private void ApplySkinColor(EntityUid uid,
        Color skinColor,
        HumanoidComponent? humanoid = null,
        SpriteComponent? spriteComp = null)
    {
        if (!Resolve(uid, ref humanoid, ref spriteComp))
        {
            return;
        }

        foreach (var (layer, spriteInfo) in humanoid.BaseLayers)
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

    private void MergeCustomBaseSprites(EntityUid uid, Dictionary<HumanoidVisualLayers, string> baseSprites,
        Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>? customBaseSprites,
        HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        foreach (var (key, id) in baseSprites)
        {
            if (!_prototypeManager.TryIndex(id, out HumanoidSpeciesSpriteLayer? baseLayer))
            {
                continue;
            }

            humanoid.BaseLayers.Add(key, baseLayer);
        }

        if (customBaseSprites == null)
        {
            return;
        }

        foreach (var (key, info) in customBaseSprites)
        {
            if (!_prototypeManager.TryIndex(info.ID, out HumanoidSpeciesSpriteLayer? baseLayer))
            {
                continue;
            }

            if (humanoid.BaseLayers.ContainsKey(key))
            {
                humanoid.BaseLayers[key] = baseLayer;
                continue;
            }

            humanoid.BaseLayers.Add(key, baseLayer);
        }
    }

    private void ApplyBaseSprites(EntityUid uid,
        HumanoidComponent? humanoid = null,
        SpriteComponent? spriteComp = null)
    {
        if (!Resolve(uid, ref humanoid, ref spriteComp))
        {
            return;
        }

        foreach (var (layer, spriteInfo) in humanoid.BaseLayers)
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
