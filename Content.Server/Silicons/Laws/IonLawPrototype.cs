using Content.Server.StationRecords.Systems;
using Content.Shared.Dataset;
using Content.Shared.Station;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Text;

namespace Content.Server.Silicons.Laws;

/// <summary>
/// A prototype for a random ion storm law.
/// </summary>
[Prototype("IonLaw")]
public sealed partial class IonLawPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The localization string for the law.
    /// </summary>
    [DataField("string")]
    public string LawString { get; private set; } = string.Empty;

    /// <summary>
    /// The variables to fill in the localization string.
    /// The key is the variable name, and the value is a list of selectors to pick from.
    /// </summary>
    [DataField("targets")]
    public Dictionary<string, List<IonLawSelector>> Targets { get; private set; } = new();
}

/// <summary>
/// Interface for selecting a value for an ion storm law variable.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class IonLawSelector
{

    /// <summary>
    /// Weight of the option being chosen.
    /// </summary>
    [DataField("weight")]
    public virtual float Weight { get; private set; } = 1.0f;

    public abstract object? Select(IRobustRandom random, IPrototypeManager proto, IEntityManager entManager);

    /// <summary>
    /// Picks a random selector from the list based on their weights.
    /// </summary>
    /// <param name="random">The random source to use.</param>
    /// <param name="selectors">The list of selectors to choose from.</param>
    /// <returns>The selected selector.</returns>
    public static IonLawSelector Pick(IRobustRandom random, IEnumerable<IonLawSelector> selectors)
    {
        var list = selectors.ToList();
        var totalWeight = list.Sum(x => x.Weight);
        var r = random.NextFloat() * totalWeight;

        foreach (var selector in list)
        {
            r -= selector.Weight;
            if (r <= 0)
                return selector;
        }
        return list.Last();
    }
}

/// <summary>
/// Selects a random value from a dataset.
/// </summary>
[DataDefinition]
public sealed partial class DatasetFill : IonLawSelector
{
    /// <summary>
    /// The dataset to pick values from.
    /// </summary>
    [DataField("dataset")]
    public ProtoId<DatasetPrototype> Dataset { get; private set; }

    public override object? Select(IRobustRandom random, IPrototypeManager proto, IEntityManager entManager)
    {
        if (!proto.TryIndex(Dataset, out var dataset))
            return null;

        return random.Pick(dataset.Values);
    }
}

/// <summary>
/// Selects a random name from the station's crew manifest.
/// </summary>
[DataDefinition]
public sealed partial class RandomManifestFill : IonLawSelector
{
    public override object? Select(IRobustRandom random, IPrototypeManager proto, IEntityManager entManager)
    {
        var stationSystem = entManager.System<SharedStationSystem>();
        var stationRecordsSystem = entManager.System<StationRecordsSystem>();
        var stations = stationSystem.GetStations();
        if (stations.Count == 0)
            return null;

        var station = random.Pick(stations);
        if (!entManager.TryGetComponent<StationRecordsComponent>(station, out var stationRecords))
            return null;

        if (!stationRecordsSystem.TryGetRandomRecord<GeneralStationRecord>(new Entity<StationRecordsComponent?>(station, stationRecords), out var record))
            return null;

        var name = "'" + record.Name.ToUpper() + "'";

        return name;
    }
}

/// <summary>
/// Selects multiple values from other selectors and joins them together.
/// </summary>
[DataDefinition]
public sealed partial class JoinedDatasetFill : IonLawSelector
{
    /// <summary>
    /// The separator to use between joined values.
    /// </summary>
    [DataField("separator")]
    public string Separator { get; private set; } = " ";

    /// <summary>
    /// The list of selectors to use.
    /// </summary>
    [DataField("selectors")]
    public List<IonLawSelector> Selectors { get; private set; } = new();

    public override object? Select(IRobustRandom random, IPrototypeManager proto, IEntityManager entManager)
    {
        var sb = new StringBuilder();
        var first = true;

        foreach (var selector in Selectors)
        {
            var value = selector.Select(random, proto, entManager);
            if (value == null)
                continue;

            if (!first)
                sb.Append(Separator);

            sb.Append(value);
            first = false;
        }

        return sb.ToString();
    }
}

/// <summary>
/// Selects a localized string, optionally filling in arguments with other selectors.
/// </summary>
[DataDefinition]
public sealed partial class TranslateFill : IonLawSelector
{
    /// <summary>
    /// The localization key.
    /// </summary>
    [DataField("key")]
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Arguments for the localization string.
    /// </summary>
    [DataField("args")]
    public Dictionary<string, IonLawSelector> Args { get; private set; } = new();

    public override object? Select(IRobustRandom random, IPrototypeManager proto, IEntityManager entManager)
    {
        var args = new List<(string, object)>();
        foreach (var (key, selector) in Args)
        {
            var value = selector.Select(random, proto, entManager);
            if (value == null)
                continue;

            args.Add((key, value));
        }

        return Loc.GetString(Key, args.ToArray());
    }
}

/// <summary>
/// Returns a constant value.
/// </summary>
[DataDefinition]
public sealed partial class ConstantFill : IonLawSelector
{
    /// <summary>
    /// The string value to return.
    /// </summary>
    [DataField("value")]
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// The boolean value to return. If set, overrides <see cref="Value"/>.
    /// </summary>
    [DataField("boolValue")]
    public bool? BoolValue { get; private set; }

    public override object? Select(IRobustRandom random, IPrototypeManager proto, IEntityManager entManager)
    {
        if (BoolValue.HasValue)
            return BoolValue.Value;
        return Value;
    }
}
