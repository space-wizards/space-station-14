using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Markings;

/// <summary>
/// Manager responsible for sharing the logic of markings between in-simulation bodies and out-of-simulation profile editing
/// </summary>
public sealed class MarkingManager
{
    [Dependency] private readonly IComponentFactory _component = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private FrozenDictionary<HumanoidVisualLayers, FrozenDictionary<string, MarkingPrototype>> _categorizedMarkings = default!;
    private FrozenDictionary<string, MarkingPrototype> _markings = default!;

    public void Initialize()
    {
        _prototype.PrototypesReloaded += OnPrototypeReload;
        CachePrototypes();
    }

    private void CachePrototypes()
    {
        var markingDict = new Dictionary<HumanoidVisualLayers, Dictionary<string, MarkingPrototype>>();

        foreach (var category in Enum.GetValues<HumanoidVisualLayers>())
        {
            markingDict.Add(category, new());
        }

        foreach (var prototype in _prototype.EnumeratePrototypes<MarkingPrototype>())
        {
            try
            {
                markingDict[prototype.BodyPart].Add(prototype.ID, prototype);
            }
            catch (Exception e)
            {
                throw new Exception($"failed to process {prototype.ID}", e);
            }
        }

        _markings = _prototype.EnumeratePrototypes<MarkingPrototype>().ToFrozenDictionary(x => x.ID);
        _categorizedMarkings = markingDict.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.ToFrozenDictionary());
    }

    public FrozenDictionary<string, MarkingPrototype> MarkingsByLayer(HumanoidVisualLayers category)
    {
        // all marking categories are guaranteed to have a dict entry
        return _categorizedMarkings[category];
    }

    /// <summary>
    ///     Markings by category, species and sex.
    /// </summary>
    /// <remarks>
    ///     This is done per category, as enumerating over every single marking by group isn't useful.
    ///     Please make a pull request if you find a use case for that behavior.
    /// </remarks>
    /// <returns></returns>
    public IReadOnlyDictionary<string, MarkingPrototype> MarkingsByLayerAndGroupAndSex(HumanoidVisualLayers layer,
        ProtoId<MarkingsGroupPrototype> group,
        Sex sex)
    {
        var groupProto = _prototype.Index(group);
        var whitelisted = groupProto.Limits.GetValueOrDefault(layer)?.OnlyGroupWhitelisted ?? groupProto.OnlyGroupWhitelisted;
        var res = new Dictionary<string, MarkingPrototype>();

        foreach (var (key, marking) in MarkingsByLayer(layer))
        {
            if (!CanBeApplied(groupProto, sex, marking, whitelisted))
                continue;

            res.Add(key, marking);
        }

        return res;
    }

    /// <summary>
    /// Gets the marking prototype associated with the marking.
    /// </summary>
    /// <param name="marking">The marking to look up</param>
    /// <param name="markingResult">When this method returns; will contain the marking prototype corresponding to the one specified by the marking if it exists.</param>
    /// <returns>Whether a marking prototype exists for the given marking</returns>
    public bool TryGetMarking(Marking marking, [NotNullWhen(true)] out MarkingPrototype? markingResult)
    {
        return _markings.TryGetValue(marking.MarkingId, out markingResult);
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<MarkingPrototype>())
            CachePrototypes();
    }

    /// <summary>
    /// Determines if a marking prototype can be applied to something with the given markings group and sex.
    /// </summary>
    /// <param name="group">The markings group to test</param>
    /// <param name="sex">The sex to test</param>
    /// <param name="prototype">The prototype to reference against</param>
    /// <returns>True if a marking with the prototype could be applied</returns>
    public bool CanBeApplied(ProtoId<MarkingsGroupPrototype> group, Sex sex, MarkingPrototype prototype)
    {
        var groupProto = _prototype.Index(group);
        var whitelisted = groupProto.Limits.GetValueOrDefault(prototype.BodyPart)?.OnlyGroupWhitelisted ?? groupProto.OnlyGroupWhitelisted;

        return CanBeApplied(groupProto, sex, prototype, whitelisted);
    }

    private bool CanBeApplied(MarkingsGroupPrototype group, Sex sex, MarkingPrototype prototype, bool whitelisted)
    {
        if (prototype.GroupWhitelist == null)
        {
            if (whitelisted)
                return false;
        }
        else
        {
            if (!prototype.GroupWhitelist.Contains(group))
                return false;
        }

        return prototype.SexRestriction == null || prototype.SexRestriction == sex;
    }

    /// <summary>
    /// Ensures that the <see cref="markingSets"/> have a valid amount of colors
    /// </summary>
    public void EnsureValidColors(Dictionary<HumanoidVisualLayers, List<Marking>> markingSets)
    {
        foreach (var markings in markingSets.Values)
        {
            for (var i = markings.Count - 1; i >= 0; i--)
            {
                if (!TryGetMarking(markings[i], out var marking))
                {
                    markings.RemoveAt(i);
                    continue;
                }

                if (marking.Sprites.Count != markings[i].MarkingColors.Count)
                {
                    markings[i] = new Marking(marking.ID, marking.Sprites.Count);
                }
            }
        }
    }

    /// <summary>
    /// Ensures that the <see cref="markingSets"/> are valid per the constraints on <see cref="group"/> and <see cref="sex"/>
    /// </summary>
    public void EnsureValidGroupAndSex(Dictionary<HumanoidVisualLayers, List<Marking>> markingSets, ProtoId<MarkingsGroupPrototype> group, Sex sex)
    {
        foreach (var markings in markingSets.Values)
        {
            for (var i = markings.Count - 1; i >= 0; i--)
            {
                if (!TryGetMarking(markings[i], out var marking) || !CanBeApplied(group, sex, marking))
                    markings.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Ensures that the <see cref="markingSets"/> only belong to the <see cref="layers"/>
    /// </summary>
    public void EnsureValidLayers(Dictionary<HumanoidVisualLayers, List<Marking>> markingSets, HashSet<HumanoidVisualLayers> layers)
    {
        foreach (var markings in markingSets.Values)
        {
            for (var i = markings.Count - 1; i >= 0; i--)
            {
                if (!TryGetMarking(markings[i], out var marking) || !layers.Contains(marking.BodyPart))
                    markings.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Ensures the list of <see cref="markingSets"/> is valid per the limits of the <see cref="group"/>
    /// </summary>
    public void EnsureValidLimits(Dictionary<HumanoidVisualLayers, List<Marking>> markingSets, ProtoId<MarkingsGroupPrototype> group, HashSet<HumanoidVisualLayers> layers, Color? skinColor, Color? eyeColor)
    {
        var groupProto = _prototype.Index(group);
        var counts = new Dictionary<HumanoidVisualLayers, int>();

        foreach (var (_, markings) in markingSets)
        {
            for (var i = markings.Count - 1; i >= 0; i--)
            {
                if (!TryGetMarking(markings[i], out var marking))
                {
                    markings.RemoveAt(i);
                    continue;
                }

                if (!groupProto.Limits.TryGetValue(marking.BodyPart, out var limit))
                    continue;

                var count = counts.GetValueOrDefault(marking.BodyPart);
                if (count >= limit.Limit)
                {
                    markings.RemoveAt(i);
                    continue;
                }

                counts[marking.BodyPart] = counts.GetValueOrDefault(marking.BodyPart) + 1;
            }
        }

        foreach (var layer in layers)
        {
            if (!groupProto.Limits.TryGetValue(layer, out var layerLimit))
                continue;

            var layerCounts = counts.GetValueOrDefault(layer);
            if (layerCounts > 0 || !layerLimit.Required)
                continue;

            foreach (var marking in layerLimit.Default)
            {
                if (!_markings.TryGetValue(marking, out var markingProto))
                    continue;

                markingSets[layer] = markingSets.GetValueOrDefault(layer) ?? [];
                var colors = MarkingColoring.GetMarkingLayerColors(markingProto, skinColor, eyeColor, markingSets[layer]);
                markingSets[layer].Add(new(marking, colors));
            }
        }
    }

    /// <summary>
    /// Returns the expected set of organs for a species to have.
    /// </summary>
    /// <param name="species">The species to look up.</param>
    /// <returns>A dictionary of organ categories to their usual organs within a species.</returns>
    public Dictionary<ProtoId<OrganCategoryPrototype>, EntProtoId<OrganComponent>> GetOrgans(ProtoId<SpeciesPrototype> species)
    {
        var speciesPrototype = _prototype.Index(species);
        var appearancePrototype = _prototype.Index(speciesPrototype.DollPrototype);

        if (!appearancePrototype.TryGetComponent<InitialBodyComponent>(out var initialBody, _component))
            return new();

        return initialBody.Organs;
    }

    /// <summary>
    /// Looks up the expected set of <see cref="OrganMarkingData" /> for the species to have
    /// </summary>
    /// <param name="species">The species to look up the usual organs of.</param>
    /// <returns>A dictionary of organ categories to their usual organ marking data within a species.</returns>
    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> GetMarkingData(ProtoId<SpeciesPrototype> species)
    {
        var ret = new Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData>();

        foreach (var (organ, proto) in GetOrgans(species))
        {
            if (!TryGetMarkingData(proto, out var organData))
                continue;

            ret[organ] = organData.Value;
        }

        return ret;
    }

    /// <summary>
    /// Expands the provided profile data into all the categories for a species.
    /// </summary>
    /// <param name="species">The species the returned dictionary should be comprehensive for.</param>
    /// <param name="sex">The sex to apply to all organs</param>
    /// <param name="skinColor">The skin color to apply to all organs</param>
    /// <param name="eyeColor">The eye color to apply to all organs</param>
    /// <returns></returns>
    /// <seealso cref="OrganProfileData" />
    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> GetProfileData(ProtoId<SpeciesPrototype> species,
        Sex sex,
        Color skinColor,
        Color eyeColor)
    {
        var ret = new Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>();

        foreach (var organ in GetOrgans(species).Keys)
        {
            ret[organ] = new()
            {
                Sex = sex,
                EyeColor = eyeColor,
                SkinColor = skinColor,
            };
        }

        return ret;
    }

    /// <summary>
    /// Gets the <see cref="OrganMarkingData" /> for the entity prototype corresponding to an organ
    /// </summary>
    /// <param name="organ">The ID of the organ entity prototype to look up</param>
    /// <param name="organData">The marking data for the organ if it exists</param>
    /// <returns>Whether the provided entity prototype ID corresponded to organ marking data that could be returned</returns>
    public bool TryGetMarkingData(EntProtoId organ, [NotNullWhen(true)] out OrganMarkingData? organData)
    {
        organData = null;

        if (!_prototype.TryIndex(organ, out var organProto))
            return false;

        if (!organProto.TryGetComponent<VisualOrganMarkingsComponent>(out var comp, _component))
            return false;

        organData = comp.MarkingData;

        return true;
    }

    /// <summary>
    /// Converts a legacy flat list of markings to a structured markings dictionary for a given species
    /// </summary>
    /// <param name="markings">A flat list of markings</param>
    /// <param name="species">The species to convert the markings for</param>
    /// <returns>A dictionary with the provided markings categorized appropriately for the species</returns>
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> ConvertMarkings(List<Marking> markings,
        ProtoId<SpeciesPrototype> species)
    {
        var ret = new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();

        var data = GetMarkingData(species);
        var layersToOrgans = data.SelectMany(kvp => kvp.Value.Layers.Select(layer => (layer, kvp.Key))).ToDictionary(pair => pair.layer, pair => pair.Key);

        foreach (var marking in markings)
        {
            if (!_prototype.TryIndex<MarkingPrototype>(marking.MarkingId, out var markingProto))
                continue;

            if (!layersToOrgans.TryGetValue(markingProto.BodyPart, out var organ))
                continue;

            var organDict = ret.GetValueOrDefault(organ) ?? [];
            ret[organ] = organDict;
            var markingList = organDict.GetValueOrDefault(markingProto.BodyPart) ?? [];
            organDict[markingProto.BodyPart] = markingList;

            markingList.Add(marking);
        }

        return ret;
    }

    /// <summary>
    /// Recursively compares two markings dictionaries for equality.
    /// </summary>
    /// <param name="a">The first markings dictionary.</param>
    /// <param name="b">The second markings dictionary.</param>
    /// <returns>Whether the dictionaries are equivalent.</returns>
    public static bool MarkingsAreEqual(Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> a,
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> b)
    {
        if (a.Count != b.Count)
            return false;

        foreach (var (organ, aDictionary) in a)
        {
            if (!b.TryGetValue(organ, out var bDictionary))
                return false;

            if (aDictionary.Count != bDictionary.Count)
                return false;

            foreach (var (layer, aMarkings) in aDictionary)
            {
                if (!bDictionary.TryGetValue(layer, out var bMarkings))
                    return false;

                if (!aMarkings.SequenceEqual(bMarkings))
                    return false;
            }
        }

        return true;
    }
}
