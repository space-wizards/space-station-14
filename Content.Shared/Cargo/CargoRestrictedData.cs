using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo;

/// <summary>
/// Used to carry data in the station cargo storage.
/// Has a custom equatable for easier searching in the hashSet, might not be good policy, oh well
/// </summary>
[DataDefinition, NetSerializable, Serializable]
public sealed partial class CargoRestrictedData : IEquatable<CargoRestrictedData>
{
    [DataField]
    public string ProductId { get; private set; }
    public string ProductProductId { get; private set; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="productId">The product id as given in <see cref="CargoProductPrototype"/></param>
    /// <param name="productProductId">The products product id, which is required when checking against the orders product id as they can differ</param>
    public CargoRestrictedData(string productId, string productProductId = "")
    {
        ProductId = productId;
        ProductProductId = productProductId;
    }

    public bool Equals(CargoRestrictedData? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ProductId == other.ProductId;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is CargoRestrictedData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return ProductId.GetHashCode();
    }
}
