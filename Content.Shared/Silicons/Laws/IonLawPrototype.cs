using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws;

/// <summary>
/// A prototype for a random ion storm law.
/// </summary>
[Prototype]
public sealed partial class IonLawPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The localization string for the law.
    /// </summary>
    [DataField]
    public string LawString = string.Empty;

    /// <summary>
    /// The weight of this law.
    /// If 0, it won't be picked.
    /// </summary>
    [DataField]
    public float Weight = 1.0f;

    /// <summary>
    /// The variables to fill in the localization string.
    /// The key is the variable name, and the value is a list of selectors to pick from.
    /// </summary>
    [DataField]
    public Dictionary<string, List<IonLawSelector>> Targets = new();
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
    [DataField]
    public virtual float Weight { get; private set; } = 1.0f;
}

/// <summary>
/// Selects a random value from a dataset.
/// </summary>
public sealed partial class DatasetFill : IonLawSelector
{
    /// <summary>
    /// The dataset to pick values from.
    /// </summary>
    [DataField]
    public ProtoId<DatasetPrototype> Dataset { get; private set; }
}

/// <summary>
/// Selects a random name from the station's crew manifest.
/// If it fails to find one, picks an entry from IonStormCrew Dataset Prototype.
/// </summary>
[DataDefinition]
public sealed partial class RandomManifestFill : IonLawSelector
{
    /// <summary>
    /// The dataset to use if no crew manifest is found. NOT OPTIONAL!
    /// </summary>
    [DataField]
    public ProtoId<DatasetPrototype> FallbackDataset { get; private set; }
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
    [DataField]
    public string Separator = " ";

    /// <summary>
    /// The list of selectors to use.
    /// </summary>
    [DataField]
    public List<IonLawSelector> Selectors = new();
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
    [DataField]
    public string Key = string.Empty;

    /// <summary>
    /// Arguments for the localization string.
    /// </summary>
    [DataField]
    public Dictionary<string, IonLawSelector> Args = new();
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
    [DataField]
    public string Value = string.Empty;

    /// <summary>
    /// The boolean value to return. If set, overrides <see cref="Value"/>.
    /// </summary>
    [DataField]
    public bool? BoolValue { get; private set; }
}
