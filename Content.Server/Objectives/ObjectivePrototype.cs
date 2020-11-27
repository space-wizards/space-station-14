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

        [ViewVariables(VVAccess.ReadWrite)]
        public string Issuer { get; private set; }

        [ViewVariables]
        public float Probability { get; private set; }

        [ViewVariables]
        public IReadOnlyList<IObjectiveCondition> Conditions => _conditions;
        [ViewVariables]
        public IReadOnlyList<IObjectiveRequirement> Requirements => _requirements;

        [ViewVariables]
        public float Difficulty => _difficultyOverride ?? _conditions.Sum(c => c.GetDifficulty());

        private List<IObjectiveCondition> _conditions = new List<IObjectiveCondition>();
        private List<IObjectiveRequirement> _requirements = new List<IObjectiveRequirement>();

        [ViewVariables(VVAccess.ReadWrite)]
        private float? _difficultyOverride = null;

        public bool CanBeAssigned(Mind mind)
        {
            foreach (var requirement in _requirements)
            {
                if (!requirement.CanBeAssigned(mind)) return false;
            }

            return true;
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var ser = YamlObjectSerializer.NewReader(mapping);

            ser.DataField(this, x => x.ID, "id", string.Empty);
            ser.DataField(this, x => x.Issuer, "issuer", "Unknown");
            ser.DataField(this, x => x.Probability, "prob", 0.3f);
            ser.DataField(this, x => x._conditions, "conditions", new List<IObjectiveCondition>());
            ser.DataField(this, x => x._requirements, "requirements", new List<IObjectiveRequirement>());
            ser.DataField(this, x => x._difficultyOverride, "difficultyOverride", null);
        }
    }
}
