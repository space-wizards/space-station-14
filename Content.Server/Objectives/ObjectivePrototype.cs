using System.Linq;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives
{
    /// <summary>
    ///     Prototype for objectives. Remember that to be assigned, it should be added to one or more objective groups in prototype. E.g. crew, traitor, wizard
    /// </summary>
    [Prototype("objective")]
    public sealed class ObjectivePrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        [DataField("issuer")] public string Issuer { get; private set; } = "Unknown";

        [ViewVariables]
        public float Difficulty => _difficultyOverride ?? _conditions.Sum(c => c.Difficulty);

        [DataField("conditions")]
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

        public bool CanBeAssigned(Mind.Mind mind)
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

        public Objective GetObjective(Mind.Mind mind)
        {
            return new(this, mind);
        }
    }
}
