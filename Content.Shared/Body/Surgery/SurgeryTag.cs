using System;

namespace Content.Shared.Body.Surgery
{
    public readonly struct SurgeryTag : IEquatable<SurgeryTag>
    {
        public string Id { get; init; }

        public bool Equals(SurgeryTag other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is SurgeryTag other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(SurgeryTag left, SurgeryTag right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SurgeryTag left, SurgeryTag right)
        {
            return !left.Equals(right);
        }

        public static implicit operator string(SurgeryTag tag)
        {
            return tag.Id;
        }

        public static implicit operator SurgeryTag(string str)
        {
            return new() {Id = str};
        }
    }
}
