using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Server.Objectives.Interfaces
{
    [Prototype("objective")]
    public class ObjectivePrototype : IPrototype, IIndexedPrototype
    {
        public string ID { get; private set; }

        public string Issuer { get; private set; }

        public float Probability { get; private set; }

        public IReadOnlyList<IObjectiveCondition> Conditions => _conditions;
        public IReadOnlyList<IObjectiveRequirement> Requirements => _requirements;

        public float Difficulty => _difficultyOverride ?? _conditions.Sum(c => c.GetDifficulty());

        private List<IObjectiveCondition> _conditions = new List<IObjectiveCondition>();
        private List<IObjectiveRequirement> _requirements = new List<IObjectiveRequirement>();

        private float? _difficultyOverride = null;

        public bool CanBeAssigned(IEntity entity)
        {
            foreach (var requirement in _requirements)
            {
                if (!requirement.CanBeAssigned(entity)) return false;
            }

            return true;
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var ser = YamlObjectSerializer.NewReader(mapping);

            ser.DataField(this, x => x.ID, "id", string.Empty);
            ser.DataField(this, x => x.Issuer, "issuer", "Other");
            ser.DataField(this, x => x.Probability, "prob", 0.3f);
            ser.DataField(this, x => x._conditions, "conditions", new List<IObjectiveCondition>());
            ser.DataField(this, x => x._requirements, "requirements", new List<IObjectiveRequirement>());
            ser.DataField(this, x => x._difficultyOverride, "difficultyOverride", null);
        }
    }
}
