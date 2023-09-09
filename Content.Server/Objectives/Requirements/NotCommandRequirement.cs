using Content.Shared.Mind;
using Content.Shared.Objectives.Interfaces;
using Content.Shared.Roles.Jobs;

namespace Content.Server.Objectives.Requirements;

// TODO make component and system
[DataDefinition]
public sealed partial class NotCommandRequirement : IObjectiveRequirement
{
    public bool CanBeAssigned(EntityUid mindId, MindComponent mind)
    {
        var entities = IoCManager.Resolve<IEntityManager>();
        var jobs = entities.System<SharedJobSystem>();
        return jobs.MindTryGetJob(mindId, out _, out var prototype) && prototype.RequireAdminNotify;
    }
}
