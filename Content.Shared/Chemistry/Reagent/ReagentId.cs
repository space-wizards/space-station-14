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
    public ReagentData? Data { get; private set; }

    public ReagentId(string prototype, ReagentData? data)
    {
        Prototype = prototype;
        Data = data;
    }

    public ReagentId()
    {
        Prototype = default!;
    }

    public bool Equals(ReagentId other)
    {
        if (Prototype != other.Prototype)
            return false;

        if (Data == null)
            return other.Data == null;

        if (other.Data == null)
            return false;

        if (Data.GetType() != other.Data.GetType())
            return false;

        return Data.Equals(other.Data);
    }

    public bool Equals(string prototype, ReagentData? otherData = null)
    {
        if (Prototype != prototype)
            return false;

        if (Data == null)
            return otherData == null;

        return Data.Equals(otherData);
    }

    public override bool Equals(object? obj)
    {
        return obj is ReagentId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Prototype, Data);
    }

    public string ToString(FixedPoint2 quantity)
    {
        return Data?.ToString(Prototype, quantity) ?? $"{Prototype}:{quantity}";
    }

    public override string ToString()
    {
        return Data?.ToString(Prototype) ?? Prototype;
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
