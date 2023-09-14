using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Objectives.Interfaces;

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
