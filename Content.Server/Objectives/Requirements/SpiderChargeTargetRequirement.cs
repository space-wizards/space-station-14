using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Objectives.Interfaces;

namespace Content.Server.Objectives.Requirements;

/// <summary>
/// Requires the player to be a ninja that has a spider charge target assigned, which is almost always the case.
/// </summary>
[DataDefinition]
public sealed partial class SpiderChargeTargetRequirement : IObjectiveRequirement
{
    public bool CanBeAssigned(EntityUid mindId, MindComponent mind)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        entMan.TryGetComponent<NinjaRoleComponent>(mindId, out var role);
        return role?.SpiderChargeTarget != null;
    }
}
