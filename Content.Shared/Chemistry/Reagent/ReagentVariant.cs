using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class ReagentVariant : IEquatable<ReagentVariant>
{
    /// <summary>
    /// Convert to a string representation. This if for logging & debugging. This is not localized and should not be
    /// shown to players.
    /// </summary>
    public virtual string ToString(string reagentId, FixedPoint2 quantity)
    {
        return $"{reagentId}:{GetType().Name}:{quantity}";
    }

    /// <summary>
    /// Convert to a string representation. This if for logging & debugging. This is not localized and should not be
    /// shown to players.
    /// </summary>
    public virtual string ToString(string reagentId)
    {
        return $"{reagentId}:{GetType().Name}";
    }

    public abstract bool Equals(ReagentVariant? other);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        return obj.GetType() == GetType() && Equals((ReagentVariant) obj);
    }

    public abstract override int GetHashCode();

    public abstract ReagentVariant Clone();
}
