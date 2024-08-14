using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

[DataDefinition, Serializable, NetSerializable]
public partial struct ReagentSpecifier: IEquatable<ReagentDef>
{
    public static readonly ReagentSpecifier Invalid = new();

    [DataField]
    public string Id { get; private set; }

    /// <summary>
    /// Any additional data that is unique to this reagent type. E.g., for blood this could be DNA data.
    /// </summary>
    [DataField]
    public ReagentVariant? Variant { get; private set; }

    [NonSerialized]
    private Entity<ReagentDefinitionComponent>? _cachedDefinitionEntity ;

    [ViewVariables]
    public Entity<ReagentDefinitionComponent>? CachedDefinitionEntity => _cachedDefinitionEntity;

    [DataField]
    public bool IsValid { get; }

    public ReagentSpecifier()
    {
        IsValid = false;
        Id = "";
        Variant = null;
    }

    public ReagentSpecifier(string id, ReagentVariant? variant = null)
    {
        IsValid = true;
        Id = id;
        Variant = variant;
    }

    public ReagentSpecifier(Entity<ReagentDefinitionComponent> reagent, ReagentVariant? variant = null)
        : this(reagent.Comp.Id, variant)
    {
        _cachedDefinitionEntity = reagent;
    }

    public ReagentSpecifier(ReagentDef reagent)
        : this(reagent.Id, reagent.Variant)
    {
        _cachedDefinitionEntity = reagent.DefinitionEntity;
    }


    public bool TryGetReagentDefinitionEntity(out Entity<ReagentDefinitionComponent> reagent)
    {
        if (_cachedDefinitionEntity == null)
        {
            reagent = default;
            return false;
        }
        reagent = _cachedDefinitionEntity.Value;
        return true;
    }

    public bool TryGetReagentDef(out ReagentDef reagentDef)
    {
        if (_cachedDefinitionEntity == null)
        {
            reagentDef = default;
            return false;
        }
        reagentDef = new (_cachedDefinitionEntity.Value, Variant);
        return true;
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

    public void Deconstruct(out string id, out ReagentVariant? variant)
    {
        id = Id;
        variant = Variant;
    }

    public void Deconstruct(out Entity<ReagentDefinitionComponent>? reagent, out ReagentVariant? variant)
    {
        reagent = _cachedDefinitionEntity;
        variant = Variant;
    }

    public static bool operator ==(ReagentSpecifier left, ReagentSpecifier right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ReagentSpecifier left, ReagentSpecifier right)
    {
        return !(left == right);
    }

    public static implicit operator string(ReagentSpecifier d) => d.Id;
    public static implicit operator ReagentSpecifier(string s) => new(s);
    public static implicit operator (string, ReagentVariant?)(ReagentSpecifier d) => (d.Id, d.Variant);
    public static implicit operator ReagentSpecifier(ReagentDef d) => new(d);
    public static implicit operator ReagentSpecifier(Entity<ReagentDefinitionComponent> def) => new(def, null);
    public static void UpdateCachedEntity(ref ReagentSpecifier reagentSpec, SharedChemistryRegistrySystem chemRegistry)
    {
        if (!chemRegistry.TryGetReagentEntity(reagentSpec.Id, out var reagent, true))
        {
            reagentSpec._cachedDefinitionEntity = null;
            return;
        }
        reagentSpec._cachedDefinitionEntity = reagent;
    }

    public static bool ResolveReagentEntity(ref ReagentSpecifier reagentSpec,
        SharedChemistryRegistrySystem chemRegistry,
        bool logIfMissing = true)
    {
        return !chemRegistry.ResolveReagent(reagentSpec.Id, ref reagentSpec._cachedDefinitionEntity, logIfMissing);
    }
}
