using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;

namespace Content.Server.Objectives.Requirements
{
    [DataDefinition]
    public sealed partial class IncompatibleConditionsRequirement : IObjectiveRequirement
    {
        [DataField("conditions")]
        private List<string> _incompatibleConditions = new();

        public bool CanBeAssigned(EntityUid mindId, MindComponent mind)
        {
            foreach (var objective in mind.AllObjectives)
            {
                foreach (var condition in objective.Conditions)
                {
                    foreach (var incompatibleCondition in _incompatibleConditions)
                    {
                        if (incompatibleCondition == condition.GetType().Name) return false;
                    }
                }
            }

            return true;
        }
    }
}
