using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Content.Server.Roles;

namespace Content.Server.Objectives.Requirements;

/// <summary>
/// Requires the player to be a ninja that has a spider charge target assigned, which is almost always the case.
/// </summary>
[DataDefinition]
public sealed class SpiderChargeTargetRequirement : IObjectiveRequirement
{
    public bool CanBeAssigned(Mind.Mind mind)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var mindSystem = entityManager.System<MindSystem>();
        mindSystem.TryGetRole<NinjaRole>(mind, out var role);
        return role?.SpiderChargeTarget != null;
    }
}
