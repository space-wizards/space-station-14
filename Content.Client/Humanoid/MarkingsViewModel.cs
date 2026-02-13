using System.Linq;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Humanoid;

/// <summary>
/// View model for UIs manipulating a set of markings, responsible for applying markings logic and keeping state synchronized.
/// </summary>
public sealed class MarkingsViewModel
{
    [Dependency] private readonly MarkingManager _marking = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private bool _enforceLimits = true;

    /// <summary>
    /// Whether the markings view model will enforce limitations on how many markings an organ can have
    /// </summary>
    /// <seealso cref="EnforcementsChanged" />
    public bool EnforceLimits
    {
        get => _enforceLimits;
        set
        {
            if (_enforceLimits == value)
                return;

            _enforceLimits = value;
            EnforcementsChanged?.Invoke();
        }
    }

    private bool _enforceGroupAndSexRestrictions = true;

    /// <summary>
    /// Whether the markings view model will enforce restrictions on the group and sex of markings for an organ
    /// </summary>
    /// <seealso cref="EnforcementsChanged" />
    public bool EnforceGroupAndSexRestrictions
    {
        get => _enforceGroupAndSexRestrictions;
        set
        {
            if (_enforceGroupAndSexRestrictions == value)
                return;

            _enforceGroupAndSexRestrictions = value;
            EnforcementsChanged?.Invoke();
        }
    }

    private bool AnyEnforcementsLifted => !_enforceLimits || !_enforceGroupAndSexRestrictions;

    /// <summary>
    /// Raised whenever the view model is enforcing a different set of constraints on possible markings than before
    /// </summary>
    /// <seealso cref="EnforceLimits" />
    /// <seealso cref="EnforceGroupAndSexRestrictions" />
    public event Action? EnforcementsChanged;

    private Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> _organProfileData = new();

    /// <summary>
    /// The organ profile data this view model is concerned with.
    /// </summary>
    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> OrganProfileData
    {
        get => _organProfileData;
        set
        {
            _organProfileData = value.ShallowClone();
            OrganProfileDataChanged?.Invoke(true);
        }
    }

    /// <summary>
    /// Sets the sex of all organ profiles in the view model.
    /// </summary>
    /// <param name="sex">The new sex</param>
    public void SetOrganSexes(Sex sex)
    {
        foreach (var (organ, data) in _organProfileData)
        {
            _organProfileData[organ] = data with { Sex = sex };
        }
        OrganProfileDataChanged?.Invoke(true);
    }

    /// <summary>
    /// Sets the skin color of all organ profiles in the view model.
    /// </summary>
    /// <param name="skinColor">The new skin color</param>
    public void SetOrganSkinColor(Color skinColor)
    {
        foreach (var (organ, data) in _organProfileData)
        {
            _organProfileData[organ] = data with { SkinColor = skinColor };
        }
        OrganProfileDataChanged?.Invoke(false);
    }

    /// <summary>
    /// Sets the eye color of all organ profiles in the view model.
    /// </summary>
    /// <param name="eyeColor">The new eye color</param>
    public void SetOrganEyeColor(Color eyeColor)
    {
        foreach (var (organ, data) in _organProfileData)
        {
            _organProfileData[organ] = data with { EyeColor = eyeColor };
        }
        OrganProfileDataChanged?.Invoke(false);
    }

    /// <summary>
    /// Raised whenever the organ profile data changes.
    /// The boolean value represents whether the set of possible markings may have changed.
    /// </summary>
    /// <seealso cref="OrganProfileData" />
    /// <seealso cref="SetOrganSexes" />
    /// <seealso cref="SetOrganSkinColor" />
    /// <seealso cref="SetOrganEyeColor" />
    public event Action<bool>? OrganProfileDataChanged;

    private Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> _markings = new();

    /// <summary>
    /// The currently applied set of markings
    /// </summary>
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings
    {
        get => _markings;
        set
        {
            _markings = value.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToDictionary(
                    it => it.Key,
                    it => it.Value.ShallowClone()));

            MarkingsReset?.Invoke();
        }
    }

    /// <summary>
    /// Raised whenever the set of markings has fully changed and requires a UI reload
    /// </summary>
    public event Action? MarkingsReset;

    /// <summary>
    /// Raised whenever a specific layer's markings have changed
    /// </summary>
    public event Action<ProtoId<OrganCategoryPrototype>, HumanoidVisualLayers>? MarkingsChanged;

    private Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> _organData = new();

    /// <summary>
    /// The organ marking data the view model is concerned with.
    /// </summary>
    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData>
        OrganData
    {
        get => _organData;
        set
        {
            if (_organData == value)
                return;

            _organData = value;
            _previousColors.Clear();
            OrganDataChanged?.Invoke();
        }
    }

    /// <summary>
    /// Raised whenever the organ data within the view model is changed.
    /// </summary>
    public event Action? OrganDataChanged;

    private readonly Dictionary<ProtoId<MarkingPrototype>, List<Color>> _previousColors = new();

    public MarkingsViewModel()
    {
        IoCManager.InjectDependencies(this);
    }

    /// <summary>
    /// Returns whether the given marking is currently selected in the model
    /// </summary>
    /// <param name="organ">The organ to check for the marking within</param>
    /// <param name="layer">The layer within the organ to check for the marking within</param>
    /// <param name="markingId">The marking ID to check for</param>
    /// <returns>Whether the marking is currently present within the set of selected markings</returns>
    public bool IsMarkingSelected(ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId)
    {
        return GetMarking(organ, layer, markingId) is not null;
    }

    /// <summary>
    /// Returns whether the marking at the given location can have its color customized by the user
    /// </summary>
    /// <inheritdoc cref="IsMarkingSelected" path="param" />
    /// <returns>Whether the marking is capable of having its color customized by the user</returns>
    public bool IsMarkingColorCustomizable(ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId)
    {
        if (!_prototype.TryIndex(markingId, out var markingProto))
            return false;

        if (markingProto.ForcedColoring)
            return false;

        if (!_organData.TryGetValue(organ, out var organData))
            return false;

        if (!_prototype.TryIndex(organData.Group, out var groupProto))
            return false;

        if (!groupProto.Appearances.TryGetValue(layer, out var appearance))
            return true;

        return !appearance.MatchSkin;
    }

    /// <summary>
    /// Returns the currently applied marking by its ID, if it exists
    /// </summary>
    /// <inheritdoc cref="IsMarkingSelected" path="param" />
    /// <returns>The marking currently applied if it exists, otherwise null</returns>
    public Marking? GetMarking(ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId)
    {
        if (!_markings.TryGetValue(organ, out var markingSet))
            return null;

        if (!markingSet.TryGetValue(layer, out var markings))
            return null;

        return markings.FirstOrNull(it => it.MarkingId == markingId);
    }

    /// <summary>
    /// Attempts to add a marking to the current set of markings
    /// </summary>
    /// <inheritdoc cref="IsMarkingSelected" path="param" />
    /// <returns>Whether the marking was successfully added to the set of markings</returns>
    public bool TrySelectMarking(ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId)
    {
        if (!_prototype.TryIndex(markingId, out var markingProto))
            return false;

        if (!_organData.TryGetValue(organ, out var organData) || !_organProfileData.TryGetValue(organ, out var profileData))
            return false;

        if (!organData.Layers.Contains(layer))
            return false;

        if (!_prototype.TryIndex(organData.Group, out var groupPrototype))
            return false;

        if (EnforceGroupAndSexRestrictions && !_marking.CanBeApplied(organData.Group, profileData.Sex, markingProto))
            return false;

        _markings[organ] = _markings.GetValueOrDefault(organ) ?? [];
        var organMarkings = _markings[organ];
        organMarkings[layer] = organMarkings.GetValueOrDefault(layer) ?? [];
        var layerMarkings = organMarkings[layer];

        var colors = _previousColors.GetValueOrDefault(markingId) ??
                     MarkingColoring.GetMarkingLayerColors(markingProto, profileData.SkinColor, profileData.EyeColor, layerMarkings);
        var newMarking = new Marking(markingId, colors) { Forced = AnyEnforcementsLifted };

        var limits = groupPrototype.Limits.GetValueOrDefault(layer);
        if (limits is null || !EnforceLimits)
        {
            layerMarkings.Add(newMarking);
            MarkingsChanged?.Invoke(organ, layer);
            return true;
        }

        if (limits.Limit == 1 && layerMarkings.Count == 1)
        {
            layerMarkings.Clear();
            layerMarkings.Add(newMarking);
            MarkingsChanged?.Invoke(organ, layer);
            return true;
        }

        if (layerMarkings.Count < limits.Limit)
        {
            layerMarkings.Add(newMarking);
            MarkingsChanged?.Invoke(organ, layer);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the list of currently selected markings for the layer on the given organ
    /// </summary>
    /// <param name="organ">The organ to look up the layer within</param>
    /// <param name="layer">The layer within the organ to look up</param>
    /// <returns>The set of markings for the provided organ if it has any, or null</returns>
    public List<Marking>? SelectedMarkings(ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer)
    {
        return !_markings.TryGetValue(organ, out var organMarkings)
            ? null
            : organMarkings.GetValueOrDefault(layer);
    }

    /// <summary>
    /// Attempts to remove a marking from the current set of markings
    /// </summary>
    /// <inheritdoc cref="IsMarkingSelected" path="param" />
    /// <returns>Whether the marking was successfully removed from the set of markings</returns>
    public bool TryDeselectMarking(ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId)
    {
        if (!_organData.TryGetValue(organ, out var organData))
            return false;

        if (!organData.Layers.Contains(layer))
            return false;

        if (!_prototype.TryIndex(organData.Group, out var groupPrototype))
            return false;

        var limits = groupPrototype.Limits.GetValueOrDefault(layer);

        _markings[organ] = _markings.GetValueOrDefault(organ) ?? [];
        var organMarkings = _markings[organ];
        organMarkings[layer] = organMarkings.GetValueOrDefault(layer) ?? [];
        var layerMarkings = organMarkings[layer];

        var count = layerMarkings.Count(marking => marking.MarkingId == markingId);
        if (count == 0)
            return false;

        if (EnforceLimits && limits is not null && limits.Required && (layerMarkings.Count - count) <= 0)
            return false;

        if (layerMarkings.Find(marking => marking.MarkingId == markingId) is { } removingMarking)
        {
            _previousColors[removingMarking.MarkingId] = removingMarking.MarkingColors.ToList();
        }
        layerMarkings.RemoveAll(marking => marking.MarkingId == markingId);
        MarkingsChanged?.Invoke(organ, layer);

        return true;
    }

    /// <summary>
    /// Attempts to set the color of the specified marking at the given index
    /// </summary>
    /// <inheritdoc cref="IsMarkingSelected" path="param" />
    /// <param name="colorIndex">The index within the marking's color array to set</param>
    /// <param name="color">The new color to set</param>
    public void TrySetMarkingColor(ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId,
        int colorIndex,
        Color color)
    {
        if (!_markings.TryGetValue(organ, out var markingSet))
            return;

        if (!markingSet.TryGetValue(layer, out var markings))
            return;

        var markingIdx = markings.FindIndex(it => it.MarkingId == markingId);
        if (markingIdx == -1)
            return;

        markings[markingIdx] = markings[markingIdx].WithColorAt(colorIndex, color);
        MarkingsChanged?.Invoke(organ, layer);
    }

    /// <summary>
    /// Ensures the markings within the model are valid.
    /// </summary>
    public void ValidateMarkings()
    {
        foreach (var (organ, organData) in _organData)
        {
            if (!_organProfileData.TryGetValue(organ, out var organProfileData))
            {
                _markings.Remove(organ);
                continue;
            }

            var actualMarkings = _markings.GetValueOrDefault(organ)?.ShallowClone() ?? [];

            _marking.EnsureValidColors(actualMarkings);
            _marking.EnsureValidGroupAndSex(actualMarkings, organData.Group, organProfileData.Sex);
            _marking.EnsureValidLayers(actualMarkings, organData.Layers);
            _marking.EnsureValidLimits(actualMarkings, organData.Group, organData.Layers, organProfileData.SkinColor, organProfileData.EyeColor);

            _markings[organ] = actualMarkings;
        }

        MarkingsReset?.Invoke();
    }

    /// <summary>
    /// Gets the count data for an organ layer.
    /// </summary>
    /// <param name="organ">The organ to look up count data for</param>
    /// <param name="layer">The layer within the organ to look up count data for</param>
    /// <param name="isRequired">Whether this layer requires at least one marking to be selected</param>
    /// <param name="count">The maximum amount of markings that can be selected for this layer</param>
    /// <param name="selected">The currently selected amount of markings</param>
    public void GetMarkingCounts(ProtoId<OrganCategoryPrototype> organ, HumanoidVisualLayers layer, out bool isRequired, out int count, out int selected)
    {
        isRequired = false;
        count = -1;
        selected = 0;

        if (!_organData.TryGetValue(organ, out var organData))
            return;

        if (!organData.Layers.Contains(layer))
            return;

        if (!_prototype.TryIndex(organData.Group, out var groupPrototype))
            return;

        if (!groupPrototype.Limits.TryGetValue(layer, out var limits))
            return;

        isRequired = limits.Required;
        count = limits.Limit;

        if (!_markings.TryGetValue(organ, out var organMarkings))
            return;

        if (!organMarkings.TryGetValue(layer, out var layerMarkings))
            return;

        selected = layerMarkings.Count;
    }

    /// <summary>
    /// Reorders the specified marking ID to the index and position relative to its index
    /// </summary>
    /// <param name="organ">The organ to reorder the markings of</param>
    /// <param name="layer">The layer to reorder the markings of</param>
    /// <param name="markingId">The marking to reorder</param>
    /// <param name="position">Whether the marking should be moved to before or after the given index</param>
    /// <param name="positionIndex">The new position index of the marking</param>
    public void ChangeMarkingOrder(ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId,
        CandidatePosition position,
        int positionIndex
    )
    {
        if (!_markings.TryGetValue(organ, out var organMarkings))
            return;

        if (!organMarkings.TryGetValue(layer, out var layerMarkings))
            return;

        var currentIndex = layerMarkings.FindIndex(marking => marking.MarkingId == markingId);
        var currentMarking = layerMarkings[currentIndex];

        if (position == CandidatePosition.Before)
        {
            layerMarkings.RemoveAt(currentIndex);
            var insertionIndex = currentIndex < positionIndex ? positionIndex - 1 : positionIndex;
            layerMarkings.Insert(insertionIndex, currentMarking);
        }
        else if (position == CandidatePosition.After)
        {
            layerMarkings.RemoveAt(currentIndex);
            var insertionIndex = currentIndex > positionIndex ? positionIndex + 1 : positionIndex;
            layerMarkings.Insert(insertionIndex, currentMarking);
        }

        MarkingsChanged?.Invoke(organ, layer);
    }
}

/// <summary>
/// Specifies whether an item in a list will be moved to before or after a corresponding index
/// </summary>
public enum CandidatePosition
{
    Before,
    After,
}
