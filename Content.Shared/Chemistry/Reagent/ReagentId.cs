using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Chemistry.Reagent;

/// <summary>
/// Struct used to uniquely identify a reagent. This is usually just a ReagentPrototype id string, however some reagents
/// contain additional data (e.g., blood could store DNA data).
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public partial struct ReagentId : IEquatable<ReagentId>
{
    // TODO rename data field.
    [DataField("ReagentId", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>), required: true)]
    public string Prototype { get; private set; }

    /// <summary>
    /// Any additional data that is unique to this reagent type. E.g., for blood this could be DNA data.
    /// </summary>
    [DataField("data")]
    public ReagentDiscriminator? Discriminator { get; private set; }

    public ReagentId(string prototype, ReagentDiscriminator? discriminator)
    {
        Prototype = prototype;
        Discriminator = discriminator;
    }

    public ReagentId()
    {
        Prototype = default!;
    }

    public bool Equals(ReagentId other)
    {
        if (Prototype != other.Prototype)
            return false;

        if (Discriminator == null)
            return other.Discriminator == null;

        if (other.Discriminator == null)
            return false;

        if (Discriminator.GetType() != other.Discriminator.GetType())
            return false;

        return Discriminator.Equals(other.Discriminator);
    }

    public bool Equals(string prototype, ReagentDiscriminator? otherData = null)
    {
        if (Prototype != prototype)
            return false;

        if (Discriminator == null)
            return otherData == null;

        return Discriminator.Equals(otherData);
    }

    public override bool Equals(object? obj)
    {
        return obj is ReagentId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Prototype, Discriminator);
    }

    public string ToString(FixedPoint2 quantity)
    {
        return Discriminator?.ToString(Prototype, quantity) ?? $"{Prototype}:{quantity}";
    }

    public override string ToString()
    {
        return Discriminator?.ToString(Prototype) ?? Prototype;
    }

    public static bool operator ==(ReagentId left, ReagentId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ReagentId left, ReagentId right)
    {
        return !(left == right);
    }
}
