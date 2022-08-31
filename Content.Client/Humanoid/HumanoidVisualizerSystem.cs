using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
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

        if (data.CustomBaseLayerInfo.Count != 0)
        {
            MergeCustomBaseSprites(uid, baseSprites.Sprites, data.CustomBaseLayerInfo);
        }
        else
        {
            MergeCustomBaseSprites(uid, baseSprites.Sprites, null);
        }

        ApplyBaseSprites(uid);
        ApplySkinColor(uid, data.SkinColor);

        if (data.CustomBaseLayerInfo.Count != 0)
        {
            foreach (var (layer, info) in data.CustomBaseLayerInfo)
            {
                SetBaseLayerColor(uid, layer, info.Color);
            }
        }

        var layerVis = data.LayerVisibility.ToHashSet();
        var layerVisDirty = ReplaceHiddenLayers(uid, layerVis, component);

        DiffAndApplyMarkings(uid, data.Markings, layerVisDirty);
    }

    private bool ReplaceHiddenLayers(EntityUid uid, HashSet<HumanoidVisualLayers> hiddenLayers,
        HumanoidComponent? humanoid)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return false;
        }

        if (hiddenLayers.SetEquals(humanoid.HiddenLayers))
        {
            return false;
        }

        SetSpriteVisibility(uid, hiddenLayers, false);

        humanoid.HiddenLayers.ExceptWith(hiddenLayers);

        SetSpriteVisibility(uid, humanoid.HiddenLayers, true);

        humanoid.HiddenLayers.Clear();
        humanoid.HiddenLayers.UnionWith(hiddenLayers);

        return true;
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
        bool hiddenLayersDirty,
        HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        // skip this entire thing if both sets are empty
        if (humanoid.CurrentMarkings.Count == 0 && newMarkings.Count == 0)
        {
            return;
        }

        var dirtyMarkings = new List<int>();
        var dirtyRangeStart = humanoid.CurrentMarkings.Count == 0 ? 0 : -1;

        // edge cases:
        // humanoid.CurrentMarkings < newMarkings.Count
        // - check if count matches this condition before diffing
        // - if count is unequal, set dirty range to start from humanoid.CurrentMarkings.Count
        // humanoid.CurrentMarkings > newMarkings.Count, no dirty markings
        // - break count upon meeting this condition
        // - clear markings from newMarkings.Count to humanoid.CurrentMarkings.Count - newMarkings.Count

        for (var i = 0; i < humanoid.CurrentMarkings.Count; i++)
        {
            // if we've reached the end of the new set of markings,
            // then that means it's time to finish
            if (newMarkings.Count == i)
            {
                break;
            }

            // if the marking is different here, set the range start to i and break, we need
            // to rebuild all markings starting from i
            if (humanoid.CurrentMarkings[i].MarkingId != newMarkings[i].MarkingId)
            {
                dirtyRangeStart = i;
                break;
            }

            // otherwise, we add the current marking to dirtyMarkings if it has different
            // settings
            // however: if the hidden layers are set to dirty, then we need to
            // instead just add every single marking, since we don't know ahead of time
            // where these markings go
            if (humanoid.CurrentMarkings[i] != newMarkings[i] || hiddenLayersDirty)
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

        if (humanoid.CurrentMarkings.Count < newMarkings.Count && dirtyRangeStart < 0)
        {
            dirtyRangeStart = humanoid.CurrentMarkings.Count;
        }

        if (dirtyRangeStart >= 0)
        {
            var range = newMarkings.GetRange(dirtyRangeStart, newMarkings.Count - dirtyRangeStart);

            if (humanoid.CurrentMarkings.Count > 0)
            {
                var oldRange = humanoid.CurrentMarkings.GetRange(dirtyRangeStart, humanoid.CurrentMarkings.Count - dirtyRangeStart);
                ClearMarkings(uid, oldRange, humanoid);
            }

            ApplyMarkings(uid, range, humanoid);
        }
        else if (humanoid.CurrentMarkings.Count != newMarkings.Count)
        {
            if (newMarkings.Count == 0)
            {
                ClearAllMarkings(uid);
            }
            else if (humanoid.CurrentMarkings.Count > newMarkings.Count)
            {
                var rangeStart = newMarkings.Count;
                var rangeCount = humanoid.CurrentMarkings.Count - newMarkings.Count;
                var range = humanoid.CurrentMarkings.GetRange(rangeStart, rangeCount);

                ClearMarkings(uid, range, humanoid);
            }
        }

        if (dirtyMarkings.Count > 0 || dirtyRangeStart >= 0 || humanoid.CurrentMarkings.Count != newMarkings.Count)
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

        if (!sprite.LayerMapTryGet(markingPrototype.BodyPart, out int targetLayer)
            || !humanoid.BaseLayers.ContainsKey(markingPrototype.BodyPart))
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

            if (markingPrototype.FollowSkinColor || colors == null || setting is { MarkingsMatchSkin: true})
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

        humanoid.SkinColor = skinColor;

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

        humanoid.BaseLayers.Clear();

        foreach (var (key, id) in baseSprites)
        {
            var sexMorph = humanoid.Sex switch
            {
                Sex.Male when HumanoidVisualLayersExtension.HasSexMorph(key) => $"{id}Male",
                Sex.Female when HumanoidVisualLayersExtension.HasSexMorph(key) => $"{id}Female",
                _ => id
            };

            if (!_prototypeManager.TryIndex(sexMorph, out HumanoidSpeciesSpriteLayer? baseLayer))
            {
                continue;
            }

            if (!humanoid.BaseLayers.TryAdd(key, baseLayer))
            {
                humanoid.BaseLayers[key] = baseLayer;
            }
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

            if (!humanoid.BaseLayers.TryAdd(key, baseLayer))
            {
                humanoid.BaseLayers[key] = baseLayer;
            }
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
