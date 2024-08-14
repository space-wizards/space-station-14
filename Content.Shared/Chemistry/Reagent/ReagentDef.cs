using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

/// <summary>
/// Struct used to uniquely identify a reagent. This is usually just a ReagentPrototype id string, however some reagents
/// contain additional data (e.g., blood could store DNA data).
/// </summary>
public struct ReagentDef : IEquatable<ReagentDef>
{
    public static readonly ReagentDef Invalid = new();
    public string Id => DefinitionEntity.Comp.Id;

    /// <summary>
    /// Any additional data that is unique to this reagent type. E.g., for blood this could be DNA data.
    /// </summary>
    public ReagentVariant? Variant;

    public Entity<ReagentDefinitionComponent> DefinitionEntity;

    public bool IsValid { get; private set; } = false;

    public ReagentDef()
    {
        IsValid = false;
        DefinitionEntity = default!;
        Variant = null;
    }

    public ReagentDef(Entity<ReagentDefinitionComponent> reagentDef, ReagentVariant? variant)
    {
        IsValid = true;
        DefinitionEntity = reagentDef;
        Variant = variant;
    }

    public bool Equals(ReagentDef other)
    {
        if (Id != other.Id)
            return false;

        if (Variant == null)
            return other.Variant == null;

        if (other.Variant == null)
            return false;

        return Variant.GetType() == other.Variant.GetType() && Variant.Equals(other.Variant);
    }

    public bool Equals(string id, ReagentVariant? otherData = null)
    {
        if (Id != id)
            return false;

        if (Variant == null)
            return otherData == null;

        return Variant.Equals(otherData);
    }

    public override bool Equals(object? obj)
    {
        return obj is ReagentDef other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Variant);
    }

    public string ToString(FixedPoint2 quantity)
    {
        return Variant?.ToString(Id, quantity) ?? $"{Id}:{quantity}";
    }

    public override string ToString()
    {
        return Variant?.ToString(Id) ?? Id;
    }

    public static bool operator ==(ReagentDef left, ReagentDef right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ReagentDef left, ReagentDef right)
    {
        return !(left == right);
    }

    public static implicit operator string(ReagentDef d) => d.Id;
    public static implicit operator ReagentDef(Entity<ReagentDefinitionComponent> def) => new(def, null);
    public static implicit operator Entity<ReagentDefinitionComponent>(ReagentDef def) => def.DefinitionEntity;
    public static implicit operator ReagentVariant?(ReagentDef def) => def.Variant;
}

