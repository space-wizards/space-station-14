using Content.Shared.Xenobiology.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology;

[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class CellModifier
{
    public virtual void OnAdd(Entity<CellContainerComponent> ent, Cell cell, IEntityManager entityManager)
    {
        // Literally do nothing
    }

    public virtual void OnRemove(Entity<CellContainerComponent> ent, Cell cell, IEntityManager entityManager)
    {
        // Literally do nothing
    }
}
