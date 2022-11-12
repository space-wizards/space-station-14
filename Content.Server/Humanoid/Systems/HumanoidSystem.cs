using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory.Events;
using Content.Shared.Preferences;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.Humanoid;

public sealed partial class HumanoidSystem : SharedHumanoidSystem
{
    [Dependency] private readonly MarkingManager _markingManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HumanoidComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HumanoidComponent, HumanoidMarkingModifierMarkingSetMessage>(OnMarkingsSet);
        SubscribeLocalEvent<HumanoidComponent, HumanoidMarkingModifierBaseLayersSetMessage>(OnBaseLayersSet);
        SubscribeLocalEvent<HumanoidComponent, GetVerbsEvent<Verb>>(OnVerbsRequest);
        SubscribeLocalEvent<HumanoidComponent, ExaminedEvent>(OnExamined);
    }

    private void Synchronize(EntityUid uid, HumanoidComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        SetAppearance(uid,
            component.Species,
            component.CustomBaseLayers,
            component.SkinColor,
            component.Sex,
            component.AllHiddenLayers.ToList(),
            component.CurrentMarkings.GetForwardEnumerator().ToList());
    }

    private void OnInit(EntityUid uid, HumanoidComponent humanoid, ComponentInit args)
    {
        if (string.IsNullOrEmpty(humanoid.Species))
        {
            return;
        }

        if (string.IsNullOrEmpty(humanoid.Initial)
            || !_prototypeManager.TryIndex(humanoid.Initial, out HumanoidProfilePrototype? startingSet))
        {
            LoadProfile(uid, HumanoidCharacterProfile.DefaultWithSpecies(humanoid.Species), humanoid);
            return;
        }

        // Do this first, because profiles currently do not support custom base layers
        foreach (var (layer, info) in startingSet.CustomBaseLayers)
        {
            humanoid.CustomBaseLayers.Add(layer, info);
        }

        LoadProfile(uid, startingSet.Profile, humanoid);
    }

    private void OnExamined(EntityUid uid, HumanoidComponent component, ExaminedEvent args)
    {
        var identity = Identity.Entity(component.Owner, EntityManager);
        var species = GetSpeciesRepresentation(component.Species).ToLower();
        var age = GetAgeRepresentation(component.Species, component.Age);

        args.PushText(Loc.GetString("humanoid-appearance-component-examine", ("user", identity), ("age", age), ("species", species)));
    }

    /// <summary>
    ///     Loads a humanoid character profile directly onto this humanoid mob.
    /// </summary>
    /// <param name="uid">The mob's entity UID.</param>
    /// <param name="profile">The character profile to load.</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void LoadProfile(EntityUid uid, HumanoidCharacterProfile profile, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        SetSpecies(uid, profile.Species, false, humanoid);
        humanoid.Sex = profile.Sex;

        SetSkinColor(uid, profile.Appearance.SkinColor, false);
        SetBaseLayerColor(uid, HumanoidVisualLayers.Eyes, profile.Appearance.EyeColor, false);

        humanoid.CurrentMarkings.Clear();

        // Hair/facial hair - this may eventually be deprecated.

        AddMarking(uid, profile.Appearance.HairStyleId, profile.Appearance.HairColor, false);
        AddMarking(uid, profile.Appearance.FacialHairStyleId, profile.Appearance.FacialHairColor, false);

        foreach (var marking in profile.Appearance.Markings)
        {
            AddMarking(uid, marking.MarkingId, marking.MarkingColors, false);
        }

        EnsureDefaultMarkings(uid, humanoid);

        humanoid.Gender = profile.Gender;
        if (TryComp<GrammarComponent>(uid, out var grammar))
        {
            grammar.Gender = profile.Gender;
        }

        humanoid.Age = profile.Age;

        Synchronize(uid);
    }

    // this was done enough times that it only made sense to do it here

    /// <summary>
    ///     Clones a humanoid's appearance to a target mob, provided they both have humanoid components.
    /// </summary>
    /// <param name="source">Source entity to fetch the original appearance from.</param>
    /// <param name="target">Target entity to apply the source entity's appearance to.</param>
    /// <param name="sourceHumanoid">Source entity's humanoid component.</param>
    /// <param name="targetHumanoid">Target entity's humanoid component.</param>
    public void CloneAppearance(EntityUid source, EntityUid target, HumanoidComponent? sourceHumanoid = null,
        HumanoidComponent? targetHumanoid = null)
    {
        if (!Resolve(source, ref sourceHumanoid) || !Resolve(target, ref targetHumanoid))
        {
            return;
        }

        targetHumanoid.Species = sourceHumanoid.Species;
        targetHumanoid.SkinColor = sourceHumanoid.SkinColor;
        targetHumanoid.Sex = sourceHumanoid.Sex;
        targetHumanoid.CustomBaseLayers = new(sourceHumanoid.CustomBaseLayers);
        targetHumanoid.CurrentMarkings = new(sourceHumanoid.CurrentMarkings);

        targetHumanoid.Gender = sourceHumanoid.Gender;
        if (TryComp<GrammarComponent>(target, out var grammar))
        {
            grammar.Gender = sourceHumanoid.Gender;
        }

        Synchronize(target, targetHumanoid);
    }

    /// <summary>
    ///     Set a humanoid mob's species. This will change their base sprites, as well as their current
    ///     set of markings to fit against the mob's new species.
    /// </summary>
    /// <param name="uid">The humanoid mob's UID.</param>
    /// <param name="species">The species to set the mob to. Will return if the species prototype was invalid.</param>
    /// <param name="sync">Whether to immediately synchronize this to the humanoid mob, or not.</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetSpecies(EntityUid uid, string species, bool sync = true, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid) || !_prototypeManager.TryIndex<SpeciesPrototype>(species, out var prototype))
        {
            return;
        }

        humanoid.Species = species;
        humanoid.CurrentMarkings.FilterSpecies(species, _markingManager);
        var oldMarkings = humanoid.CurrentMarkings.GetForwardEnumerator().ToList();
        humanoid.CurrentMarkings = new(oldMarkings, prototype.MarkingPoints, _markingManager, _prototypeManager);

        if (sync)
        {
            Synchronize(uid, humanoid);
        }
    }

    /// <summary>
    ///     Sets the skin color of this humanoid mob. Will only affect base layers that are not custom,
    ///     custom base layers should use <see cref="SetBaseLayerColor"/> instead.
    /// </summary>
    /// <param name="uid">The humanoid mob's UID.</param>
    /// <param name="skinColor">Skin color to set on the humanoid mob.</param>
    /// <param name="sync">Whether to synchronize this to the humanoid mob, or not.</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetSkinColor(EntityUid uid, Color skinColor, bool sync = true, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        humanoid.SkinColor = skinColor;

        if (sync)
            Synchronize(uid, humanoid);
    }

    /// <summary>
    ///     Sets the base layer ID of this humanoid mob. A humanoid mob's 'base layer' is
    ///     the skin sprite that is applied to the mob's sprite upon appearance refresh.
    /// </summary>
    /// <param name="uid">The humanoid mob's UID.</param>
    /// <param name="layer">The layer to target on this humanoid mob.</param>
    /// <param name="id">The ID of the sprite to use. See <see cref="HumanoidSpeciesSpriteLayer"/>.</param>
    /// <param name="sync">Whether to synchronize this to the humanoid mob, or not.</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetBaseLayerId(EntityUid uid, HumanoidVisualLayers layer, string id, bool sync = true,
        HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_prototypeManager.HasIndex<HumanoidSpeciesSpriteLayer>(id))
        {
            return;
        }

        if (humanoid.CustomBaseLayers.TryGetValue(layer, out var info))
        {
            humanoid.CustomBaseLayers[layer] = new(id, info.Color);
        }
        else
        {
            var layerInfo = new CustomBaseLayerInfo(id, humanoid.SkinColor);
            humanoid.CustomBaseLayers.Add(layer, layerInfo);
        }

        if (sync)
            Synchronize(uid, humanoid);
    }

    /// <summary>
    ///     Sets the color of this humanoid mob's base layer. See <see cref="SetBaseLayerId"/> for a
    ///     description of how base layers work.
    /// </summary>
    /// <param name="uid">The humanoid mob's UID.</param>
    /// <param name="layer">The layer to target on this humanoid mob.</param>
    /// <param name="color">The color to set this base layer to.</param>
    public void SetBaseLayerColor(EntityUid uid, HumanoidVisualLayers layer, Color color, bool sync = true, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        if (humanoid.CustomBaseLayers.TryGetValue(layer, out var info))
        {
            humanoid.CustomBaseLayers[layer] = new(info.ID, color);
        }
        else
        {
            var layerInfo = new CustomBaseLayerInfo(string.Empty, color);
            humanoid.CustomBaseLayers.Add(layer, layerInfo);
        }

        if (sync)
            Synchronize(uid, humanoid);
    }

    /// <summary>
    ///     Toggles a humanoid's sprite layer visibility.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="layer">Layer to toggle visibility for</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void ToggleHiddenLayer(EntityUid uid, HumanoidVisualLayers layer, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid, false))
        {
            return;
        }

        if (humanoid.HiddenLayers.Contains(layer))
        {
            humanoid.HiddenLayers.Remove(layer);
        }
        else
        {
            humanoid.HiddenLayers.Add(layer);
        }

        Synchronize(uid, humanoid);
    }

    /// <summary>
    ///     Sets the visibility for multiple layers at once on a humanoid's sprite.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="layers">An enumerable of all sprite layers that are going to have their visibility set</param>
    /// <param name="visible">The visibility state of the layers given</param>
    /// <param name="permanent">If this is a permanent change, or temporary. Permanent layers are stored in their own hash set.</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetLayersVisibility(EntityUid uid, IEnumerable<HumanoidVisualLayers> layers, bool visible, bool permanent = false,
        HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        foreach (var layer in layers)
        {
            if (visible)
            {
                if (permanent && humanoid.PermanentlyHidden.Contains(layer))
                {
                    humanoid.PermanentlyHidden.Remove(layer);
                }

                humanoid.HiddenLayers.Remove(layer);
            }
            else
            {
                if (permanent)
                {
                    humanoid.PermanentlyHidden.Add(layer);
                }

                humanoid.HiddenLayers.Add(layer);
            }
        }

        Synchronize(uid, humanoid);
    }

    /// <summary>
    ///     Adds a marking to this humanoid.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="marking">Marking ID to use</param>
    /// <param name="color">Color to apply to all marking layers of this marking</param>
    /// <param name="sync">Whether to immediately sync this marking or not</param>
    /// <param name="forced">If this marking was forced (ignores marking points)</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void AddMarking(EntityUid uid, string marking, Color? color = null, bool sync = true, bool forced = false, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
        {
            return;
        }

        var markingObject = prototype.AsMarking();
        markingObject.Forced = forced;
        if (color != null)
        {
            for (var i = 0; i < prototype.Sprites.Count; i++)
            {
                markingObject.SetColor(i, color.Value);
            }
        }

        humanoid.CurrentMarkings.AddBack(prototype.MarkingCategory, markingObject);

        if (sync)
            Synchronize(uid, humanoid);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="marking">Marking ID to use</param>
    /// <param name="colors">Colors to apply against this marking's set of sprites.</param>
    /// <param name="sync">Whether to immediately sync this marking or not</param>
    /// <param name="forced">If this marking was forced (ignores marking points)</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void AddMarking(EntityUid uid, string marking, IReadOnlyList<Color> colors, bool sync = true, bool forced = false, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
        {
            return;
        }

        var markingObject = new Marking(marking, colors);
        markingObject.Forced = forced;
        humanoid.CurrentMarkings.AddBack(prototype.MarkingCategory, markingObject);

        if (sync)
            Synchronize(uid, humanoid);
    }

    /// <summary>
    ///     Removes a marking from a humanoid by ID.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="marking">The marking to try and remove.</param>
    /// <param name="sync">Whether to immediately sync this to the humanoid</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void RemoveMarking(EntityUid uid, string marking, bool sync = true, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
        {
            return;
        }

        humanoid.CurrentMarkings.Remove(prototype.MarkingCategory, marking);

        if (sync)
            Synchronize(uid, humanoid);
    }

    /// <summary>
    ///     Removes a marking from a humanoid by category and index.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="category">Category of the marking</param>
    /// <param name="index">Index of the marking</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void RemoveMarking(EntityUid uid, MarkingCategories category, int index, HumanoidComponent? humanoid = null)
    {
        if (index < 0
            || !Resolve(uid, ref humanoid)
            || !humanoid.CurrentMarkings.TryGetCategory(category, out var markings)
            || index >= markings.Count)
        {
            return;
        }

        humanoid.CurrentMarkings.Remove(category, index);

        Synchronize(uid, humanoid);
    }

    /// <summary>
    ///     Sets the marking ID of the humanoid in a category at an index in the category's list.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="category">Category of the marking</param>
    /// <param name="index">Index of the marking</param>
    /// <param name="markingId">The marking ID to use</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetMarkingId(EntityUid uid, MarkingCategories category, int index, string markingId, HumanoidComponent? humanoid = null)
    {
        if (index < 0
            || !_markingManager.MarkingsByCategory(category).TryGetValue(markingId, out var markingPrototype)
            || !Resolve(uid, ref humanoid)
            || !humanoid.CurrentMarkings.TryGetCategory(category, out var markings)
            || index >= markings.Count)
        {
            return;
        }

        var marking = markingPrototype.AsMarking();
        for (var i = 0; i < marking.MarkingColors.Count && i < markings[index].MarkingColors.Count; i++)
        {
            marking.SetColor(i, markings[index].MarkingColors[i]);
        }

        humanoid.CurrentMarkings.Replace(category, index, marking);

        Synchronize(uid, humanoid);
    }

    /// <summary>
    ///     Sets the marking colors of the humanoid in a category at an index in the category's list.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="category">Category of the marking</param>
    /// <param name="index">Index of the marking</param>
    /// <param name="colors">The marking colors to use</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetMarkingColor(EntityUid uid, MarkingCategories category, int index, List<Color> colors,
        HumanoidComponent? humanoid = null)
    {
        if (index < 0
            || !Resolve(uid, ref humanoid)
            || !humanoid.CurrentMarkings.TryGetCategory(category, out var markings)
            || index >= markings.Count)
        {
            return;
        }

        for (var i = 0; i < markings[index].MarkingColors.Count && i < colors.Count; i++)
        {
            markings[index].SetColor(i, colors[i]);
        }

        Synchronize(uid, humanoid);
    }

    /// <summary>
    /// Takes ID of the species prototype, returns UI-friendly name of the species.
    /// </summary>
    public string GetSpeciesRepresentation(string speciesId)
    {
        if (_prototypeManager.TryIndex<SpeciesPrototype>(speciesId, out var species))
        {
            return Loc.GetString(species.Name);
        }
        else
        {
            return Loc.GetString("humanoid-appearance-component-unknown-species");
        }
    }

    public string GetAgeRepresentation(string species, int age)
    {
        _prototypeManager.TryIndex<SpeciesPrototype>(species, out var speciesPrototype);

        if (speciesPrototype == null)
        {
            Logger.Error("Tried to get age representation of species that couldn't be indexed: " + species);
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.YoungAge)
        {
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.OldAge)
        {
            return Loc.GetString("identity-age-middle-aged");
        }

        return Loc.GetString("identity-age-old");
    }

    private void EnsureDefaultMarkings(EntityUid uid, HumanoidComponent? humanoid)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        humanoid.CurrentMarkings.EnsureDefault(humanoid.SkinColor, _markingManager);
    }
}
