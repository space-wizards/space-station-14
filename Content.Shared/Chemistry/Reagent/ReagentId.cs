using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Linq;
using Robust.Shared.Prototypes;

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
    public List<ReagentData>? Data { get; private set; } = new();

    public ReagentId(string prototype, List<ReagentData>? data)
    {
        Prototype = prototype;
        Data = data ?? new();
    }

    public ReagentId()
    {
        Prototype = default!;
        Data = new();
    }

    [Obsolete("If you are using this, you don't care about getting extended reagent runtime data like DNA and dynamic color")]
    public ReagentId(string prototype)
    {
        Prototype = prototype;
        Data = null;
    }

    public Color GetColor()
    {
        // There should only ever be one color in Data,
        // but maybe we should interpolate the whole list here anyway.
        var fetchedReagentColorData = Data?.OfType<ReagentColorData>().FirstOrDefault();
        if (fetchedReagentColorData != null)
            return fetchedReagentColorData.SubstanceColor;

        var protoMan =  IoCManager.Resolve<IPrototypeManager>();
        var proto = new ProtoId<ReagentPrototype>(Prototype);
        return !protoMan.TryIndex(proto, out var regProto)
               ? default(Color)
               : regProto.SubstanceColor;
    }

    public List<ReagentData> EnsureReagentData()
    {
        return (Data != null) ? Data : new List<ReagentData>();
    }

    public bool Equals(ReagentId other)
    {
        if (Prototype != other.Prototype)
            return false;

        if (Data == null)
            return other.Data == null;

        if (other.Data == null)
            return false;

        if (Data.Except(other.Data).Any() || other.Data.Except(Data).Any() || Data.Count != other.Data.Count)
            return false;

        return true;
    }

    public bool Equals(string prototype, List<ReagentData>? otherData = null)
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
        return $"{Prototype}:{quantity}";
    }

    public override string ToString()
    {
        return $"{Prototype}";
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
