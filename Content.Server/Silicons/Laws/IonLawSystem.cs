using Content.Server.StationRecords.Systems;
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
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;
    private readonly Dictionary<string, List<IonLawSelector>> _selectors = new();
    private IonLawPrototype? _ionLaw;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("ion-law");

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


        DatasetFill DatasetFill(ProtoId<DatasetPrototype> datasetId) => new() { Dataset = datasetId };
        RandomManifestFill RandomManifestFill(ProtoId<DatasetPrototype> fallback) => new() { FallbackDataset = fallback };
        ConstantFill ConstantFill(bool val) => new() { BoolValue = val };

        AddSelector("ION-NUMBER-BASE", DatasetFill("IonStormNumberBase"));

        AddSelector("ION-NUMBER-MOD", DatasetFill("IonStormNumberMod"));

        AddSelector("ION-ADJECTIVE", DatasetFill("IonStormAdjectives"));

        AddSelector("ION-SUBJECT", DatasetFill("IonStormThreats"));
        AddSelector("ION-SUBJECT", DatasetFill("IonStormCrew"));
        AddSelector("ION-SUBJECT", DatasetFill("IonStormObjects"));
        AddSelector("ION-SUBJECT", RandomManifestFill("IonStormCrew"));

        AddSelector("ION-WHO", DatasetFill("IonStormCrew"));
        AddSelector("ION-WHO", RandomManifestFill("IonStormCrew"));

        AddSelector("ION-MUST", DatasetFill("IonStormMusts"));

        AddSelector("ION-THING", DatasetFill("IonStormObjects"));
        AddSelector("ION-THING", DatasetFill("IonStormConcepts"));

        AddSelector("ION-JOB", DatasetFill("IonStormCrew"));
        AddSelector("ION-JOB", RandomManifestFill("IonStormCrew"));

        AddSelector("ION-WHO-GENERAL", DatasetFill("IonStormAreas"));
        AddSelector("ION-WHO-GENERAL", RandomManifestFill("IonStormCrew"));

        AddSelector("ION-PLURAL", ConstantFill(true));
        AddSelector("ION-PLURAL", ConstantFill(false));

        AddSelector("ION-REQUIRE", DatasetFill("IonStormConcepts"));
        AddSelector("ION-REQUIRE", DatasetFill("IonStormRequires"));

        AddSelector("ION-SEVERITY", DatasetFill("IonStormAllergySeverities"));

        AddSelector("ION-ALLERGY", DatasetFill("IonStormConcepts"));
        AddSelector("ION-ALLERGY", DatasetFill("IonStormAllergies"));

        AddSelector("ION-FEELING", DatasetFill("IonStormFeelings"));

        AddSelector("ION-CONCEPT", DatasetFill("IonStormConcepts"));

        AddSelector("ION-FOOD", DatasetFill("IonStormFoods"));

        AddSelector("ION-DRINK", DatasetFill("IonStormDrinks"));

        AddSelector("ION-CHANGE", DatasetFill("IonStormCrew"));
        AddSelector("ION-CHANGE", RandomManifestFill("IonStormCrew"));

        AddSelector("ION-WHO-RANDOM", DatasetFill("IonStormCrew"));
        AddSelector("ION-WHO-RANDOM", RandomManifestFill("IonStormCrew"));

        AddSelector("ION-AREA", DatasetFill("IonStormAreas"));

        AddSelector("ION-PART", ConstantFill(true));
        AddSelector("ION-PART", ConstantFill(false));

        AddSelector("ION-OBJECT", DatasetFill("IonStormObjects"));

        AddSelector("ION-HARM-PROTECT", DatasetFill("IonStormConcepts"));
        AddSelector("ION-HARM-PROTECT", DatasetFill("IonStormCrew"));
        AddSelector("ION-HARM-PROTECT", RandomManifestFill("IonStormCrew"));

        AddSelector("ION-VERB", DatasetFill("IonStormVerbs"));
    }

    /// <summary>
    /// Adds a selector to the cache.
    /// Used for picking datasets to fill the keys in the strings for the Ion Laws.
    /// </summary>
    /// <param name="key">The key in the strings file to fill.</param>
    /// <param name="selector">The type of dataset to use and the prototype ID for it, if it takes one.</param>
    private void AddSelector(string key, IonLawSelector selector)
    {
        if (!_selectors.ContainsKey(key))
            _selectors[key] = new List<IonLawSelector>();

        _selectors[key].Add(selector);
    }


    /// <summary>
    /// Generates a random ion law by picking an ion law prototype and filling its placeholders with random values, from datasets.
    /// </summary>
    /// <returns>A formatted string representing the new ion law.</returns>
    public string GetIonLaw()
    {
        var laws = _prototypeManager.EnumeratePrototypes<IonLawPrototype>().ToList();
        if (laws.Count == 0)
        {
            _sawmill.Error("No Ion Laws found");
            return Loc.GetString("ion-law-error-no-protos");
        }


        var totalWeight = laws.Sum(p => p.Weight);
        if (totalWeight <= 0)
        {
            // if all weights are 0, teat them as equal and just pick one at random.
            _ionLaw = _random.Pick(laws);
        }
        else
        {
            var value = _random.NextFloat() * totalWeight;
            IonLawPrototype? ionLaw = null;
            foreach (var law in laws)
            {
                if (law.Weight <= 0)
                    continue;

                ionLaw = law;
                value -= law.Weight;
                if (value <= 0)
                {
                    break;
                }
            }
            _ionLaw = ionLaw;
        }

        if (_ionLaw == null)
        {
            _sawmill.Error("Ion Law was null");
            return Loc.GetString("ion-law-error-was-null");
        }

        return Loc.GetString(_ionLaw.LawString, ("ion", 0));
    }

    /// <summary>
    /// Gets a value for a specific selector and index, generating it if it doesn't exist in the cache.
    /// This allows laws to reference the same generated value multiple times using the same index.
    /// </summary>
    /// <param name="selectorName">The key of the selector list to pick from (e.g.: "ION-WHO").</param>
    /// <returns>A string or object representing the generated law component.</returns>
    public object GetOrGenerateValue(string selectorName)
    {
        if (!_selectors.TryGetValue(selectorName, out var selectors))
        {
            _sawmill.Error("No selectors for Ion Laws found");
            return Loc.GetString("ion-law-error-no-selectors");
        }

        var availableSelectors = selectors.ToList();
        while (availableSelectors.Count > 0)
        {
            var selector = PickSelector(availableSelectors, _ionLaw);
            if (selector == null)
                break;

            var newValue = GetSelectorValue(selector);
            if (newValue is string s && string.IsNullOrWhiteSpace(s))
            {
                availableSelectors.Remove(selector);
                continue;
            }

            return newValue;
        }

        _sawmill.Error("No available selectors found for the Ion Law found - this should never happen, selector was: " + selectorName);
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
            case DatasetFill datasetFill:
                if (_prototypeManager.TryIndex(datasetFill.Dataset, out var dataset) && dataset.Values.Any())
                {
                    return _random.Pick(dataset.Values);
                }
                _sawmill.Error("Selected DataSet (" + selector + ") was empty or not found" );
                return Loc.GetString("ion-law-error-dataset-empty-or-not-found");
            case RandomManifestFill randomManifestFill:
                var stations = _stationSystem.GetStations();
                if (stations.Count > 0)
                {
                    var station = _random.Pick(stations);
                    if (TryComp(station, out StationRecordsComponent? stationRecords) &&
                        _stationRecordsSystem.TryGetRandomRecord((station, stationRecords), out GeneralStationRecord? record))
                    {
                        var upperName = "'" + record.Name.ToUpper() + "'";
                        return upperName;
                    }
                }

                // Fallback to dataset if no manifest record found or stations are empty
                if (_prototypeManager.TryIndex(randomManifestFill.FallbackDataset, out var fallbackDataset) && fallbackDataset.Values.Any())
                {
                    return _random.Pick(fallbackDataset.Values);
                }
                _sawmill.Error("Fallback DataSet (" + selector + ") was empty or not found" );
                return Loc.GetString("ion-law-error-fallback-dataset-empty-or-not-found");
            case ConstantFill constantFill:
                if (constantFill.BoolValue.HasValue)
                    return constantFill.BoolValue.Value;
                _sawmill.Error("The selected Constant Fill did not have a value: " + constantFill );
                return Loc.GetString("ion-law-error-no-bool-value");
            default:
            {
                _sawmill.Error("Selected DataSet (" + selector + ") was not selected" );
                return Loc.GetString("ion-law-error-no-selector-selected");
            }
        }
    }
}
