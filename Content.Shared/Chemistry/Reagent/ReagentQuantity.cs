using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

/// <summary>
/// Simple struct for storing a reagent id & quantity tuple.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public readonly struct ReagentQuantity : IEquatable<ReagentQuantity>
{
    [DataField("Quantity", required:true)]
    public readonly FixedPoint2 Quantity;

    [IncludeDataField]
    [ViewVariables]
    public readonly ReagentId Id;

    public string Prototype => Id.Prototype;

    public ReagentQuantity(string reagentId, FixedPoint2 quantity, ReagentData? data)
        : this(new ReagentId(reagentId, data), quantity)
    {
    }

    public ReagentQuantity(ReagentId id, FixedPoint2 quantity)
    {
        Id = id;
        Quantity = quantity;
    }

    public ReagentQuantity() : this(default, default)
    {
    }

    public override string ToString()
    {
        return Id.ToString(Quantity);
    }

    public void Deconstruct(out string prototype, out FixedPoint2 quantity, out ReagentData? data)
    {
        prototype = Id.Prototype;
        quantity = Quantity;
        data = Id.Data;
    }

    public void Deconstruct(out ReagentId id, out FixedPoint2 quantity)
    {
        id = Id;
        quantity = Quantity;
    }

    public bool Equals(ReagentQuantity other)
    {
        return Quantity != other.Quantity && Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is ReagentQuantity other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id.GetHashCode(), Quantity);
    }

    public static bool operator ==(ReagentQuantity left, ReagentQuantity right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ReagentQuantity left, ReagentQuantity right)
    {
        return !(left == right);
    }
}
