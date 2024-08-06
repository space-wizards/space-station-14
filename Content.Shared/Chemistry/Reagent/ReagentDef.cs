using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

/// <summary>
/// Struct used to uniquely identify a reagent. This is usually just a ReagentPrototype id string, however some reagents
/// contain additional data (e.g., blood could store DNA data).
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public partial struct ReagentDef : IEquatable<ReagentDef>
{
    [DataField("ReagentId",required: true)]
    public string Id { get; init; } = string.Empty;

    [Obsolete("Use Id Field instead")]
    public string Prototype => Id;

    /// <summary>
    /// Any additional data that is unique to this reagent type. E.g., for blood this could be DNA data.
    /// </summary>
    [DataField("data")]
    public ReagentVariant? Variant { get; init; } = null;

    [Obsolete("Use Variant Field instead")]
    public ReagentVariant? Data => Variant;

    [NonSerialized]
    private Entity<ReagentDefinitionComponent> _definitionEntity = default!;

    [NonSerialized]
    private bool _isValid = false;

    [ViewVariables]
    public bool IsValid => _isValid;

    public Entity<ReagentDefinitionComponent> DefinitionEntity
    {
        get
        {
            if (!IsValid)
                throw new NullReferenceException($"ReagentDef with Id:{Id} is invalid, its DefinitionEntity is null");
            return _definitionEntity;
        }
        init => _definitionEntity = value;
    }

    public ReagentDef(string id, ReagentVariant? data)
    {
        Id = id;
        Variant = data;
    }

    public ReagentDef(Entity<ReagentDefinitionComponent> reagentDef, ReagentVariant? data) : this(reagentDef.Comp.Id,
        data)
    {
        _isValid = true;
    }

    public ReagentDef()
    {
        Id = string.Empty;
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

    public bool Validate(SharedChemistryRegistrySystem chemRegistry, bool logMissing = true)
    {
        if (IsValid)
            return true;
        if (!chemRegistry.TryIndex(Id, out var foundEnt, logMissing))
            return false;
        _definitionEntity = foundEnt.Value;
        _isValid = true;
        return true;
    }
}

