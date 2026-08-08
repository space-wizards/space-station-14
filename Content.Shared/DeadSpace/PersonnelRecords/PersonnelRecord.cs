// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.PersonnelRecords;

/// <summary>
/// Personnel (HR) record for a crewmember. Sits alongside <c>GeneralStationRecord</c> and
/// <c>CriminalRecord</c> under the same <c>StationRecordKey</c>.
/// Can be viewed and edited in a Personnel Records console by department heads, the HoP and the captain.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed partial record PersonnelRecord
{
    /// <summary>
    /// Current disciplinary status of the person.
    /// </summary>
    [DataField]
    public EmploymentStatus Status = EmploymentStatus.None;

    /// <summary>
    /// Reason for the current status. Required for every action, including cancellation.
    /// </summary>
    [DataField]
    public string? Reason;

    /// <summary>
    /// The name of the person (by ID) who issued the current status.
    /// </summary>
    [DataField]
    public string? InitiatorName;

    /// <summary>
    /// Full history of personnel actions taken against this person.
    /// </summary>
    [DataField]
    public List<PersonnelHistory> History = new();

    /// <summary>
    /// The job the person held at the moment a Demotion or Dismissal order was issued.
    /// Used by <c>PersonnelOrderCompletionSystem</c> to detect execution: once the person's
    /// actual job prototype no longer matches this value, the order is considered carried out.
    /// Cleared back to null once the order is executed or annulled.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? JobAtOrder;

    /// <summary>
    /// The displayed job title at the moment a Demotion or Dismissal order was issued. The general
    /// record already contains the new title by the time execution is detected, so this snapshot is
    /// required to preserve the actual previous title in history.
    /// </summary>
    [DataField]
    public string? JobTitleAtOrder;

    /// <summary>
    /// <see cref="Status"/> as it was immediately before a Demotion or Dismissal order was
    /// issued (either <see cref="EmploymentStatus.None"/> or <see cref="EmploymentStatus.Reprimand"/>).
    /// Cancelling the order restores this value rather than always falling back to None, so an
    /// earlier reprimand isn't silently erased by cancelling a later escalation. Not part of the
    /// original field list in the design doc: append-only <see cref="History"/> alone can't
    /// disambiguate this after an execute-then-reissue cycle, so this snapshot follows the same
    /// pattern as <see cref="JobAtOrder"/>. Cleared back to null once the order is executed or annulled.
    /// </summary>
    [DataField]
    public EmploymentStatus? StatusBeforeOrder;

    /// <summary>
    /// The job title the person held right before their most recently executed order.
    /// Used only for record-keeping / printing, not for logic.
    /// </summary>
    [DataField]
    public string? PreviousJobTitle;

    /// <summary>
    /// Jobs for which this record has already freed a vacancy slot this round.
    /// Second line of defense against slot-count abuse: a given job can only free a slot
    /// for this record once per round, regardless of how the occupied/free/round-start
    /// invariant is computed.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> SlotsFreedFor = new();
}

/// <summary>
/// A single line of personnel action history, and the time it was added at.
/// </summary>
[Serializable, NetSerializable]
public record struct PersonnelHistory(TimeSpan AddTime, PersonnelActionType Type, string Text, string? InitiatorName);
