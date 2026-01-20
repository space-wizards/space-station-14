using System.Linq;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Humanoid;

public sealed class MarkingsViewModel
{
    [Dependency] private readonly MarkingManager _marking = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private bool _enforceLimits = true;

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

    public event Action? EnforcementsChanged;

    private Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> _organProfileData = new();

    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> OrganProfileData
    {
        get => _organProfileData;
        set
        {
            _organProfileData = value.ShallowClone();
            OrganProfileDataChanged?.Invoke();
        }
    }

    public void SetOrganSexes(Sex sex)
    {
        foreach (var (organ, data) in _organProfileData)
        {
            _organProfileData[organ] = data with { Sex = sex };
        }
        OrganProfileDataChanged?.Invoke();
    }

    public void SetOrganSkinColor(Color skinColor)
    {
        foreach (var (organ, data) in _organProfileData)
        {
            _organProfileData[organ] = data with { SkinColor = skinColor };
        }
        OrganProfileDataChanged?.Invoke();
    }

    public void SetOrganEyeColor(Color eyeColor)
    {
        foreach (var (organ, data) in _organProfileData)
        {
            _organProfileData[organ] = data with { EyeColor = eyeColor };
        }
        OrganProfileDataChanged?.Invoke();
    }

    public event Action? OrganProfileDataChanged;

    private Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> _markings = new();

    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings
    {
        get => _markings;
        set
        {
            _markings = value.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToDictionary(
                    it => it.Key,
                    it => it.Value.Select(marking => new Marking(marking)).ToList()));

            MarkingsReset?.Invoke();
        }
    }

    public event Action? MarkingsReset;

    public event Action<ProtoId<OrganCategoryPrototype>, HumanoidVisualLayers>? MarkingsChanged;

    private Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> _organData = new();

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

    public event Action? OrganDataChanged;

    private Dictionary<ProtoId<MarkingPrototype>, List<Color>> _previousColors = new();

    public MarkingsViewModel()
    {
        IoCManager.InjectDependencies(this);
    }

    public bool IsMarkingSelected(ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId)
    {
        return TryGetMarking(organ, layer, markingId) is not null;
    }

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

    public Marking? TryGetMarking(ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId)
    {
        if (!_markings.TryGetValue(organ, out var markingSet))
            return null;

        if (!markingSet.TryGetValue(layer, out var markings))
            return null;

        return markings.FirstOrDefault(it => it.MarkingId == markingId);
    }

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
        var newMarking = new Marking(markingId, colors);
        newMarking.Forced = AnyEnforcementsLifted;

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

    public List<Marking>? SelectedMarkings(ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer)
    {
        if (!_markings.TryGetValue(organ, out var organMarkings))
            return null;

        if (!organMarkings.TryGetValue(layer, out var layerMarkings))
            return null;

        return layerMarkings;
    }

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

        if (markings.FirstOrDefault(it => it.MarkingId == markingId) is not { } marking)
            return;

        marking.SetColor(colorIndex, color);
        MarkingsChanged?.Invoke(organ, layer);
    }

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

    public void GetMarkingCounts(ProtoId<OrganCategoryPrototype> organ, HumanoidVisualLayers layer, out bool isRequired, out int count, out int selected)
    {
        isRequired = false;
        count = 0;
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

public enum CandidatePosition
{
    Before,
    After,
}
