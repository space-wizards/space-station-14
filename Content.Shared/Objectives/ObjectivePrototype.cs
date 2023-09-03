using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Objectives.Interfaces;
using Robust.Shared.Prototypes;

namespace Content.Shared.Objectives
{
    /// <summary>
    ///     Prototype for objectives. Remember that to be assigned, it should be added to one or more objective groups in prototype. E.g. crew, traitor, wizard
    /// </summary>
    [Prototype("objective")]
    public sealed class ObjectivePrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("issuer")] public string Issuer { get; private set; } = "Unknown";

        [ViewVariables]
        public float Difficulty => _difficultyOverride ?? _conditions.Sum(c => c.Difficulty);

        [DataField("conditions", serverOnly: true)]
        private List<IObjectiveCondition> _conditions = new();
        [DataField("requirements")]
        private List<IObjectiveRequirement> _requirements = new();

        [ViewVariables]
        public IReadOnlyList<IObjectiveCondition> Conditions => _conditions;

        [DataField("canBeDuplicate")]
        public bool CanBeDuplicateAssignment { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("difficultyOverride")]
        private float? _difficultyOverride = null;

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

        public Objective GetObjective(EntityUid mindId, MindComponent mind)
        {
            return new Objective(this, mindId, mind);
        }
    }
}
