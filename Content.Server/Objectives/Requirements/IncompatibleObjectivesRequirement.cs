using Content.Server.Objectives.Interfaces;

namespace Content.Server.Objectives.Requirements
{
    [DataDefinition]
    public sealed class IncompatibleObjectivesRequirement : IObjectiveRequirement
    {
        [DataField("objectives")]
        private readonly List<string> _incompatibleObjectives = new();

        public bool CanBeAssigned(Mind.Mind mind)
        {
            foreach (var objective in mind.AllObjectives)
            {
                foreach (var incompatibleObjective in _incompatibleObjectives)
                {
                    if (incompatibleObjective == objective.Prototype.ID) return false;
                }
            }

            return true;
        }
    }
}
