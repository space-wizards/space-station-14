using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Salvage;

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
    /// Minimum cost required for this mission type.
    /// Innately harder missions won't be available under easier difficulties.
    /// </summary>
    [DataField("minDifficulty")]
    public float MinDifficulty = 0f;
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

