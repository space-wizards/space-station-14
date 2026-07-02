using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

/// <summary>
/// Simple struct for storing a <see cref="ReagentId"/> & quantity tuple.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public partial struct ReagentQuantity : IEquatable<ReagentQuantity>, IRobustCloneable<ReagentQuantity>
{
    [DataField("Quantity", required: true)]
    public FixedPoint2 Quantity { get; private set; }

    [IncludeDataField]
    [ViewVariables]
    public ReagentId Reagent { get; private set; }

    public ReagentQuantity(string reagentId, FixedPoint2 quantity, List<ReagentData>? data = null)
        : this(new ReagentId(reagentId, data), quantity)
    {
    }

    public ReagentQuantity(ReagentId reagent, FixedPoint2 quantity)
    {
        Reagent = reagent;
        Quantity = quantity;
    }

    public ReagentQuantity(ReagentQuantity reagentQuantity)
    {
        Quantity = reagentQuantity.Quantity;
        if (reagentQuantity.Reagent.Data is not { } data)
        {
            Reagent = new ReagentId(reagentQuantity.Reagent.Prototype, null);
            return;
        }

        List<ReagentData> copy = new(data.Count);
        foreach (var item in data)
        {
            copy.Add(item.Clone());
        }
        Reagent = new ReagentId(reagentQuantity.Reagent.Prototype, copy);
    }

    public readonly ReagentQuantity Clone()
    {
        return new ReagentQuantity(this);
    }

    public ReagentQuantity() : this(default, default)
    {
    }

    public override string ToString()
    {
        return Reagent.ToString(Quantity);
    }

    public void Deconstruct(out string prototype, out FixedPoint2 quantity, out List<ReagentData>? data)
    {
        prototype = Reagent.Prototype;
        quantity = Quantity;
        data = Reagent.Data;
    }

    public void Deconstruct(out ReagentId id, out FixedPoint2 quantity)
    {
        id = Reagent;
        quantity = Quantity;
    }

    public bool Equals(ReagentQuantity other)
    {
        return Quantity == other.Quantity && Reagent.Equals(other.Reagent);
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
