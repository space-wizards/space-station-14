using Content.Shared.Xenobiology.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology;

[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class CellModifier : IEquatable<CellModifier>
{
    public virtual void OnAdd(Entity<CellContainerComponent> ent, Cell cell, IEntityManager entityManager)
    {
        // Literally do nothing
    }

    public virtual void OnRemove(Entity<CellContainerComponent> ent, Cell cell, IEntityManager entityManager)
    {
        // Literally do nothing
    }

    public override bool Equals(object? obj)
    {
        return obj is CellModifier cellModifier && Equals(cellModifier);
    }

    public bool Equals(CellModifier? other)
    {
        return other is not null && GetType() == other.GetType();
    }

    public override int GetHashCode()
    {
        return GetType().GetHashCode();
    }
}
