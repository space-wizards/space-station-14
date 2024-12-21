using Content.Shared.Xenobiology.Components.Container;
using JetBrains.Annotations;

namespace Content.Shared.Xenobiology.Modifiers;

/// <summary>
/// Doesn't do anything, because OwO is a server component :(
/// </summary>
[Serializable, UsedImplicitly]
public sealed partial class CellOwOModifier : CellModifier
{
    public override void OnAdd(Entity<CellContainerComponent> ent, Cell cell, IEntityManager entityManager)
    {
        base.OnAdd(ent, cell, entityManager);
    }

    public override void OnRemove(Entity<CellContainerComponent> ent, Cell cell, IEntityManager entityManager)
    {
        base.OnRemove(ent, cell, entityManager);
    }
}
