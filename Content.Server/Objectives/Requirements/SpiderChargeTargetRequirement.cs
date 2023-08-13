using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Content.Server.Roles;

namespace Content.Server.Objectives.Requirements;

[DataDefinition]
public sealed class SpiderChargeTargetRequirement : IObjectiveRequirement
{
    public bool CanBeAssigned(Mind.Mind mind)
    {
        foreach (var role in mind.Roles)
        {
            if (role is NinjaRole ninja)
            {
                // if ninja is on dev (no warps) dont tell it to blow up... somewhere?
                // the charge can still be used but its not an obj
                return ninja.SpiderChargeTarget != null;
            }
        }

        return false;
    }
}
