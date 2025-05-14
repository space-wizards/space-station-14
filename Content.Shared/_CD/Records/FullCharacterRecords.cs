using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CD.Records;

/// <summary>
/// Contains the full records information, not just stuff that is in the database.
/// </summary>
[Serializable, NetSerializable]
public sealed class FullCharacterRecords(
    PlayerProvidedCharacterRecords pRecords,
    uint? stationRecordsKey,
    string name,
    int age,
    string jobTitle,
    ProtoId<JobIconPrototype> jobIcon,
    ProtoId<SpeciesPrototype> species,
    Gender gender,
    Sex sex,
    string? fingerprint,
    string? dna,
    EntityUid? owner = null)
{
    [ViewVariables]
    public PlayerProvidedCharacterRecords PRecords = pRecords;

    /// <summary>
    /// Key for the equivalent entry in the station records
    ///
    /// Sadly, this has to be a uint because StationRecordsKey is not serializable
    /// </summary>
    [ViewVariables]
    public uint? StationRecordsKey = stationRecordsKey;

    /// <summary>
    ///     Name tied to this record.
    /// </summary>
    [ViewVariables]
    public string Name = name;

    /// <summary>
    ///     Age of the person that this record represents.
    /// </summary>
    [ViewVariables]
    public int Age = age;

    /// <summary>
    ///     Job title tied to this record.
    /// </summary>
    [ViewVariables]
    public string JobTitle = jobTitle;

    /// <summary>
    ///     Job icon tied to this record. Imp edit - made this a protoID
    /// </summary>
    [ViewVariables]
    public ProtoId<JobIconPrototype> JobIcon = jobIcon;

    /// <summary>
    ///     Species tied to this record. imp edit - made this a protoID
    /// </summary>
    [ViewVariables]
    public ProtoId<SpeciesPrototype> Species = species;

    /// <summary>
    ///     Gender identity tied to this record.
    /// </summary>
    [ViewVariables]
    public Gender Gender = gender;

    /// <summary>
    ///     Sex identity tied to this record.
    /// </summary>
    [ViewVariables]
    public Sex Sex = sex;

    [ViewVariables]
    public string? Fingerprint = fingerprint;

    /// <summary>
    ///     DNA of the person.
    /// </summary>
    [ViewVariables]
    // ReSharper disable once InconsistentNaming
    public string? DNA = dna;

    /// <summary>
    /// The entity that owns this record. Should always nonnull inside CharacterRecordsComponent. This field should not be accessed client side.
    /// </summary>
    [ViewVariables]
    [NonSerialized]
    public EntityUid? Owner = owner;
}
