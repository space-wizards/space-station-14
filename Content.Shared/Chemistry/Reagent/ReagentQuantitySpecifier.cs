using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;


[DataDefinition, Serializable, NetSerializable]
public partial struct ReagentQuantitySpecifier : IEquatable<ReagentQuantitySpecifier>
{
    public static readonly ReagentQuantitySpecifier Invalid = new();

    [DataField]
    public ReagentSpecifier Reagent;

    [DataField]
    public FixedPoint2 Quantity;

    [DataField]
    public bool IsValid => Reagent.IsValid;

    public ReagentQuantitySpecifier()
    {
        Reagent = ReagentSpecifier.Invalid;
        Quantity = 0;
    }

    public ReagentQuantitySpecifier(ReagentQuantity reagentQuantity)
    {
        Reagent = reagentQuantity.ReagentDef;
        Quantity = reagentQuantity.Quantity;
    }

    public ReagentQuantitySpecifier(ReagentSpecifier reagent, FixedPoint2 quantity)
    {
        Reagent = reagent;
        Quantity = quantity;
    }

    public ReagentQuantitySpecifier(Entity<ReagentDefinitionComponent> definitionEntity, FixedPoint2 quantity, ReagentVariant? variant = null)
        : this(new ReagentSpecifier(definitionEntity, variant), quantity)
    {
    }

    public override string ToString()
    {
        return Reagent.ToString(Quantity);
    }

    public void Deconstruct(out string reagentId, out FixedPoint2 quantity, out ReagentVariant? data)
    {
        reagentId = Reagent.Id;
        quantity = Quantity;
        data = Reagent.Variant;
    }

    public void Deconstruct(out ReagentSpecifier id, out FixedPoint2 quantity)
    {
        id = Reagent;
        quantity = Quantity;
    }

    public bool Equals(ReagentQuantitySpecifier other)
    {
        return Quantity != other.Quantity && Reagent.Equals(other.Reagent);
    }

    public override bool Equals(object? obj)
    {
        return obj is ReagentQuantitySpecifier other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Reagent.GetHashCode(), Quantity);
    }

    public static bool operator ==(ReagentQuantitySpecifier left, ReagentQuantitySpecifier right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ReagentQuantitySpecifier left, ReagentQuantitySpecifier right)
    {
        return !(left == right);
    }

    public static implicit operator ReagentQuantitySpecifier(KeyValuePair<ReagentSpecifier, FixedPoint2> data) =>
        new(data.Key, data.Value);
    public static implicit operator ReagentQuantitySpecifier(ReagentQuantity q) => new(q);
    public static implicit operator string(ReagentQuantitySpecifier d) => d.Reagent.Id;
    public static implicit operator (string, ReagentVariant?, FixedPoint2)(ReagentQuantitySpecifier d) => (d.Reagent.Id, d.Reagent.Variant, d.Quantity);
    public static implicit operator (ReagentSpecifier, FixedPoint2)(ReagentQuantitySpecifier q) => (q.Reagent,q.Quantity);
    public static implicit operator ReagentQuantitySpecifier((ReagentSpecifier, FixedPoint2)d) => new(d.Item1, d.Item2);

    public static void UpdateCachedEntity(ref ReagentQuantitySpecifier reagentQuantity, SharedChemistryRegistrySystem chemRegistry)
    {
        ReagentSpecifier.UpdateCachedEntity(ref reagentQuantity.Reagent, chemRegistry);
    }

    public static bool ResolveReagentEntity(ref ReagentQuantitySpecifier quantSpec,
        SharedChemistryRegistrySystem chemRegistry,
        bool logIfMissing = true)
    {
        return ReagentSpecifier.ResolveReagentEntity(ref quantSpec.Reagent, chemRegistry, logIfMissing);
    }

    public static bool TryGetReagentQuantity(ReagentQuantitySpecifier quantSpec,
        SharedChemistryRegistrySystem chemRegistry,
        out ReagentQuantity reagentQuantity,
        bool logIfMissing = true)
    {
        if (!ReagentSpecifier.ResolveReagentEntity(ref quantSpec.Reagent, chemRegistry, logIfMissing))
        {
            reagentQuantity = default;
            return false;
        }
        //This is not null because it gets resolved.
        reagentQuantity = (new(quantSpec.Reagent.CachedDefinitionEntity!.Value, quantSpec.Reagent.Variant), quantSpec.Quantity);
        return true;
    }
}
