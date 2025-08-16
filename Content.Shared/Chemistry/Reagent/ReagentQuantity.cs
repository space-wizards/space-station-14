using System.Globalization;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

/// <summary>
/// Simple struct for storing a <see cref="ReagentId"/> & quantity tuple.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public partial struct ReagentQuantity(ReagentId reagent, FixedPoint2 quantity) : IEquatable<ReagentQuantity>
{
    [DataField("Quantity", required:true)]
    public FixedPoint2 Quantity { get; private set; } = quantity;

    [IncludeDataField]
    [ViewVariables]
    public ReagentId Reagent { get; private set; } = reagent;

    public ReagentQuantity(string reagentId, FixedPoint2 quantity, List<ReagentData>? data = null) : this(
        new ReagentId(reagentId, data),
        quantity)
    {
    }

    public ReagentQuantity() : this(default, default)
    {
    }

    /// <summary>
    /// Convenience method for getting the localized string for the quantity of this reagent. (E.g., 123u)
    /// </summary>
    public string LocalizedQuantity()
    {
        return LocalizedQuantity(Quantity);
    }

    /// <summary>
    /// Convenience function for getting the localized string for a given FixedPoint2. (E.g., 123u)
    /// </summary>
    /// <param name="quantity">The quantity to format. If null, it will default to 0.</param>
    public static string LocalizedQuantity(FixedPoint2? quantity)
    {
        // Weird loc id because backwards compatibility with older localizations is good
        return Loc.GetString("reagent-dispenser-window-quantity-label-text",
            ("quantity", (quantity ?? 0).ToString(CultureInfo.CurrentCulture)));
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
