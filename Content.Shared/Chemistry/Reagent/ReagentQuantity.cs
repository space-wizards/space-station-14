using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Reagent;

/// <summary>
/// Simple struct for storing a <see cref="ReagentId"/> & quantity tuple.
/// </summary>
public partial struct ReagentQuantity: IEquatable<ReagentQuantity>
{
    public static readonly ReagentQuantity Invalid = new();

    public ReagentDef ReagentDef;

    public FixedPoint2 Quantity;
    public bool IsValid => ReagentDef.IsValid;

    public Entity<ReagentDefinitionComponent> DefinitionEntity => ReagentDef.DefinitionEntity;

    public ReagentVariant? Variant => ReagentDef.Variant;

    public string Id => ReagentDef.Id;

    public ReagentQuantity()
    {
        ReagentDef = ReagentDef.Invalid;
        Quantity = 0;
    }

    public ReagentQuantity(ReagentDef reagent, FixedPoint2 quantity)
    {
        ReagentDef = reagent;
        Quantity = quantity;
    }

    public ReagentQuantity(Entity<ReagentDefinitionComponent> definitionEntity, FixedPoint2 quantity,
        ReagentVariant? variant = null)
        : this(new ReagentDef(definitionEntity, variant), quantity)
    {
    }


    public override string ToString()
    {
        return ReagentDef.ToString(Quantity);
    }

    public void Deconstruct(out string reagentId, out FixedPoint2 quantity, out ReagentVariant? data)
    {
        reagentId = ReagentDef.Id;
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
        return  Quantity != other.Quantity
               && ReagentDef.Equals(other.ReagentDef);
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

    public static implicit operator (ReagentDef, FixedPoint2)(ReagentQuantity q) => (q.ReagentDef,q.Quantity);
    public static implicit operator ReagentQuantity((ReagentDef, FixedPoint2)d) => new(d.Item1, d.Item2);
    public static implicit operator ReagentDef(ReagentQuantity q) => q.ReagentDef;
    public static implicit operator FixedPoint2(ReagentQuantity q) => q.Quantity;
    public static implicit operator Entity<ReagentDefinitionComponent>(ReagentQuantity q) => q.ReagentDef.DefinitionEntity;
    public static implicit operator ReagentVariant?(ReagentQuantity q) => q.ReagentDef.Variant;
    public static implicit operator string(ReagentQuantity q) => q.ReagentDef.Id;
}
