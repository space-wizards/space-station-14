// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Access.Systems;

/// <summary>
/// DS14: Raised by <c>IdCardConsoleSystem</c> right before it would actually write a new job onto a
/// target's ID card and station record - before the job icon/department/
/// <c>GeneralStationRecord.JobPrototype</c>, name, title or access changes from the same request.
/// Any system may cancel it to reject the entire write atomically.
///
/// Used by <c>PersonnelVacancySystem</c> to stop someone being reassigned into a job whose
/// round-start slot cap it already had to compensate for once (a Demotion/Dismissal order freed a
/// slot, a new hire already filled it normally, and now the original person - or anyone else - is
/// about to be handed the same job again through the ID card console, which doesn't touch the slot
/// pool at all). Deliberately its own event rather than a direct call, so it stays general-purpose
/// and doesn't require IdCardConsoleSystem to know Personnel Records exists.
/// </summary>
public sealed class IdCardJobAssignmentAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Actor;
    public readonly EntityUid TargetId;
    public readonly ProtoId<JobPrototype> NewJob;

    public IdCardJobAssignmentAttemptEvent(EntityUid actor, EntityUid targetId, ProtoId<JobPrototype> newJob)
    {
        Actor = actor;
        TargetId = targetId;
        NewJob = newJob;
    }
}

/// <summary>
/// Raised after an ID console has successfully changed the target to a different job. Unlike a
/// generic station-record update, this unambiguously represents a real assignment and can safely
/// be used to consume a vacancy that Personnel Records previously freed.
/// </summary>
public sealed class IdCardJobAssignedEvent : EntityEventArgs
{
    public readonly EntityUid Actor;
    public readonly EntityUid TargetId;
    public readonly ProtoId<JobPrototype> NewJob;

    public IdCardJobAssignedEvent(EntityUid actor, EntityUid targetId, ProtoId<JobPrototype> newJob)
    {
        Actor = actor;
        TargetId = targetId;
        NewJob = newJob;
    }
}
