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
    public Gender Gender = Gender.Neuter;

    /// <summary>
    ///     The departments that this person is in.
    ///     Useful for displaying this person in specific
    ///     departments. This can be empty, and it
    ///     should be handled properly. Should be assigned
    ///     at round start/player spawn when the job prototype
    ///     ID is accessed.
    /// </summary>
    /// <remarks>
    ///     This probably shouldn't be user-settable, otherwise you
    ///     run the risk of users having the ability to duplicate
    ///     themselves across departments. Have any custom jobs just
    ///     be renames of an existing job so that department information
    ///     is preserved.
    /// </remarks>
    [ViewVariables]
    public List<string> Departments = new();

    /// <summary>
    ///     The priority to display this record at.
    ///     This is taken from the 'weight' of a job prototype,
    ///     usually.
    /// </summary>
    [ViewVariables]
    public int DisplayPriority;
}
