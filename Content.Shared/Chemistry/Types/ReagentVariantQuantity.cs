using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
namespace Content.Shared.Chemistry.Types;

[DataRecord, Serializable, NetSerializable]
public partial struct ReagentVariantQuantity  : IEquatable<ReagentVariantQuantity> //This is a struct so that we don't allocate. Allocations are paying taxes. Nobody wants to pay taxes.
{

    [DataField(required:true)]
    public string ReagentId  = string.Empty;

    [NonSerialized]
    public Entity<ReagentDefinitionComponent>? ReagentDef;

    [DataField(required: true)]
    public ReagentVariant? Variant;

    [DataField(required:true)]
    public FixedPoint2 Quantity;

    //Cached index of the reagent type if this is in a solution
    [ViewVariables]
    public int CachedReagentIndex = -1;

    public ReagentVariantQuantity()
    {
    }

    public ReagentVariantQuantity(string reagentId, ReagentVariant variant, FixedPoint2 quantity)
    {
        ReagentDef = null;
        ReagentId = reagentId;
        Variant = variant;
        Quantity = quantity;
    }

    public ReagentVariantQuantity(Entity<ReagentDefinitionComponent> reagentDef, ReagentVariant variant, FixedPoint2 quantity)
    {
        ReagentDef = reagentDef;
        ReagentId = reagentDef.Comp.Id;
        Variant = variant;
        Quantity = quantity;
    }

    public ReagentVariantQuantity(
        Entity<ReagentDefinitionComponent> reagentDef,
        ReagentVariant variant,
        int reagentIndex ,
        FixedPoint2 quantity)
    {
        ReagentDef = reagentDef;
        ReagentId = reagentDef.Comp.Id;
        Variant = variant;
        CachedReagentIndex = reagentIndex;
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

    public override string ToString()
    {
        return $"{ReagentId}:{Quantity}";
    }

    public void UpdateDef(SharedChemistryRegistrySystem chemRegistry)
    {
        if (ReagentDef != null && ReagentId == ReagentDef.Value.Comp.Id)
            return;

        if (chemRegistry.TryIndex(ReagentId, out ReagentDef))
            return;
        chemRegistry.Log.Error($"Reagent with ID:{ReagentId} could not be found in the chemical registry!");
    }

}
