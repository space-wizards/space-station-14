using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

/// <summary>
/// Simple struct for storing a <see cref="ReagentId"/> & quantity tuple.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition, Obsolete("Use Chemistry.Types.ReagentQuantity or Chemistry.Types.ReagentVariantQuantity instead!")]
public partial struct ReagentQuantity : IEquatable<ReagentQuantity>
{
    [DataField("Quantity", required:true)]
    public FixedPoint2 Quantity { get; private set; }

    [IncludeDataField]
    [ViewVariables]
    public ReagentId Reagent { get; private set; }

    public bool TryConvert([NotNullWhen(true)] out Types.ReagentQuantity? newData,
        SharedChemistryRegistrySystem? chemRegistry = null)
    {
        newData = null;
        if (Reagent.Data != null)
            return false;
        chemRegistry ??= IoCManager.Resolve<EntitySystemManager>().GetEntitySystem<SharedChemistryRegistrySystem>();
        if (!chemRegistry.TryIndex(Reagent.Prototype, out var regDef))
            return false;
        newData = new Types.ReagentQuantity(regDef.Value, Quantity);
        return true;
    }

    public bool TryConvertToVariant([NotNullWhen(true)] out Types.ReagentVariantQuantity? newData,
        SharedChemistryRegistrySystem? chemRegistry = null)
    {
        newData = null;
        if (Reagent.Data == null)
            return false;
        chemRegistry ??= IoCManager.Resolve<EntitySystemManager>().GetEntitySystem<SharedChemistryRegistrySystem>();
        if (!chemRegistry.TryIndex(Reagent.Prototype, out var regDef))
            return false;
        newData = new Types.ReagentVariantQuantity(regDef.Value, Reagent.Data ,Quantity);
        return true;
    }

    public ReagentQuantity(string reagentId, FixedPoint2 quantity, ReagentData? data)
        : this(new ReagentId(reagentId, data), quantity)
    {
    }

    public ReagentQuantity(ReagentId reagent, FixedPoint2 quantity)
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

    public void Deconstruct(out ReagentId id, out FixedPoint2 quantity)
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
