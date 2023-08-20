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
    public readonly Reagent Reagent;

    public ReagentQuantity(string reagentId, FixedPoint2 quantity, ReagentData? data)
        : this(new Reagent(reagentId, data), quantity)
    {
    }

    public ReagentQuantity(Reagent reagent, FixedPoint2 quantity)
    {
        Reagent = reagent;
        Quantity = quantity;
    }

    public ReagentQuantity() : this(default, default)
    {
    }

    public override string ToString()
    {
        return Reagent.ToString(Quantity);
    }

    public void Deconstruct(out string prototype, out FixedPoint2 quantity, out ReagentData? data)
    {
        prototype = Reagent.Prototype;
        quantity = Quantity;
        data = Reagent.Data;
    }

    public void Deconstruct(out Reagent id, out FixedPoint2 quantity)
    {
        id = Reagent;
        quantity = Quantity;
    }

    public bool Equals(ReagentQuantity other)
    {
        return Quantity != other.Quantity && Reagent.Equals(other.Reagent);
    }

    public override bool Equals(object? obj)
    {
        return obj is ReagentQuantity other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Reagent.GetHashCode(), Quantity);
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
