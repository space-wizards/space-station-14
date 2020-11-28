using System.Collections.Generic;
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.ViewVariables;

namespace Content.Server.Objectives
{
    public class Objective
    {
        [ViewVariables]
        public readonly Mind Mind;
        [ViewVariables]
        public readonly ObjectivePrototype Prototype;
        private List<IObjectiveCondition> _conditions = new List<IObjectiveCondition>();
        [ViewVariables]
        public IReadOnlyList<IObjectiveCondition> Conditions => _conditions;

        public Objective(ObjectivePrototype prototype, Mind mind)
        {
            Prototype = prototype;
            Mind = mind;
            foreach (var condition in prototype.Conditions)
            {
                _conditions.Add(condition.GetAssigned(mind));
            }
        }
    }
}
