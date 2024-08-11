using Content.Shared.Xenobiology.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology;

[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class CellModifier
{
    public abstract void Modify(Entity<CellContainerComponent> ent, Cell cell, IEntityManager entityManager);
}
