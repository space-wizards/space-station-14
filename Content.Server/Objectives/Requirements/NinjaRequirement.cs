using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Content.Server.Roles;

namespace Content.Server.Objectives.Requirements;

/// <summary>
/// Requires the player's mind to have the ninja role component, aka be a ninja.
/// </summary>
[DataDefinition]
public sealed partial class NinjaRequirement : IObjectiveRequirement
{
    public bool CanBeAssigned(EntityUid mindId, MindComponent mind)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        return entMan.HasComponent<NinjaRoleComponent>(mindId);
    }
}
