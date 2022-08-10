using Content.Server.Objectives.Interfaces;

namespace Content.Server.Objectives
{
    public sealed class Objective : IEquatable<Objective>
    {
        [ViewVariables]
        public readonly Mind.Mind Mind;
        [ViewVariables]
        public readonly ObjectivePrototype Prototype;
        private readonly List<IObjectiveCondition> _conditions = new();
        [ViewVariables]
        public IReadOnlyList<IObjectiveCondition> Conditions => _conditions;

        public Objective(ObjectivePrototype prototype, Mind.Mind mind)
        {
            Prototype = prototype;
            Mind = mind;
            foreach (var condition in prototype.Conditions)
            {
                _conditions.Add(condition.GetAssigned(mind));
            }
        }

        public bool Equals(Objective? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(Mind, other.Mind) || !Equals(Prototype, other.Prototype)) return false;
            if (_conditions.Count != other._conditions.Count) return false;
            for (var i = 0; i < _conditions.Count; i++)
            {
                if (!_conditions[i].Equals(other._conditions[i])) return false;
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Objective) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Mind, Prototype, _conditions);
        }
    }
}
