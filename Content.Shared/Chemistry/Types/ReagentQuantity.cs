using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Types;


[DataRecord, Serializable, NetSerializable]
public partial struct ReagentQuantity
{
    [DataField(required:true)]
    public string ReagentId;

    [NonSerialized]
    public Entity<ReagentDefinitionComponent>? ReagentDef;


    [DataField(required:true)]
    public FixedPoint2 Quantity;

    public ReagentQuantity()
    {
        ReagentId = string.Empty;
        ReagentDef = null;
    }

    public ReagentQuantity(string reagentId,
        FixedPoint2 quantity)
    {
        ReagentId = reagentId;
        Quantity = quantity;
        ReagentDef = null;
    }

    public ReagentQuantity(Entity<ReagentDefinitionComponent> reagentDef,
        FixedPoint2 quantity)
    {
        ReagentId = reagentDef.Comp.Id;
        Quantity = quantity;
        ReagentDef = reagentDef;
    }

    public bool IsValid(SharedChemistryRegistrySystem chemRegistry)
    {
        if (ReagentDef != null)
        {
            return ReagentDef.Value.Comp.Id == ReagentId;
        }
        return chemRegistry.TryIndex(ReagentId, out ReagentDef) && ReagentDef.Value.Comp.Id == ReagentId;
    }

    public static implicit operator ReagentQuantity(
        (string, FixedPoint2) t) => new (t.Item1, t.Item2);
    public static implicit operator ReagentQuantity(
        (Entity<ReagentDefinitionComponent>, FixedPoint2) t) => new (t.Item1, t.Item2);

    public static implicit operator (string reagentId, FixedPoint2 quantity)(ReagentQuantity r) => (r.ReagentId, r.Quantity);

    public static implicit operator (string reagentId, Entity<ReagentDefinitionComponent>? reagentDef, FixedPoint2 quantity)(
        ReagentQuantity r)
        => (r.ReagentId, r.ReagentDef, r.Quantity);

    public void Deconstruct(out string reagentId,
        out FixedPoint2 quantity)
    {
        reagentId = ReagentId;
        quantity = Quantity;
    }

    public void Deconstruct(out string reagentId,
        out Entity<ReagentDefinitionComponent>? reagentDef,
        out FixedPoint2 quantity)
    {
        reagentId = ReagentId;
        reagentDef = ReagentDef;
        quantity = Quantity;
    }

    public void UpdateReagentDef(SharedChemistryRegistrySystem chemRegistry)
    {
        if (ReagentDef != null && ReagentId == ReagentDef.Value.Comp.Id)
            return;

        if (chemRegistry.TryIndex(ReagentId, out ReagentDef))
            return;
        chemRegistry.Log.Error($"Reagent with ID:{ReagentId} could not be found in the chemical registry!");
    }
}
