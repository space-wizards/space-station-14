using System.Collections.Generic;
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Objectives.Requirements
{
    [DataDefinition]
    public class IncompatibleObjectivesRequirement : IObjectiveRequirement
    {
        [DataField("objectives")]
        private readonly List<string> _incompatibleObjectives = new();

        public bool CanBeAssigned(Mind mind)
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
