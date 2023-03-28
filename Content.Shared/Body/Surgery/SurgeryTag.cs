using Robust.Shared.Serialization;
using System;

namespace Content.Shared.Body.Surgery;

[Serializable, NetSerializable]
public readonly struct SurgeryTag : IEquatable<SurgeryTag>
{
    public string ID { get; init; }

    public bool Equals(SurgeryTag other)
    {
        return ID == other.ID;
    }

    public override bool Equals(object? obj)
    {
        return obj is SurgeryTag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return ID.GetHashCode();
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
        return tag.ID;
    }

    public static implicit operator SurgeryTag(string str)
    {
        return new() {ID = str};
    }
}
