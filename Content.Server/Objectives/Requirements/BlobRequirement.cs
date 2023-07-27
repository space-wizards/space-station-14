using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Content.Server.Roles;

namespace Content.Server.Objectives.Requirements;

[DataDefinition]
public sealed class BlobRequirement : IObjectiveRequirement
{
    public bool CanBeAssigned(Mind.Mind mind)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var mindSystem = entityManager.System<MindSystem>();
        return mindSystem.HasRole<BlobRole>(mind);
    }
}
