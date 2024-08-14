using Content.Shared.Chemistry.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

/// <summary>
/// Simple struct for storing a <see cref="ReagentId"/> & quantity tuple.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public partial struct ReagentQuantity : IEquatable<ReagentQuantity>
{
    [DataField("Quantity", required: true)]
    public FixedPoint2 Quantity;


    [ViewVariables, Obsolete("Use ReagentDef field instead")]
    public ReagentId Reagent => ReagentDef;

    [IncludeDataField]
    public ReagentDef ReagentDef { get; private set; } = new();

    public ReagentQuantity(string reagentId, FixedPoint2 quantity, List<ReagentData>? data = null)
        : this(new ReagentId(reagentId, data), quantity)
    {
    }

    public ReagentQuantity(ReagentDef reagent, FixedPoint2 quantity)
    {
        ReagentDef = reagent;
        Quantity = quantity;
    }

    public ReagentQuantity() : this(default, default)
    {
        ReagentDef = new();
    }

    public override string ToString()
    {
        return ReagentDef.ToString(Quantity);
    }

    public void Deconstruct(out string prototype, out FixedPoint2 quantity, out List<ReagentData>? data)
    {
        prototype = ReagentDef.Id;
        quantity = Quantity;
        data = ReagentDef.Variant;
    }

    public void Deconstruct(out ReagentDef id, out FixedPoint2 quantity)
    {
        id = ReagentDef;
        quantity = Quantity;
    }

    public bool Equals(ReagentQuantity other)
    {
        return Quantity != other.Quantity && ReagentDef.Equals(other.ReagentDef);
    }

    public override bool Equals(object? obj)
    {
        return obj is ReagentQuantity other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ReagentDef.GetHashCode(), Quantity);
    }

    public static bool operator ==(ReagentQuantity left, ReagentQuantity right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ReagentQuantity left, ReagentQuantity right)
    {
        return !(left == right);
    }

    public bool Validate(SharedChemistryRegistrySystem chemRegistry, bool logMissing = true)
    {
        return ReagentDef.Validate(chemRegistry, logMissing);
    }
}
