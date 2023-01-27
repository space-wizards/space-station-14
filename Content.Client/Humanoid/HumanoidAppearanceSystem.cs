using System.Linq;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Content.Shared.Humanoid.HumanoidAppearanceState;

namespace Content.Client.Humanoid;

public sealed class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidAppearanceComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, HumanoidAppearanceComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not HumanoidAppearanceState state)
            return;

        ApplyState(uid, component, Comp<SpriteComponent>(uid), state);
    }

    private void ApplyState(EntityUid uid, HumanoidAppearanceComponent component, SpriteComponent sprite, HumanoidAppearanceState state)
    {
        component.Sex = state.Sex;
        component.Species = state.Species;
        component.Age = state.Age;
        component.SkinColor = state.SkinColor;
        component.EyeColor = state.EyeColor;
        component.HiddenLayers = new(state.HiddenLayers);
        component.PermanentlyHidden = new(state.PermanentlyHidden);

        component.CustomBaseLayers = state.CustomBaseLayers.ShallowClone();

        // Caching hair and facial hair colors from their markings
        if(!_markingManager.MustMatchSkin(state.Species, HumanoidVisualLayers.Hair, _prototypeManager))
        {
            if (state.Markings.TryGetCategory(MarkingCategories.Hair, out var hairMarkings) &&
                hairMarkings.Count > 0)
            component.CachedHairColor = hairMarkings[0].MarkingColors.FirstOrDefault();
        }
        else component.CachedHairColor = state.SkinColor;
        
        if(!_markingManager.MustMatchSkin(state.Species, HumanoidVisualLayers.FacialHair, _prototypeManager))
        {
            if (state.Markings.TryGetCategory(MarkingCategories.FacialHair, out var facialHairMarkings) &&
                facialHairMarkings.Count > 0)
            component.CachedFacialHairColor = facialHairMarkings[0].MarkingColors.FirstOrDefault();
        }
        else component.CachedFacialHairColor = state.SkinColor;

        UpdateLayers(component, sprite);

        ApplyMarkingSet(uid, state.Markings, component, sprite);

        sprite[sprite.LayerMapReserveBlank(HumanoidVisualLayers.Eyes)].Color = state.EyeColor;
    }

    private static bool IsHidden(HumanoidAppearanceComponent humanoid, HumanoidVisualLayers layer)
        => humanoid.HiddenLayers.Contains(layer) || humanoid.PermanentlyHidden.Contains(layer);

    private void UpdateLayers(HumanoidAppearanceComponent component, SpriteComponent sprite)
    {
        var oldLayers = new HashSet<HumanoidVisualLayers>(component.BaseLayers.Keys);
        component.BaseLayers.Clear();

        // add default species layers
        var speciesProto = _prototypeManager.Index<SpeciesPrototype>(component.Species);
        var baseSprites = _prototypeManager.Index<HumanoidSpeciesBaseSpritesPrototype>(speciesProto.SpriteSet);
        foreach (var (key, id) in baseSprites.Sprites)
        {
            oldLayers.Remove(key);
            if (!component.CustomBaseLayers.ContainsKey(key))
                SetLayerData(component, sprite, key, id, sexMorph: true);
        }

        // add custom layers
        foreach (var (key, info) in component.CustomBaseLayers)
        {
            oldLayers.Remove(key);
            SetLayerData(component, sprite, key, info.ID, sexMorph: false, color: info.Color); ;
        }

        // hide old layers
        // TODO maybe just remove them altogether?
        foreach (var key in oldLayers)
        {
            if (sprite.LayerMapTryGet(key, out var index))
                sprite[index].Visible = false;
        }
    }

    private void SetLayerData(
        HumanoidAppearanceComponent component,
        SpriteComponent sprite,
        HumanoidVisualLayers key,
        string? protoId,
        bool sexMorph = false,
        Color? color = null)
    {
        var layerIndex = sprite.LayerMapReserveBlank(key);
        var layer = sprite[layerIndex];
        layer.Visible = !IsHidden(component, key);

        if (color != null)
            layer.Color = color.Value;

        if (protoId == null)
            return;

        if (sexMorph)
            protoId = HumanoidVisualLayersExtension.GetSexMorph(key, component.Sex, protoId);

        var proto = _prototypeManager.Index<HumanoidSpeciesSpriteLayer>(protoId);
        component.BaseLayers[key] = proto;

        if (proto.MatchSkin)
            layer.Color = component.SkinColor.WithAlpha(proto.LayerAlpha);

        if (proto.BaseSprite != null)
            sprite.LayerSetSprite(layerIndex, proto.BaseSprite);
    }

    /// <summary>
    ///     Loads a profile directly into a humanoid.
    /// </summary>
    /// <param name="uid">The humanoid entity's UID</param>
    /// <param name="profile">The profile to load.</param>
    /// <param name="humanoid">The humanoid entity's humanoid component.</param>
    /// <remarks>
    ///     This should not be used if the entity is owned by the server. The server will otherwise
    ///     override this with the appearance data it sends over.
    /// </remarks>
    public void LoadProfile(EntityUid uid, HumanoidCharacterProfile profile, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        var customBaseLayers = new Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>();

        var speciesPrototype = _prototypeManager.Index<SpeciesPrototype>(profile.Species);
        var markings = new MarkingSet(profile.Appearance.Markings, speciesPrototype.MarkingPoints, _markingManager,
            _prototypeManager);

        // legacy: remove in the future?
        markings.RemoveCategory(MarkingCategories.Hair);
        markings.RemoveCategory(MarkingCategories.FacialHair);

        var hair = new Marking(profile.Appearance.HairStyleId, new[] { profile.Appearance.HairColor });
        markings.AddBack(MarkingCategories.Hair, hair);

        var facialHair = new Marking(profile.Appearance.FacialHairStyleId,
            new[] { profile.Appearance.FacialHairColor });
        markings.AddBack(MarkingCategories.FacialHair, facialHair);

        markings.FilterSpecies(profile.Species, _markingManager, _prototypeManager);

        // Caching hair and facial hair colors from their markings
        Color? hairColor = null;
        if(!_markingManager.MustMatchSkin(profile.Species, HumanoidVisualLayers.Hair, _prototypeManager))
        {
            if (humanoid.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var hairMarkings) &&
                hairMarkings.Count > 0)
            hairColor = hairMarkings[0].MarkingColors.FirstOrDefault();
        }
        else hairColor = profile.Appearance.SkinColor;
        
        Color? facialHairColor = null;
        if(!_markingManager.MustMatchSkin(profile.Species, HumanoidVisualLayers.FacialHair, _prototypeManager))
        {
            if (humanoid.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var facialHairMarkings) &&
                facialHairMarkings.Count > 0)
            facialHairColor = facialHairMarkings[0].MarkingColors.FirstOrDefault();
        }
        else facialHairColor = profile.Appearance.SkinColor;
        
        markings.EnsureDefault(
            profile.Appearance.SkinColor, 
            profile.Appearance.EyeColor, 
            hairColor, 
            facialHairColor, 
            _markingManager);
        
        /*
        markings.EnsureDefault(
            profile.Appearance.SkinColor, 
            profile.Appearance.EyeColor, 
            profile.Appearance.HairColor, 
            profile.Appearance.FacialHairColor, 
            _markingManager);
        */

        DebugTools.Assert(uid.IsClientSide());

        var state = new HumanoidAppearanceState(markings,
            new(),
            new(),
            customBaseLayers,
            profile.Sex,
            profile.Gender,
            profile.Age,
            profile.Species,
            profile.Appearance.SkinColor,
            profile.Appearance.EyeColor);

        ApplyState(uid, humanoid, Comp<SpriteComponent>(uid), state);
    }

    private void ApplyMarkingSet(EntityUid uid,
        MarkingSet newMarkings,
        HumanoidAppearanceComponent humanoid,
        SpriteComponent sprite)
    {
        // skip this entire thing if both sets are empty
        if (humanoid.MarkingSet.Markings.Count == 0 && newMarkings.Markings.Count == 0)
            return;

        // I am lazy and I CBF resolving the previous mess, so I'm just going to nuke the markings.
        // Really, markings should probably be a separate component altogether.

        ClearAllMarkings(uid, humanoid, sprite);

        humanoid.MarkingSet = new(newMarkings);

        foreach (var markingList in humanoid.MarkingSet.Markings.Values)
        {
            foreach (var marking in markingList)
            {
                if (_markingManager.TryGetMarking(marking, out var markingPrototype))
                    ApplyMarking(uid, markingPrototype, marking.MarkingColors, marking.Visible, humanoid, sprite);
            }
        }
    }

    private void ClearAllMarkings(EntityUid uid, HumanoidAppearanceComponent humanoid,
        SpriteComponent sprite)
    {
        foreach (var markingList in humanoid.MarkingSet.Markings.Values)
        {
            foreach (var marking in markingList)
            {
                RemoveMarking(uid, marking, sprite);
            }
        }
    }

    private void ClearMarkings(EntityUid uid, List<Marking> markings, HumanoidAppearanceComponent humanoid,
        SpriteComponent spriteComp)
    {
        foreach (var marking in markings)
        {
            RemoveMarking(uid, marking, spriteComp);
        }
    }

    private void RemoveMarking(EntityUid uid, Marking marking,
        SpriteComponent spriteComp)
    {
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

    // TODO: Do marking things on server-side, so client only must apply it.
    private void ApplyMarking(EntityUid uid,
        MarkingPrototype markingPrototype,
        IReadOnlyList<Color>? colors,
        bool visible,
        HumanoidAppearanceComponent humanoid,
        SpriteComponent sprite)
    {
        if (!sprite.LayerMapTryGet(markingPrototype.BodyPart, out int targetLayer))
        {
            return;
        }

        visible &= !IsHidden(humanoid, markingPrototype.BodyPart);
        visible &= humanoid.BaseLayers.TryGetValue(markingPrototype.BodyPart, out var setting)
           && setting.AllowsMarkings;

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
            
            if (!visible || setting == null) // this is kinda implied
            {
                continue;
            }

            // Marking coloring
            var skinColor = humanoid.SkinColor;
            skinColor.A = setting.LayerAlpha;
            var markingColors = MarkingColoring.GetMarkingLayerColors(
                    markingPrototype,
                    skinColor,
                    humanoid.EyeColor,
                    humanoid.CachedHairColor,
                    humanoid.CachedFacialHairColor
                );

            if (setting.MarkingsMatchSkin) // Slimes use this for hair
            {
                sprite.LayerSetColor(layerId, skinColor);
            }
            else if (markingPrototype.ForcedColoring || colors == null)
            {
                sprite.LayerSetColor(layerId, markingColors[j]);
            }
            else
            {
                sprite.LayerSetColor(layerId, colors[j]);
            }
        }
    }

    public override void SetSkinColor(EntityUid uid, Color skinColor, bool sync = true, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid) || humanoid.SkinColor == skinColor)
            return;

        humanoid.SkinColor = skinColor;

        if (sync)
            Dirty(humanoid);

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        foreach (var (layer, spriteInfo) in humanoid.BaseLayers)
        {
            if (!spriteInfo.MatchSkin)
                continue;

            var index = sprite.LayerMapReserveBlank(layer);
            sprite[index].Color = skinColor.WithAlpha(spriteInfo.LayerAlpha);
        }
    }

    protected override void SetLayerVisibility(
        EntityUid uid,
        HumanoidAppearanceComponent humanoid,
        HumanoidVisualLayers layer,
        bool visible,
        bool permanent,
        ref bool dirty)
    {
        base.SetLayerVisibility(uid, humanoid, layer, visible, permanent, ref dirty);

        var sprite = Comp<SpriteComponent>(uid);
        if (!sprite.LayerMapTryGet(layer, out var index))
        {
            if (!visible)
                return;
            else
                index = sprite.LayerMapReserveBlank(layer);
        }

        var spriteLayer = sprite[index];
        if (spriteLayer.Visible == visible)
            return;

        spriteLayer.Visible = visible;

        // I fucking hate this. I'll get around to refactoring sprite layers eventually I swear

        foreach (var markingList in humanoid.MarkingSet.Markings.Values)
        {
            foreach (var marking in markingList)
            {
                if (_markingManager.TryGetMarking(marking, out var markingPrototype) && markingPrototype.BodyPart == layer)
                    ApplyMarking(uid, markingPrototype, marking.MarkingColors, marking.Visible, humanoid, sprite);
            }
        }
    }
}
