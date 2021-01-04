using System.Collections.Generic;
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Requirements
{
    public class IncompatibleObjectivesRequirement : IObjectiveRequirement
    {
        private List<string> _incompatibleObjectives = new();
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x=>x._incompatibleObjectives, "objectives", new List<string>());
        }

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

        public IDeepClone DeepClone()
        {
            return new IncompatibleObjectivesRequirement
            {
                _incompatibleObjectives = _incompatibleObjectives.ShallowClone()
            };
        }
    }
}
