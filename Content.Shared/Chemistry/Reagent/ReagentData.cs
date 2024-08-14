using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class ReagentData : IEquatable<ReagentData>
{
    public abstract bool Equals(ReagentData? other);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        return obj.GetType() == GetType() && Equals((ReagentData) obj);
    }

    public abstract override int GetHashCode();

    public abstract ReagentData Clone();
}
