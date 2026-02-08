using Content.Shared.Dataset;
using Content.Shared.Silicons.Laws;
using Content.Shared.Station;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Silicons.Laws;

/// <summary>
/// This handles generating random ion laws.
/// </summary>
public sealed class IonLawSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStationSystem _stationSystem = default!;
    [Dependency] private readonly SharedStationRecordsSystem _stationRecordsSystem = default!;

    private readonly Dictionary<string, List<IonLawSelector>> _selectors = new();
    private readonly Dictionary<(string, int), object> _cachedValues = new();
    private IonLawPrototype? _lastLaw;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        BuildSelectors();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (obj.ByType.ContainsKey(typeof(DatasetPrototype)))
            BuildSelectors();
    }

    private void BuildSelectors()
    {
        _selectors.Clear();

        void AddSelector(string key, IonLawSelector selector)
        {
            if (!_selectors.ContainsKey(key))
                _selectors[key] = new List<IonLawSelector>();
            _selectors[key].Add(selector);
        }

        DatasetFill Df(string datasetId) => new() { Dataset = new ProtoId<DatasetPrototype>(datasetId) };
        RandomManifestFill Rmf(string fallback) => new() { FallbackDataset = new ProtoId<DatasetPrototype>(fallback) };
        ConstantFill Cf(bool val) => new() { BoolValue = val };

        AddSelector("ION-NUMBER-BASE", Df("IonStormNumberBase"));

        AddSelector("ION-NUMBER-MOD", Df("IonStormNumberMod"));

        AddSelector("ION-ADJECTIVE", Df("IonStormAdjectives"));

        AddSelector("ION-SUBJECT", Df("IonStormThreats"));
        AddSelector("ION-SUBJECT", Df("IonStormCrew"));
        AddSelector("ION-SUBJECT", Df("IonStormObjects"));
        AddSelector("ION-SUBJECT", Rmf("IonStormCrew"));

        AddSelector("ION-WHO", Df("IonStormCrew"));
        AddSelector("ION-WHO", Rmf("IonStormCrew"));

        AddSelector("ION-MUST", Df("IonStormMusts"));

        AddSelector("ION-THING", Df("IonStormObjects"));
        AddSelector("ION-THING", Df("IonStormConcepts"));

        AddSelector("ION-JOB", Df("IonStormCrew"));
        AddSelector("ION-JOB", Rmf("IonStormCrew"));

        AddSelector("ION-WHO-GENERAL", Df("IonStormAreas"));
        AddSelector("ION-WHO-GENERAL", Rmf("IonStormCrew"));

        AddSelector("ION-PLURAL", Cf(true));
        AddSelector("ION-PLURAL", Cf(false));

        AddSelector("ION-REQUIRE", Df("IonStormConcepts"));
        AddSelector("ION-REQUIRE", Df("IonStormRequires"));

        AddSelector("ION-SEVERITY", Df("IonStormAllergySeverities"));

        AddSelector("ION-ALLERGY", Df("IonStormConcepts"));
        AddSelector("ION-ALLERGY", Df("IonStormAllergies"));

        AddSelector("ION-FEELING", Df("IonStormFeelings"));

        AddSelector("ION-CONCEPT", Df("IonStormConcepts"));

        AddSelector("ION-FOOD", Df("IonStormFoods"));

        AddSelector("ION-DRINK", Df("IonStormDrinks"));

        AddSelector("ION-CHANGE", Df("IonStormCrew"));
        AddSelector("ION-CHANGE", Df("IonStormChanges"));
        AddSelector("ION-CHANGE", Rmf("IonStormCrew"));

        AddSelector("ION-WHO-RANDOM", Df("IonStormCrew"));
        AddSelector("ION-WHO-RANDOM", Rmf("IonStormCrew"));

        AddSelector("ION-AREA", Df("IonStormAreas"));

        AddSelector("ION-PART", Cf(true));
        AddSelector("ION-PART", Cf(false));

        AddSelector("ION-OBJECT", Df("IonStormObjects"));

        AddSelector("ION-HARM-PROTECT", Df("IonStormConcepts"));
        AddSelector("ION-HARM-PROTECT", Df("IonStormCrew"));
        AddSelector("ION-HARM-PROTECT", Rmf("IonStormCrew"));

        AddSelector("ION-VERB", Df("IonStormVerbs"));
    }

    public string GetIonLaw()
    {
        var laws = _prototypeManager.EnumeratePrototypes<IonLawPrototype>().ToList();
        if (laws.Count == 0)
            return Loc.GetString("ion-law-error-no-protos");

        var totalWeight = laws.Sum(p => p.Weight);
        if (totalWeight <= 0)
        {
            // if all weights are 0, just pick one at random.
            _lastLaw = _random.Pick(laws);
        }
        else
        {
            var value = _random.NextFloat() * totalWeight;
            IonLawPrototype? lastLaw = null;
            foreach (var law in laws)
            {
                if (law.Weight <= 0)
                    continue;

                lastLaw = law;
                value -= law.Weight;
                if (value <= 0)
                {
                    break;
                }
            }
            _lastLaw = lastLaw;
        }

        if (_lastLaw == null)
        {
            return Loc.GetString("ion-law-error-last-null");
        }

        _cachedValues.Clear();
        return Loc.GetString(_lastLaw.LawString, ("ion", 0));
    }

    public object GetOrGenerateValue(string selectorName, int index)
    {
        var key = (selectorName, index);
        if (_cachedValues.TryGetValue(key, out var value))
        {
            return value;
        }

        if (!_selectors.TryGetValue(selectorName, out var selectors))
            return Loc.GetString("ion-law-error-no-selectors");

        var availableSelectors = selectors.ToList();
        while (availableSelectors.Count > 0)
        {
            var selector = PickSelector(availableSelectors, _lastLaw);
            if (selector == null)
                break;

            var newValue = GetSelectorValue(selector);
            if (newValue is string s && string.IsNullOrWhiteSpace(s))
            {
                availableSelectors.Remove(selector);
                continue;
            }

            _cachedValues[key] = newValue;
            return newValue;
        }

        return Loc.GetString("ion-law-error-no-available-selectors");
    }

    private IonLawSelector? PickSelector(List<IonLawSelector> selectors, IonLawPrototype? law)
    {
        var weightedSelectors = new List<(IonLawSelector, float)>();
        foreach (var s in selectors)
        {
            var weight = s.Weight;

            if (s is DatasetFill df)
            {
                if (law != null && law.SelectorWeightAdjust.TryGetValue(df.Dataset.Id, out var adjustedWeight))
                    weight = adjustedWeight;
            }
            else if (s is RandomManifestFill)
            {
                if (law != null && law.SelectorWeightAdjust.TryGetValue("RandomManifestFill", out var adjustedWeight))
                {
                    weight = adjustedWeight;
                }
            }

            if (weight > 0)
                weightedSelectors.Add((s, weight));
        }

        var totalWeight = weightedSelectors.Sum(s => s.Item2);
        if (totalWeight <= 0)
            return null;

        var value = _random.NextFloat() * totalWeight;
        foreach (var (selector, selectorWeight) in weightedSelectors)
        {
            value -= selectorWeight;
            if (value <= 0)
                return selector;
        }

        return weightedSelectors.Last().Item1;
    }

    private object GetSelectorValue(IonLawSelector selector)
    {
        switch (selector)
        {
            case DatasetFill df:
                if (_prototypeManager.TryIndex(df.Dataset, out var dataset) && dataset.Values.Any())
                {
                    return _random.Pick(dataset.Values);
                }
                return Loc.GetString("ion-law-error-dataset-empty-or-not-found");
            case RandomManifestFill rmf:
                var stations = _stationSystem.GetStations();
                if (stations.Count > 0)
                {
                    var station = _random.Pick(stations);
                    if (EntityManager.TryGetComponent(station, out StationRecordsComponent? stationRecords) &&
                        _stationRecordsSystem.TryGetRandomRecord((station, stationRecords), out GeneralStationRecord? record))
                    {
                        var upperName = "'" + record.Name.ToUpper() + "'";
                        return upperName;
                    }
                }

                // Fallback to dataset if no manifest record found or stations are empty
                if (_prototypeManager.TryIndex(rmf.FallbackDataset, out var fallbackDataset) && fallbackDataset.Values.Any())
                {
                    return _random.Pick(fallbackDataset.Values);
                }

                return Loc.GetString("ion-law-error-fallback-dataset-empty-or-not-found");
            case ConstantFill cf:
                if (cf.BoolValue != null)
                    return cf.BoolValue.Value;
                return cf.Value;
            default:
                return Loc.GetString("ion-law-error-no-selector-selected");
        }
    }
}
