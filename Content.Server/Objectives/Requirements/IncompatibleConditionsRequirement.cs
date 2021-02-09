using System.Collections.Generic;
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.Objectives.Requirements
{
    public class IncompatibleConditionsRequirement : IObjectiveRequirement
    {
        private List<string> _incompatibleConditions = new();

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x=>x._incompatibleConditions, "conditions", new List<string>());
        }

        public bool CanBeAssigned(Mind mind)
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
