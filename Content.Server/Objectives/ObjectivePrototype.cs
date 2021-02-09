using System.Collections.Generic;
using System.Linq;
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Server.Objectives
{
    [Prototype("objective")]
    public class ObjectivePrototype : IPrototype, IIndexedPrototype
    {
        [ViewVariables]
        [YamlField("id")]
        public string ID { get; private set; }

        [ViewVariables] [YamlField("issuer")] public string Issuer { get; private set; } = "Unknown";

        [ViewVariables] [YamlField("prob")] public float Probability { get; private set; } = 0.3f;

        [ViewVariables]
        public float Difficulty => _difficultyOverride ?? _conditions.Sum(c => c.Difficulty);

        [YamlField("conditions")]
        private List<IObjectiveCondition> _conditions = new();
        [YamlField("requirements")]
        private List<IObjectiveRequirement> _requirements = new();

        [ViewVariables]
        public IReadOnlyList<IObjectiveCondition> Conditions => _conditions;

        [ViewVariables]
        [YamlField("canBeDuplicate")]
        public bool CanBeDuplicateAssignment { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [YamlField("difficultyOverride")]
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
