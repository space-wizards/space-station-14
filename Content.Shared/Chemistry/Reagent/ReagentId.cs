using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Linq;

namespace Content.Shared.Chemistry.Reagent;

/// <summary>
/// Struct used to uniquely identify a reagent. This is usually just a ReagentPrototype id string, however some reagents
/// contain additional data (e.g., blood could store DNA data).
/// </summary>
[Serializable, NetSerializable]
[DataDefinition, Obsolete("Use ReagentDef Instead!")]
public partial struct ReagentId : IEquatable<ReagentId>
{

    [DataField("ReagentId", required: true)]
    public string Prototype { get; init; }

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

    public static implicit operator ReagentId(ReagentDef d)
    {
        return new ReagentId
        {
            Prototype = d.Id,
            Data = d.Variant,
        };
    }

    public static implicit operator ReagentDef(ReagentId d)
    {
        return new ReagentDef(d.Prototype, d.Data);
    }

    public ReagentId(Entity<ReagentDefinitionComponent> reagentDef, ReagentVariant? data) : this(reagentDef.Comp.Id,
        data)
    {
    }

    public ReagentId()
    {
        Prototype = default!;
        Data = new();
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
        if (Prototype != id)
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

    [Obsolete("Use ReagentDef instead of ReagentId")]
    public static bool operator ==(ReagentId left, ReagentId right)
    {
        return left.Equals(right);
    }

    [Obsolete("Use ReagentDef instead of ReagentId")]
    public static bool operator !=(ReagentId left, ReagentId right)
    {
        return !(left == right);
    }

    [Obsolete("Use ReagentDef instead of ReagentId")]
    public static bool operator ==(ReagentDef left, ReagentId right)
    {
        return left.Equals(right);
    }


    [Obsolete("Use ReagentDef instead of ReagentId")]
    public static bool operator !=(ReagentDef left, ReagentId right)
    {
        return !(left == right);
    }

    [Obsolete("Use ReagentDef instead of ReagentId")]
    public static bool operator ==(ReagentId left, ReagentDef right)
    {
        return left.Equals(right);
    }

    [Obsolete("Use ReagentDef instead of ReagentId")]
    public static bool operator !=(ReagentId left, ReagentDef right)
    {
        return !(left == right);
    }
}
