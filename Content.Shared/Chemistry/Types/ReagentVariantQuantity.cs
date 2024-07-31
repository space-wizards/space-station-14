using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
namespace Content.Shared.Chemistry.Types;

[DataRecord, Serializable, NetSerializable]
public partial struct ReagentVariantQuantity  : IEquatable<ReagentVariantQuantity>
{

    [DataField(required:true)]
    public string ReagentId  = string.Empty;

    [DataField(required: true)]
    public ReagentVariant? Variant;

    [DataField(required:true)]
    public FixedPoint2 Quantity;

    public bool IsValid(SharedChemistryRegistrySystem chemRegistry)
    {
        return Variant != null && chemRegistry.TryIndex(ReagentId, out var regDef) &&
               regDef.Value.Comp.Id == ReagentId;
    }

    public ReagentVariantQuantity()
    {
    }

    public ReagentVariantQuantity(string reagentId, ReagentVariant variant, FixedPoint2 quantity)
    {
        ReagentId = reagentId;
        Variant = variant;
        Quantity = quantity;
    }

    public ReagentVariantQuantity(Entity<ReagentDefinitionComponent> reagentDef, ReagentVariant variant, FixedPoint2 quantity)
    {
        ReagentId = reagentDef.Comp.Id;
        Variant = variant;
        Quantity = quantity;
    }


    public bool Equals(ReagentVariantQuantity other)
    {
        return other.GetHashCode() == GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;
        return obj.GetHashCode() == GetHashCode();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ReagentId.GetHashCode(), Variant?.GetHashCode());
    }
}
