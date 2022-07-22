using Content.Shared.CharacterAppearance;
using Content.Shared.Humanoid;
using Content.Shared.Markings;
using Content.Shared.Species;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Humanoid;

public sealed class HumanoidVisualizerSystem : VisualizerSystem<SharedHumanoidComponent>
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SpriteSystem _spriteSystem = default!;
    [Dependency] private MarkingManager _markingManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SharedHumanoidComponent, ComponentInit>(OnInitialize);
    }

    private void OnInitialize(EntityUid uid, SharedHumanoidComponent component, ComponentInit args)
    {
        if (!_prototypeManager.TryIndex(component.Species, out SpeciesPrototype? species)
            || !_prototypeManager.TryIndex(species.SpriteSet, out HumanoidSpeciesSpritesPrototype? spriteSet)
            || !_prototypeManager.TryIndex(spriteSet.BaseSprites, out HumanoidSpeciesBaseSpritesPrototype? baseSprites))
        {
            return;
        }

        ApplyBaseSprites(uid, baseSprites.Sprites);
        ApplySkinColor(uid, baseSprites.Sprites, component.SkinColor);

        if (_prototypeManager.TryIndex(component.Initial, out HumanoidMarkingStartingSet? startingSet))
        {
            var points = GetMarkingLimitsAndDefaults(species.MarkingPoints, baseSprites.Sprites);
            ApplyMarkings(uid, new(startingSet.Markings), points);
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, SharedHumanoidComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);
    }

    private Dictionary<MarkingCategories, MarkingPoints> GetMarkingLimitsAndDefaults(string pointPrototypeId,
        Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer> spriteSettings)
    {
        // empty string implies no limit
        if (string.IsNullOrEmpty(pointPrototypeId))
        {
            return new();
        }

        if (!_prototypeManager.TryIndex(pointPrototypeId, out MarkingPointsPrototype? pointPrototype))
        {
            throw new ArgumentException("invalid prototype ID for marking points");
        }

        var points = MarkingPoints.CloneMarkingPointDictionary(pointPrototype.Points);

        foreach (var (layer, setting) in spriteSettings)
        {
            var category = MarkingCategoriesConversion.FromHumanoidVisualLayers(layer);
            if (setting.ReplaceOnly && MarkingCategoriesConversion.IsReplaceable(category))
            {
                if (!points.TryGetValue(category, out var point))
                {
                    point = new();
                }

                point.Points = 1;
                points[category] = point;
            }
        }

        return points;
    }

    private void ApplyDefaultMarkings(EntityUid uid,
        Dictionary<MarkingCategories, MarkingPoints> usedPoints,
        SharedHumanoidComponent? humanoid = null,
        SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, ref humanoid))
        {
            return;
        }

        foreach (var (layerType, points) in usedPoints)
        {
            if (points.Required && points.Points > 0)
            {
                while (points.Points > 0)
                {
                    // this all has to be checked, continues shouldn't occur because
                    // points.Points needs to be subtracted
                    if (points.DefaultMarkings.TryGetValue(points.Points - 1, out var marking)
                            && _markingManager.Markings().TryGetValue(marking, out var markingPrototype)
                            && markingPrototype.MarkingCategory == layerType // check if this actually belongs on this layer, too
                            && sprite.LayerMapTryGet(markingPrototype.BodyPart, out int targetLayer))
                    {
                        for (int j = 0; j < markingPrototype.Sprites.Count; j++)
                        {
                            var rsi = (SpriteSpecifier.Rsi) markingPrototype.Sprites[j];
                            string layerId = $"{markingPrototype.ID}-{rsi.RsiState}";

                            if (sprite.LayerMapTryGet(layerId, out var existingLayer))
                            {
                                sprite.RemoveLayer(existingLayer);
                                sprite.LayerMapRemove(markingPrototype.ID);
                            }

                            int layer = sprite.AddLayer(markingPrototype.Sprites[j], targetLayer + j + 1);
                            sprite.LayerMapSet(layerId, layer);
                            sprite.LayerSetColor(layerId, humanoid.SkinColor);
                        }

                        humanoid.CurrentMarkings.AddBack(markingPrototype.AsMarking());
                    }

                    points.Points--;
                }
            }
        }
    }

    private void ApplyMarkings(EntityUid uid,
        MarkingsSet markings,
        Dictionary<MarkingCategories, MarkingPoints> points,
        SharedHumanoidComponent? humanoid = null,
        SpriteComponent? spriteComp = null)
    {
        var markingsEnumerator = markings.GetReverseEnumerator();

        while (markingsEnumerator.MoveNext())
        {
            var marking = (Marking) markingsEnumerator.Current!;

            if (!marking.Visible || !_markingManager.IsValidMarking(marking, out MarkingPrototype? markingPrototype))
            {
                continue;
            }

            // this should be validated elsewhere?
            if (marking.MarkingColors.Count != markingPrototype.Sprites.Count)
            {
                marking = new Marking(marking.MarkingId, markingPrototype.Sprites.Count);
            }

            if (points.TryGetValue(markingPrototype.MarkingCategory, out MarkingPoints? point))
            {
                if (marking.Forced || point.Points == 0)
                {
                    continue;
                }

                point.Points--;
            }

            ApplyMarking(uid, markingPrototype, marking.MarkingColors);
        }
    }

    private void ApplyMarking(EntityUid uid, MarkingPrototype markingPrototype, IReadOnlyList<Color> colors,
        SharedHumanoidComponent? humanoid = null,
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

        for (var j = 0; j < markingPrototype.Sprites.Count; j++)
        {
            var rsi = (SpriteSpecifier.Rsi) markingPrototype.Sprites[j];
            var layerId = $"{markingPrototype.ID}-{rsi.RsiState}";

            if (sprite.LayerMapTryGet(layerId, out var existingLayer))
            {
                sprite.RemoveLayer(existingLayer);
                sprite.LayerMapRemove(markingPrototype.ID);
            }

            var layer = sprite.AddLayer(markingPrototype.Sprites[j], targetLayer + j + 1);
            sprite.LayerMapSet(layerId, layer);
            if (markingPrototype.FollowSkinColor)
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
            if (!spriteInfo.MatchSkin || !spriteComp.LayerMapTryGet(layer, out var index))
            {
                continue;
            }

            spriteComp.LayerSetColor(index, skinColor);
        }
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
