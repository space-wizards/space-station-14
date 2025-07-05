using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Requires that at least one of a list of jobs have been taken on the station.
/// </summary>
public sealed class JobExistsRequirementSystem : EntitySystem
{
    [Dependency] private readonly SharedJobSystem _jobs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JobExistsRequirementComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(EntityUid uid, JobExistsRequirementComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        var query = EntityQueryEnumerator<MindComponent>();

        while (query.MoveNext(out var mindId, out var traitor))
        {
            if (_jobs.MindTryGetJob(mindId, out var prototype) && comp.Jobs.Contains(prototype))
                return;
        }

        args.Cancelled = true;
    }
}
