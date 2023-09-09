using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Interfaces;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Objectives
{
    /// <summary>
    ///     Prototype for objectives. Remember that to be assigned, it should be added to one or more objective groups in prototype. E.g. crew, traitor, wizard
    /// </summary>
    [Prototype("objective")]
    public sealed class ObjectivePrototype : IPrototype
    {
        [IdDataField, ViewVariables]
        public string ID { get; private set; } = default!;

        [DataField("issuer")]
        public string Issuer = "Unknown";

        [ViewVariables]
        public float Difficulty => Conditions.Sum(c => GetDifficulty(c));

        [DataField("conditions", serverOnly: true, customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public List<string> Conditions = new();

        [DataField("requirements")]
        private List<IObjectiveRequirement> _requirements = new();

        [DataField("canBeDuplicate")]
        public bool CanBeDuplicateAssignment;

        public bool CanBeAssigned(EntityUid mindId, MindComponent mind)
        {
            foreach (var requirement in _requirements)
            {
                if (!requirement.CanBeAssigned(mindId, mind))
                    return false;
            }

            if (!CanBeDuplicateAssignment)
            {
                foreach (var objective in mind.AllObjectives)
                {
                    if (objective.Prototype.ID == ID)
                        return false;
                }
            }

            return true;
        }

        private float GetDifficulty(string id)
        {
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            if (!protoMan.TryIndex<EntityPrototype>(id, out var proto))
                return 0f;

            if (!proto.TryGetComponent<ObjectiveConditionComponent>(out var cond))
                return 0f;

            return cond.Difficulty;
        }
    }
}
