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
        if (_prototypeManager.TryIndex(component.Initial, out HumanoidMarkingStartingSet? startingSet))
        {
            component.CurrentMarkings = new(startingSet.Markings);
        }
        UpdateHumanoidAppearance(uid, true, true, true, component.HiddenLayers, component);
    }

    private void UpdateHumanoidAppearance(EntityUid uid,
        bool updateBaseSprites = false,
        bool updateSkinColor = false,
        bool updateMarkings = false,
        HashSet<HumanoidVisualLayers>? newHiddenSet = null,
        SharedHumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        if (!_prototypeManager.TryIndex(humanoid.Species, out SpeciesPrototype? species)
            || !_prototypeManager.TryIndex(species.SpriteSet, out HumanoidSpeciesSpritesPrototype? spriteSet)
            || !_prototypeManager.TryIndex(spriteSet.BaseSprites, out HumanoidSpeciesBaseSpritesPrototype? baseSprites))
        {
            return;
        }

        // This is the humanoid appearance sprite pipeline.

        if (updateBaseSprites)
            ApplyBaseSprites(uid, baseSprites.Sprites);

        if (updateSkinColor)
            ApplySkinColor(uid, baseSprites.Sprites, humanoid.SkinColor);

        if (newHiddenSet != null)
            ReplaceHiddenLayers(uid, newHiddenSet, humanoid);

        if (updateMarkings)
            ApplyAllMarkings(uid, species, baseSprites);
    }

    // Since this doesn't allow you to discern what you'd want to change
    // without doing a double call, this will always setup all markings/accessories,
    // skin color, etc. upon change. Oops.
    //
    // Alternatively, the 'last changed' items could be cached server-side, and given
    // a null state upon the next state change, that way specific things aren't refreshed
    // every single time something is changed.
    protected override void OnAppearanceChange(EntityUid uid, SharedHumanoidComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);
    }

    private void HandleState(EntityUid uid, SharedHumanoidComponent humanoid, ref ComponentHandleState args)
    {
        if (args.Current is not HumanoidComponentState state)
        {
            return;
        }

        humanoid.Age = state.Age;
        humanoid.Gender = state.Gender;

        if (!_prototypeManager.TryIndex(state.Species, out SpeciesPrototype? species)
            || !_prototypeManager.TryIndex(species.SpriteSet, out HumanoidSpeciesSpritesPrototype? spriteSet)
            || !_prototypeManager.TryIndex(spriteSet.BaseSprites, out HumanoidSpeciesBaseSpritesPrototype? baseSprites))
        {
            throw new ArgumentException("invalid species or sprites passed into component state");
        }

        // a lot of these checks are here to ensure that
        // we don't needlessly reorganize everything when one single
        // field's state was changed

        var updateBaseSprites = false;
        var updateSkinColor = false;
        var updateVisibility = false;
        var updateMarkings = false;

        if (humanoid.Species != state.Species || humanoid.Sex != state.Sex)
        {
            humanoid.Species = state.Species;
            humanoid.Sex = state.Sex;

            updateBaseSprites = true;
        }

        if (humanoid.SkinColor != state.SkinColor)
        {
            humanoid.SkinColor = state.SkinColor;

            updateSkinColor = true;
        }

        // This one is hard. It's less nicer to store the previous state in the component.
        var hiddenLayers = state.HiddenLayers.ToHashSet();

        if (!humanoid.HiddenLayers.SequenceEqual(hiddenLayers))
        {
            updateVisibility = true;
        }

        if (!humanoid.CurrentMarkings.Equals(state.Markings))
        {
            humanoid.CurrentMarkings = state.Markings;

            updateMarkings = true;
        }

        UpdateHumanoidAppearance(uid,
            updateBaseSprites,
            updateSkinColor,
            updateMarkings,
            updateVisibility ? hiddenLayers : null,
            humanoid);
    }

    private void ReplaceHiddenLayers(EntityUid uid, HashSet<HumanoidVisualLayers> hiddenLayers,
        SharedHumanoidComponent? humanoid)
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

    private void ApplyAllMarkings(EntityUid uid, SpeciesPrototype species, HumanoidSpeciesBaseSpritesPrototype baseSprites, SharedHumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        var points = GetMarkingLimitsAndDefaults(species.MarkingPoints, baseSprites.Sprites);
        ApplyMarkings(uid, humanoid.CurrentMarkings, points);
        ApplyDefaultMarkings(uid, points);
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
            if (!points.Required || points.Points <= 0)
            {
                continue;
            }

            while (points.Points > 0)
            {
                // this all has to be checked, continues shouldn't occur because
                // points.Points needs to be subtracted
                if (points.DefaultMarkings.TryGetValue(points.Points - 1, out var marking)
                    && _markingManager.Markings().TryGetValue(marking, out var markingPrototype)
                    && markingPrototype.MarkingCategory == layerType) // check if this actually belongs on this layer, too
                {
                    ApplyMarking(uid, markingPrototype, null);

                    humanoid.CurrentMarkings.AddBack(markingPrototype.AsMarking());
                }

                points.Points--;
            }
        }
    }

    private void ApplyMarkings(EntityUid uid,
        MarkingsSet markings,
        Dictionary<MarkingCategories, MarkingPoints> points,
        SharedHumanoidComponent? humanoid = null,
        SpriteComponent? spriteComp = null)
    {
        if (!Resolve(uid, ref spriteComp, ref humanoid))
        {
            return;
        }

        humanoid.CurrentMarkings = markings;

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

    private void ApplyMarking(EntityUid uid,
        MarkingPrototype markingPrototype,
        IReadOnlyList<Color>? colors,
        SharedHumanoidComponent? humanoid = null,
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
            var rsi = (SpriteSpecifier.Rsi) markingPrototype.Sprites[j];
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
