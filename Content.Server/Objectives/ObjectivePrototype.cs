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
        public string ID { get; private set; }

        [ViewVariables]
        public string Issuer { get; private set; }

        [ViewVariables]
        public float Probability { get; private set; }

        [ViewVariables]
        public float Difficulty => _difficultyOverride ?? _conditions.Sum(c => c.Difficulty);

        private List<IObjectiveCondition> _conditions = new();
        private List<IObjectiveRequirement> _requirements = new();

        [ViewVariables]
        public IReadOnlyList<IObjectiveCondition> Conditions => _conditions;

        [ViewVariables]
        public bool CanBeDuplicateAssignment { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
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

        public void LoadFrom(YamlMappingNode mapping)
        {
            var ser = YamlObjectSerializer.NewReader(mapping);

            ser.DataField(this, x => x.ID, "id", string.Empty);
            ser.DataField(this, x => x.Issuer, "issuer", "Unknown");
            ser.DataField(this, x => x.Probability, "prob", 0.3f);
            ser.DataField(ref _conditions, "conditions", new List<IObjectiveCondition>());
            ser.DataField(ref _requirements, "requirements", new List<IObjectiveRequirement>());
            ser.DataField(ref _difficultyOverride, "difficultyOverride", null);
            ser.DataField(this, x => x.CanBeDuplicateAssignment, "canBeDuplicate", false);
        }

        public Objective GetObjective(Mind mind)
        {
            return new(this, mind);
        }
    }
}
