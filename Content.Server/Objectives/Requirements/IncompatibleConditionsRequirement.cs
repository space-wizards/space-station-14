using Content.Server.Objectives.Interfaces;

namespace Content.Server.Objectives.Requirements
{
    [DataDefinition]
    public sealed class IncompatibleConditionsRequirement : IObjectiveRequirement
    {
        [DataField("conditions")]
        private readonly List<string> _incompatibleConditions = new();

        public bool CanBeAssigned(Mind.Mind mind)
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
