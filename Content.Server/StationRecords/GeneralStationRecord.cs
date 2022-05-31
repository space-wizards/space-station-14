using Robust.Shared.Enums;

namespace Content.Server.StationRecords;

/// <summary>
///     General station record. Indicates the crewmember's name and job.
/// </summary>
public sealed class GeneralStationRecord
{
    /// <summary>
    ///     Name tied to this station record.
    /// </summary>
    [ViewVariables]
    public string Name = string.Empty;

    /// <summary>
    ///     Job title tied to this station record.
    /// </summary>
    [ViewVariables]
    public string JobTitle = string.Empty;

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
    [ViewVariables]
    public List<string> Departments = new();

    /// <summary>
    ///     The priority to display this record at.
    ///     Higher is better, i.e., 4 will display over 3.
    ///     Assigned at round start/player spawn.
    /// </summary>
    [ViewVariables]
    public uint DisplayPriority;
}
