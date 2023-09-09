using Content.Shared.Mind;
using Content.Shared.Objectives.Interfaces;
using Content.Shared.Whitelist;

namespace Content.Server.Objectives.Requirements
{
    // TODO: event handled by system
    [DataDefinition]
    public sealed partial class IncompatibleConditionsRequirement : IObjectiveRequirement
    {
        /// <summary>
        /// Blacklist for condition components that are not allowed to exist on any objective condition.
        /// </summary>
        [DataField("conditions"), ViewVariables(VVAccess.ReadWrite)]
        public EntityWhitelist Conditions = new();

        public bool CanBeAssigned(EntityUid mindId, MindComponent mind)
        {
            foreach (var objective in mind.AllObjectives)
            {
                foreach (var condition in objective.Conditions)
                {
                    if (Conditions.IsValid(condition))
                        return false;
                }
            }

            return true;
        }
    }
}
