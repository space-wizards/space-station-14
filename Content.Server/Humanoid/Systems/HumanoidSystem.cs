using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory.Events;
using Content.Shared.Preferences;
using Content.Shared.Tag;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.Humanoid;

public sealed class HumanoidSystem : SharedHumanoidSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HumanoidComponent, ComponentInit>(OnInit);

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
            component.AllHiddenLayers.ToList(),
            component.CurrentMarkings.GetForwardEnumerator().ToList());
    }

    private void OnInit(EntityUid uid, HumanoidComponent humanoid, ComponentInit args)
    {
        if (string.IsNullOrEmpty(humanoid.Species))
        {
            return;
        }

        SetSpecies(uid, humanoid.Species, false, humanoid);

        if (!string.IsNullOrEmpty(humanoid.Initial)
            && _prototypeManager.TryIndex(humanoid.Initial, out HumanoidMarkingStartingSet? startingSet))
        {
            foreach (var marking in startingSet.Markings)
            {
                AddMarking(uid, marking.MarkingId, marking.MarkingColors, false);
            }

            foreach (var (layer, info) in startingSet.CustomBaseLayers)
            {
                humanoid.CustomBaseLayers.Add(layer, info);
            }
        }

        EnsureDefaultMarkings(uid, humanoid);

        Synchronize(uid, humanoid);
    }

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

        Synchronize(target, targetHumanoid);
    }

    public void SetSpecies(EntityUid uid, string species, bool sync = true, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        humanoid.Species = species;
        var prototype = _prototypeManager.Index<SpeciesPrototype>(species);
        humanoid.CurrentMarkings.FilterSpecies(species, _markingManager);
        var oldMarkings = humanoid.CurrentMarkings.GetForwardEnumerator().ToList();
        humanoid.CurrentMarkings = new(oldMarkings, prototype.MarkingPoints, _markingManager, _prototypeManager);

        if (sync)
        {
            Synchronize(uid, humanoid);
        }
    }

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

    public void ToggleHiddenLayer(EntityUid uid, HumanoidVisualLayers layer, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
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

    public void AddMarking(EntityUid uid, string marking, Color? color = null, bool sync = true, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
        {
            return;
        }

        var markingObject = prototype.AsMarking();
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

    public void AddMarking(EntityUid uid, string marking, IReadOnlyList<Color> colors, bool sync = true, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
        {
            return;
        }

        humanoid.CurrentMarkings.AddBack(prototype.MarkingCategory, new Marking(marking, colors));

        if (sync)
            Synchronize(uid, humanoid);
    }

    /// <summary>
    ///     Remove a marking by ID. This will attempt to fetch
    ///     the marking, removing it if possible.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="marking"></param>
    /// <param name="humanoid"></param>
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

    private void EnsureDefaultMarkings(EntityUid uid, HumanoidComponent? humanoid)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        humanoid.CurrentMarkings.EnsureDefault(humanoid.SkinColor, _markingManager);
    }
}
