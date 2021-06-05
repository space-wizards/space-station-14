#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Objectives
{
    [Prototype("objective")]
    public class ObjectivePrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables] [DataField("issuer")] public string Issuer { get; private set; } = "Unknown";

        [ViewVariables] [DataField("prob")] public float Probability { get; private set; } = 0.3f;

        [ViewVariables]
        public float Difficulty => _difficultyOverride ?? _conditions.Sum(c => c.Difficulty);

        [DataField("conditions")]
        private List<IObjectiveCondition> _conditions = new();
        [DataField("requirements")]
        private List<IObjectiveRequirement> _requirements = new();

        [ViewVariables]
        public IReadOnlyList<IObjectiveCondition> Conditions => _conditions;

        [ViewVariables]
        [DataField("canBeDuplicate")]
        public bool CanBeDuplicateAssignment { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("difficultyOverride")]
        private float? _difficultyOverride = null;

        public bool CanBeAssigned(Mind mind)
        {
            foreach (var requirement in _requirements)
            {
                if (!requirement.CanBeAssigned(mind)) return false;
            }

            if (!CanBeDuplicateAssignment)
            {
                foreach (var objective in mind.AllObjectives)
                {
                    if (objective.Prototype.ID == ID) return false;
                }
            }

            return true;
        }

        public Objective GetObjective(Mind mind)
        {
            return new(this, mind);
        }
    }
}
