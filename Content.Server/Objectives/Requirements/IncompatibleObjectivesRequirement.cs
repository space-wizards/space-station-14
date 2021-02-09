using System.Collections.Generic;
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.Objectives.Requirements
{
    public class IncompatibleObjectivesRequirement : IObjectiveRequirement
    {
        private List<string> _incompatibleObjectives = new();

        void IExposeData.ExposeData(ObjectSerializer serializer)
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
    }
}
