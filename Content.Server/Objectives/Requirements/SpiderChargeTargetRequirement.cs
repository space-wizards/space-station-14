using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Content.Server.Roles;
using System.Linq;

namespace Content.Server.Objectives.Requirements;

[DataDefinition]
public sealed class SpiderChargeTargetRequirement : IObjectiveRequirement
{
    public bool CanBeAssigned(Mind.Mind mind)
    {
        var role = mind.Roles.Where(role => role is NinjaRole).FirstOrDefault();
        if (role == null)
            return false;

        // if ninja is on dev (no warps) dont tell it to blow up... somewhere?
        // the charge can still be used but its not an obj
        return ((NinjaRole) role).SpiderChargeTarget != null;
    }
}
