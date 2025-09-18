using Content.Shared.Humanoid;
using Robust.Shared.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.Serialization;

namespace Content.Shared._CD.Records;

/// <summary>
/// Contains the full record for a character that is displayed in consoles.
/// Combines both player-provided data and station record data.
/// </summary>
[Serializable, NetSerializable]
public sealed class FullCharacterRecords(
    PlayerProvidedCharacterRecords pRecords,
    uint? stationRecordsKey,
    string name,
    int age,
    string jobTitle,
    string jobIcon,
    string species,
    Gender gender,
    Sex sex,
    string? fingerprint,
    string? dna,
    EntityUid? owner = null)
{
    [ViewVariables]
    public PlayerProvidedCharacterRecords PRecords = pRecords;

    /// <summary>
    /// Key for the equivalent entry in the station records.
    /// </summary>
    [ViewVariables]
    public uint? StationRecordsKey = stationRecordsKey;

    [ViewVariables]
    public string Name = name;

    [ViewVariables]
    public int Age = age;

    [ViewVariables]
    public string JobTitle = jobTitle;

    [ViewVariables]
    public string JobIcon = jobIcon;

    [ViewVariables]
    public string Species = species;

    [ViewVariables]
    public Gender Gender = gender;

    [ViewVariables]
    public Sex Sex = sex;

    [ViewVariables]
    public string? Fingerprint = fingerprint;

    [ViewVariables]
    public string? DNA = dna;

    /// <summary>
    /// Entity that owns this record. Only populated on the server.
    /// </summary>
    [ViewVariables]
    [NonSerialized]
    public EntityUid? Owner = owner;
}
