using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles checking the job blacklist for this objective.
/// </summary>
public sealed class NotJobRequirementSystem : EntitySystem
{
    [Dependency] private readonly SharedJobSystem _jobs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NotJobRequirementComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(EntityUid uid, NotJobRequirementComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        _jobs.MindTryGetJob(args.MindId, out var proto);

        // if player has no job then don't care
        if (proto is not null && proto.ID == comp.Job)
            args.Cancelled = true;
    }
}
