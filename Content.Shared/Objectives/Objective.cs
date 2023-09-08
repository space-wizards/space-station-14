using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Shared.Objectives
{
    public sealed class Objective : IEquatable<Objective>
    {
        [ViewVariables]
        public readonly EntityUid MindId;
        [ViewVariables]
        public readonly ObjectivePrototype Prototype;
        [ViewVariables]
        public readonly List<EntityUid> Conditions = new();

        public Objective(EntityUid mindId, ObjectivePrototype prototype, List<EntityUid> conditions)
        {
            MindId = mindId;
            Prototype = prototype;
            Conditions = conditions;
        }

        public bool Equals(Objective? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(MindId, other.MindId) || !Equals(Prototype, other.Prototype)) return false;
            if (_conditions.Count != other._conditions.Count) return false;

            for (var i = 0; i < _conditions.Count; i++)
            {
                // just comparing condition entity ids here
                // ideally compare the entities...
                if (_conditions[i] != other._conditions[i]) return false;
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
