// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.PersonnelRecords.Components;

/// <summary>
/// Lives on the station entity. Tracks, per job, how many "extra" free slots
/// <c>PersonnelVacancySystem</c> has handed out beyond round-start capacity when a
/// Demotion/Dismissal order executed - and only those slots. This is what lets the system safely
/// reclaim a slot later without ever touching a job's round-start allotment or a free slot that
/// exists for some unrelated reason (an admin command, a different game rule, etc).
///
/// Server-only bookkeeping, not networked - nothing on the client needs to know this.
/// </summary>
[RegisterComponent]
public sealed partial class PersonnelSlotTrackingComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<JobPrototype>, int> ExtraSlotsFreed = new();
}
