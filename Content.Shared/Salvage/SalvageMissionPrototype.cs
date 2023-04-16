using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Salvage;

/// <summary>
/// Contains data for a salvage mission type.
/// Most of the logic is handled via code due to specifics of how each mission type is setup.
/// </summary>
[Prototype("salvageMission")]
public sealed class SalvageMissionPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Naming scheme for the FTL marker.
    /// </summary>
    [DataField("nameProto", customTypeSerializer:typeof(PrototypeIdSerializer<DatasetPrototype>))]
    public string NameProto = "names_borer";

    /// <summary>
    /// Player-friendly description for the console.
    /// </summary>
    [DataField("desc")]
    public string Description = string.Empty;

    /// <summary>
    /// Minimum tier at which this mission can appear.
    /// </summary>
    [DataField("minDifficulty")]
    public int MinDifficulty;

    /// <summary>
    /// Maximum tier at which this mission can appear.
    /// </summary>
    [DataField("maxDifficulty")]
    public int MaxDifficulty = int.MaxValue;
}

[Serializable, NetSerializable]
public enum DifficultyRating : byte
{
    None,
    Minor,
    Moderate,
    Hazardous,
    Extreme,
}

