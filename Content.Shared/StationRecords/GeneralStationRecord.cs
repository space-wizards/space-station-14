using Robust.Shared.Enums;
using Robust.Shared.Serialization;

namespace Content.Shared.StationRecords;

/// <summary>
///     General station record. Indicates the crewmember's name and job.
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneralStationRecord
{
    /// <summary>
    ///     Name tied to this station record.
    /// </summary>
    [ViewVariables]
    public string Name = string.Empty;

    /// <summary>
    ///     Age of the person that this station record represents.
    /// </summary>
    [ViewVariables]
    public int Age;

    /// <summary>
    ///     Job title tied to this station record.
    /// </summary>
    [ViewVariables]
    public string JobTitle = string.Empty;

    /// <summary>
    ///     Job icon tied to this station record.
    /// </summary>
    [ViewVariables]
    public string JobIcon = string.Empty;

    [ViewVariables]
    public string JobPrototype = string.Empty;

    /// <summary>
    ///     Species tied to this station record.
    /// </summary>
    [ViewVariables]
    public string Species = string.Empty;

    /// <summary>
    ///     Gender identity tied to this station record.
    /// </summary>
    /// <remarks>Sex should be placed in a medical record, not a general record.</remarks>
    [ViewVariables]
    public Gender Gender = Gender.Epicene;

    /// <summary>
    ///     The priority to display this record at.
    ///     This is taken from the 'weight' of a job prototype,
    ///     usually.
    /// </summary>
    [ViewVariables]
    public int DisplayPriority;
}
