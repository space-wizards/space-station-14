using Robust.Shared.Serialization;

namespace Content.Shared.Cargo;

[DataDefinition, NetSerializable, Serializable]
public sealed partial class CargoRestrictedData : IEquatable<CargoRestrictedData>
{
    [DataField]
    public string ProductId { get; private set; }

    public CargoRestrictedData(string productId)
    {
        ProductId = productId;
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
