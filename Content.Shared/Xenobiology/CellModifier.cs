using Content.Shared.Xenobiology.Components.Container;

namespace Content.Shared.Xenobiology;

[Serializable, ImplicitDataDefinitionForInheritors]
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
